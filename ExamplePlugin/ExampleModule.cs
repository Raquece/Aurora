using Aurora.Core.IO;
using Aurora.Core.Modules;
using Aurora.Core.Modules.Events;
using System;

namespace ExamplePlugin
{
    /// <summary>
    /// Example plugin module to display total number of packets intercepted.
    /// </summary>
    [BaseCommand("stats")]
    public class ExampleStatisticsModule : Module<ExampleStatisticsModule.Config>
    {
        public ExampleStatisticsModule(TerminalModule terminal, ListenerModule listener, FileIOModule file)
        {
            _terminal = terminal;
            _listener = listener;
            _file = file;
        }

        public override string ConfigurationFileName => "StatisticsModule";

        /// <summary>
        /// Counter of every packet caught in this session
        /// </summary>
        private ulong counter = 0;

        // Service modules
        private readonly TerminalModule _terminal;
        private readonly ListenerModule _listener;
        private readonly FileIOModule _file;

        public override bool Initialise()
        {
            // Opens a reader on the configuration for persistent storage
            _file.OpenReader($"config/{ConfigurationFileName}", FileIOModule.ThreadPersistence.Dedicated);

            // Subscribe to event
            _listener.OnPacketCaptured += _listener_OnPacketCaptured;

            _terminal.Info("Custom statistics module loaded", this);

            return true;
        }

        /// <summary>
        /// Event subscriber to increment and save counters after each packet capture
        /// </summary>
        private void _listener_OnPacketCaptured(object sender, PacketCapturedEventArgs e)
        {
            // Increment counters
            counter++;
            Configuration.Total++;

            // Update the configuration to save the total counter
            _file.PerformAction($"config/{ConfigurationFileName}", file =>
            {
                Persistence.WriteXml(file, Configuration);

                return null;
            });
        }

        /// <summary>
        /// Lists packet capture statistics
        /// </summary>
        [Command("list")]
        public void List()
        {
            _terminal.Info($"Caught this session: {counter}", this);
            _terminal.Info($"Caught overall     : {Configuration.Total}", this);
        }

        public class Config
        {
            public ulong Total { get; set; }
        }
    }
}
