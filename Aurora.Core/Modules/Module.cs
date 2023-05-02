using SharpPcap;
using System;
using System.Collections.Generic;
using System.Text;

namespace Aurora.Core.Modules
{
    /// <summary>
    /// Declares the abstract class definition of a module.
    /// </summary>
    public abstract class Module : IModule
    {
        /// <summary>
        /// Returns the name of the module
        /// </summary>
        public string Name => GetType().Name;

        /// <summary>
        /// Initialises the module instance.
        /// </summary>
        /// <returns>Whether or not initialisation was successful.</returns>
        public abstract bool Initialise();

        /// <summary>
        /// Attribute for a base command group string to access a module's command group
        /// </summary>
        [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
        protected sealed class BaseCommandAttribute : Attribute
        {
            /// <summary>
            /// Creates a new instance of the BaseCommand attribute.
            /// </summary>
            /// <param name="baseString">The string to access the group of commands</param>
            public BaseCommandAttribute(string baseString)
            {
                CommandBase = baseString;
            }

            /// <summary>
            /// The string to access the group of commands
            /// </summary>
            public string CommandBase { get; }
        }

        /// <summary>
        /// Specifies a command that can be called from the terminal
        /// </summary>
        protected sealed class CommandAttribute : Attribute
        {
            /// <summary>
            /// Creates a new instance of the Command attribute
            /// </summary>
            /// <param name="alias">The alias to use the command</param>
            public CommandAttribute(string alias)
            {
                Alias = alias;
            }

            /// <summary>
            /// Creates a new instance of the Command attribute
            /// </summary>
            /// <param name="alias">The alias to use the command</param>
            /// <param name="group">The group that the command falls under</param>
            public CommandAttribute(string alias, string group)
            {
                Alias = alias;
                Group = group;
            }

            /// <summary>
            /// The alias that the command uses
            /// </summary>
            public string Alias { get; }

            /// <summary>
            /// The group that the command falls under, if any
            /// </summary>
            public string Group { get; }
        }
    }

    /// <summary>
    /// Declares the abstract class definition of a module with a class configuration file.
    /// </summary>
    /// <typeparam name="T">The class of the configuration file.</typeparam>
    public abstract class Module<T> : Module
    {
        /// <summary>
        /// The file location of the configuration file
        /// </summary>
        public abstract string ConfigurationFileName { get; }

        /// <summary>
        /// The configuration of the module
        /// </summary>
        public T Configuration { get; set; }
    }
}
