using Sandbox.ModAPI.Ingame;
using System;
using VRage;

namespace IngameScript
{
    public static class CommandExts
    {
        
        public static void SendBroadcast<TData>(this IMyIntergridCommunicationSystem igc, Command<TData> cmd)
        {
            igc.SendBroadcastMessage(cmd.Tag, cmd.Serialize());
        }

        public static void SendUnicast<TData>(this IMyIntergridCommunicationSystem igc, long dest, Command<TData> cmd)
        {
            igc.SendUnicastMessage(dest, cmd.Tag, cmd.Serialize());
        }
    }
    /// <summary>
    /// A message which can be sent/received via IGC
    /// </summary>
    public interface ICommand
    {

        /// <summary>
        /// Tries deserializing the object, updating the command's state
        /// </summary>
        /// <param name="obj">A serialized representation of the data to be serialized</param>
        /// <returns>Whether deserialization succeeded </returns>
        bool Deserialize(object obj);

        /// <summary>
        /// Tag used to identify this message
        /// </summary>
        /// <value>Identifier</value>
        string Tag { get;}
    }

    /// <summary>
    /// A typed wrapper over Command
    /// </summary>
    /// <typeparam name="TData">A serializable data type</typeparam>
    public abstract class Command<TData>: ICommand
    {

        
        /// <summary>
        /// Serializes the message
        /// </summary>
        /// <returns>A type which can be sent via IGC</returns>
        public abstract TData Serialize();
        
        protected abstract bool Deserialize(TData data);

        public string Tag => this.GetType().Name;

        bool ICommand.Deserialize(object obj)
        {
            if (obj is TData)
            {
                return Deserialize((TData)obj);
            }
            return false;
        }

    }

}
