using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;
using System.Runtime.Serialization;
using VRageRender;
using System.Diagnostics.Eventing.Reader;

namespace IngameScript
{

    
    /// <summary>
    /// A base class that handles all broadcast/unicast messages received via IGC
    /// </summary>
    public class MessageHandler: IDisposable
    {
        private readonly IMyIntergridCommunicationSystem IGC;
        private readonly IList<IMyBroadcastListener> broadcastListeners = new List<IMyBroadcastListener>();
        private readonly Action<string> logger = (st) => { };

        /// <summary>
        /// Maps message type to a function that tries to parse it into an ICommand
        /// </summary>
        private Dictionary<string, Func<object, ICommand>> CommandParsers { get; set; } = new Dictionary<string, Func<object, ICommand>>();

        public Dictionary<string, ICommandHandler> CommandHandlers { get; private set; } = new Dictionary<string, ICommandHandler>();

        /// <summary>
        /// Registers an ICommand parser
        /// </summary>
        /// <typeparam name="TCommand">Type of command, this is used to determine the parsing process</typeparam>
        /// <param name="messageType">Integer identifying message type, to differentiate between parsers</param>
        private void RegisterParser<TCommand>() where TCommand: ICommand, new()
        {
            var messageType = new TCommand().Tag;
            if (CommandParsers.ContainsKey(messageType))
            {
                throw new ArgumentException($"A parser for tag {messageType} is already registered");
            }
            CommandParsers.Add(messageType, (object obj) =>
            {
                TCommand cmd = new TCommand();
                if (cmd.Deserialize(obj))
                {
                    return cmd;
                }
                return null;
            });
        }

        /// <summary>
        /// Registers an ICommand handler
        /// </summary>
        /// <typeparam name="TCommand">Type of command</typeparam>
        /// <param name="handler">Handler</param>
        /// <param name="acceptBroadcasts">Should we register a broadcast listener?</param>
        /// <typeparam name="TCommand"></typeparam>
        public void RegisterHandler<TCommand>(ICommandHandler handler, bool acceptBroadcasts = false) where TCommand: ICommand, new()
        {
            RegisterParser<TCommand>();
            var messageType = new TCommand().Tag;
            if (CommandHandlers.ContainsKey(messageType))
            {
                throw new ArgumentException($"A cmd-handler for tag {messageType} is already registered");
            }
            CommandHandlers.Add(messageType, handler);
            if (acceptBroadcasts)
            {
                broadcastListeners.Add(IGC.RegisterBroadcastListener(messageType));
            }
        }

        /// <summary>
        /// Tries handling an IGC message, whether sent via broadcast or unicast
        /// </summary>
        /// <param name="msg">Message</param>
        /// <returns>Did the handling succeed?</returns>
        private bool TryHandle(MyIGCMessage msg)
        {
            if (!CommandParsers.ContainsKey(msg.Tag))
            {
                logger($"Can't find parser for tag {msg.Tag}");
                return false;
            }
            var cmd = CommandParsers[msg.Tag](msg.Data);
            if (cmd == null)
            {
                logger($"Failed to parse message of tag {msg.Tag}, whose data is {msg.Data}");
                return false;
            }
            if (!CommandHandlers.ContainsKey(msg.Tag))
            {
                logger($"Parsed message of tag {msg.Tag} but couldn't find handler");
                return false;
            }
            var handled = CommandHandlers[msg.Tag].tryHandle(cmd, msg.Source);
            if (!handled)
            {
                logger($"Command handler couldn't handle message of tag {msg.Tag} whose inner data is {msg.Data}");
                return false;
            }
            return true;
        }



        private bool disposed = false;
        public MessageHandler(IMyIntergridCommunicationSystem igc, Action<string> logger = null)
        {
            
            this.IGC = igc;
            IGC.UnicastListener.SetMessageCallback();
            if (logger != null)
            {
                this.logger = logger;
            }
            logger($"{nameof(MessageHandler)} has been created");
        }
        public void Dispose()
        {
            if (!disposed)
            {
                logger($"{nameof(MessageHandler)} is being disposed.");
                IGC.UnicastListener.DisableMessageCallback();
                foreach (var listener in broadcastListeners)
                {
                    IGC.DisableBroadcastListener(listener);
                }
                disposed = true;
            }
        }
        public void Tick()
        {
            if (disposed)
            {
                return;
            }
            while (IGC.UnicastListener.HasPendingMessage)
            {
                var msg = IGC.UnicastListener.AcceptMessage();
                TryHandle(msg);
            }
            
            foreach (var listener in broadcastListeners)
            {
                if (listener.HasPendingMessage)
                {
                    var msg = listener.AcceptMessage();
                    TryHandle(msg);
                }
            }
        }


    }

}
