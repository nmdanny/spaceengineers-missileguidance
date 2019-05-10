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

namespace IngameScript
{
    public class MessageSender
    {
        private readonly IMyIntergridCommunicationSystem IGC;
        public MessageSender(IMyIntergridCommunicationSystem igc)
        {
            this.IGC = igc;
        }
        public void Broadcast<TData>(Command<TData> cmd, string tag)
        {
            var data = cmd.Sendable();
            this.IGC.SendBroadcastMessage(tag, data);
        }
        public void Unicast<TData>(Command<TData> cmd, string tag, long addressee)
        {
            var data = cmd.Sendable();
            this.IGC.SendUnicastMessage(addressee, tag, data);
        }
    }


    public abstract class MessageHandler: IDisposable
    {
        private readonly IMyIntergridCommunicationSystem IGC;
        private readonly IMyBroadcastListener listener;
        private readonly Action<string> logger = (st) => { };
        private readonly IEnumerable<CommandFactory> factories;
        public MessageHandler(IMyIntergridCommunicationSystem igc, string listenerTag,  IEnumerable<CommandFactory> factories, 
            Action<string> logger = null)
        {
            this.IGC = igc;
            this.listener = IGC.RegisterBroadcastListener(listenerTag);
            this.listener.SetMessageCallback();
            this.factories = factories;
            if (logger != null)
            {
                this.logger = logger;
            }
            logger($"{nameof(MessageHandler)} has been created, listening on {listener.Tag}.");
        }
        public void Dispose()
        {
            if (IGC != null)
            {
                listener.DisableMessageCallback();
                IGC.DisableBroadcastListener(this.listener);
                logger($"{nameof(MessageHandler)} is being disposed.");
            }
        }
        public void Tick()
        {
            while (listener.HasPendingMessage)
            {
                var msg = listener.AcceptMessage();
                if (msg.Tag == this.listener.Tag)
                {
                    foreach (var fac in this.factories)
                    {
                        object cmd;
                        if (fac.TryCreate(msg.Data, out cmd))
                        {
                            HandleMessage(cmd, msg.Source);
                            return;
                        }
                    }
                    // TODO: this doesn't work with messaging belonging to other ships.
                    /*
                    throw new ArgumentException($"None of {nameof(MessageHandler)} factories could handle the message " +
                        $"tag={msg.Tag}, data={msg.Data}.");
                        */
                }
                else
                {
                    OnInvalidMessage(msg);
                }
            }
        }

        protected abstract void HandleMessage(object command, long source);

        protected void OnInvalidMessage(MyIGCMessage message)
        {
            logger($"Received invalid message from {message.Source}: {message.Data}");
        }
    }

    public interface CommandFactory
    {
        bool TryCreate(object rawMsgData, out object cmd);
        int Tag { get; }
    }

    public abstract class Command<TData>
    {

        public class Factory<C> : CommandFactory where C : Command<TData>, new()
        {
            private readonly int tag;
            public Factory(int tag)
            {
                this.tag = tag;
            }
            public int Tag => this.tag;

            public bool TryCreate(object rawMsgData, out object cmd)
            {
                if (rawMsgData is MyTuple<int, TData>)
                {
                    var tup = (MyTuple<int, TData>)rawMsgData;
                    if (tup.Item1 == Tag)
                    {
                        var realCmd = new C();
                        realCmd.Deserialize(tup.Item2);
                        cmd = realCmd;
                        return true;
                    }
                }
                cmd = null;
                return false;
            }
        }

        public Command() { }
        protected abstract CommandFactory LocalFactory { get; }
        public int Tag => LocalFactory.Tag;
        protected abstract TData Serialize();
        
        public MyTuple<int, TData> Sendable()
        {
            return MyTuple.Create(Tag, Serialize());
        }
        protected abstract void Deserialize(TData data);
    }
}
