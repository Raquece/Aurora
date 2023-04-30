using Aurora.Core.Modules;
using System;

namespace ExamplePlugin
{
    public class ExampleModule : Module
    {
        public ExampleModule(TerminalModule terminal)
        {
            _terminal = terminal;
        }

        // Service modules
        private readonly TerminalModule _terminal;

        public override bool Initialise()
        {
            _terminal.Info("Custom module loaded", this);

            return true;
        }
    }
}
