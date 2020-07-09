using System;
using System.Collections.Generic;

namespace IngameScript
{
    /// <summary>
    /// Handler for a specific command
    /// </summary>
    public interface ICommandHandler 
    {
        /// <summary>
        /// Tries handling a command
        /// </summary>
        /// <param name="cmd">Command</param>
        /// <param name="source">Source adddress</param>
        /// <returns>Whether the handler succeeded</returns>
        bool tryHandle(ICommand cmd, long source);
    }

    public sealed class LambdaCommandHandler<TCommand>: ICommandHandler where TCommand: ICommand
    {
        public Func<TCommand, long, bool> Handler { get; set;}

        public LambdaCommandHandler() {}

        public LambdaCommandHandler(Action<TCommand, long> handler)
        {
            Handler = (cmd, src) => { handler(cmd, src); return true; };
        }

        public bool tryHandle(ICommand cmd, long source)
        {
            if (cmd is TCommand) 
            {
                return Handler((TCommand)cmd, source);
            }
            return false;
        }
    }
}
