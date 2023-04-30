using Aurora.Core.Modules.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Aurora.Core.Modules
{
    /// <summary>
    /// Module for processing firewall and packet logging
    /// </summary>
    public class LoggingModule : Module<LoggingModule.Config>
    {
        public LoggingModule(FileIOModule file, ListenerModule listener)
        {
            _file = file;
            _listener = listener;
        }

        public override string ConfigurationFileName => "LoggingModule.cfg";

        // Service Modules
        private readonly FileIOModule _file;
        private readonly ListenerModule _listener;

        public override bool Initialise()
        {
            _listener.OnPacketCaptured += _listener_OnPacketCaptured;

            _file.OpenReader("logs/network.log", FileIOModule.ThreadPersistence.Dedicated);

            return true;
        }

        private void LogPacket(PacketCapturedEventArgs e)
        {
            var pcap = e.Packet.GetPacket();
            _file.Append("logs/network.log", $"Captured on DEV {e.CaptureDevice.Name} [{e.Packet.Timeval.Date}]");
        }

        private void _listener_OnPacketCaptured(object sender, PacketCapturedEventArgs e)
        {
            if (e == null)
            {
                return;
            }

            if (Configuration.Interfaces.Exists(i => i == e.CaptureDevice.Name))
            {
                LogPacket(e);
            }

        }

        public class Config
        {
            /// <summary>
            /// List of interfaces that are being monitored.
            /// </summary>
            public List<string> Interfaces { get; set; } = new List<string>();
        }
    }
}
