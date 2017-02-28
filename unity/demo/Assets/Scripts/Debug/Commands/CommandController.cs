using System.Collections.Generic;

namespace Assets.Scripts.Debug.Commands
{
    /// <summary> Responsible for command registration. </summary>
    public class CommandController
    {
        public readonly Dictionary<string, ICommand> Commands = new Dictionary<string, ICommand>();

        /// <summary> Gets command by name. </summary>
        /// <param name="name">Name of command.</param>
        /// <returns>Command</returns>
        public ICommand this[string name] { get { return Commands[name]; } }

        /// <summary> Registers command. </summary>
        /// <param name="command">Command.</param>
        public void Register(ICommand command)
        {
            Commands[command.Name] = command;
        }

        /// <summary> Unregisters command. </summary>
        /// <param name="command">Command.</param>
        public void Unregister(ICommand command)
        {
            Commands.Remove(command.Name);
        }

        /// <summary> Checks whether command is registered. </summary>
        /// <param name="command">Command.</param>
        /// <returns>True if command registered.</returns>
        public bool Contains(string command)
        {
            return Commands.ContainsKey(command);
        }      
    }
}
