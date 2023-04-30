using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurora.Core.Modules
{
    /// <summary>
    /// Module for processing information regarding installed modules and services.
    /// </summary>
    [BaseCommand("modules")]
    public class ServiceModule : Module
    {
        public ServiceModule(DIContainer container, TerminalModule terminal) 
        {
            _container = container;
            _terminal = terminal;
        }

        // Service modules
        private readonly DIContainer _container;
        private readonly TerminalModule _terminal;

        /// <summary>
        /// Initialises the module instance.
        /// </summary>
        /// <returns>Whether or not initialisation was successful.</returns>
        public override bool Initialise()
        {
            ListModules();

            return _container != null;
        }

        /// <summary>
        /// Lists all modules loaded
        /// </summary>
        [Command("list")]
        public void ListModules()
        {
            _terminal.WriteLine($"Modules: ({_container.ModuleTypes.Length}){_container.ModuleTypes.Aggregate("", (agg, dev) => agg + " " + dev.Name)}");
        }
    }
}
