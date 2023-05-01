using Aurora.Core.Modules.Events;
using PacketDotNet;
using PacketDotNet.Ieee80211;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Aurora.Core.Modules
{
    /// <summary>
    /// Module for processing firewall and packet logging
    /// </summary>
    [BaseCommand("logging")]
    public class LoggingModule : Module<LoggingModule.Config>
    {
        public LoggingModule(FileIOModule file, ListenerModule listener, TerminalModule terminal)
        {
            _file = file;
            _listener = listener;
            _terminal = terminal;
        }

        public override string ConfigurationFileName => "LoggingModule.cfg";

        // Service Modules
        private readonly FileIOModule _file;
        private readonly ListenerModule _listener;
        private readonly TerminalModule _terminal;

        private bool started = false;

        public override bool Initialise()
        {
            _file.OpenReader("logs/network.log", FileIOModule.ThreadPersistence.Dedicated);
            _file.OpenReader("logs/network.pcap", FileIOModule.ThreadPersistence.Dedicated);

            return true;
        }

        private void LogPacket(PacketCapturedEventArgs e)
        {
            var packet = e.Packet.GetPacket();

            _file.Append("logs/network.pcap", e.Packet.Data);

            var tcpPacket = packet.Extract<TcpPacket>();
            if (tcpPacket != null)
            {
                var ipPacket = (IPPacket)tcpPacket.ParentPacket;
                _file.Append("logs/network.log", $"Captured on DEV {e.CaptureDevice.Name} [{e.Packet.Timeval.Date}] TCP {ipPacket.SourceAddress}:{tcpPacket.SourcePort} => {ipPacket.DestinationAddress}:{tcpPacket.DestinationPort}");
                return;
            }

            var udpPacket = packet.Extract<UdpPacket>();
            if (udpPacket != null)
            {
                var ipPacket = (IPPacket)udpPacket.ParentPacket;
                _file.Append("logs/network.log", $"Captured on DEV {e.CaptureDevice.Name} [{e.Packet.Timeval.Date}] UDP {ipPacket.SourceAddress}:{udpPacket.SourcePort} => {ipPacket.DestinationAddress}:{udpPacket.DestinationPort}");
                return;
            }
        }

        /// <summary>
        /// Starts the listener
        /// </summary>
        [Command("start")]
        public void Start()
        {
            _listener.OnPacketCaptured += _listener_OnPacketCaptured;
        }

        /// <summary>
        /// Stops the listener
        /// </summary>
        [Command("stop")]
        public void Stop()
        {
            _listener.OnPacketCaptured -= _listener_OnPacketCaptured;
        }

        /// <summary>
        /// Lists all rules
        /// </summary>
        [Command("list", "rules")]
        public void ListRules()
        {
            _terminal.Info("LOGGING RULES", this);
            _terminal.Info("| Interface Rules", this);
            ListInterfaces();
        }

        /// <summary>
        /// Adds a rule to the list of listened to interfaces
        /// </summary>
        /// <param name="rule">The rule that will be added</param>
        [Command("add", "rules interface")]
        public void AddInterface(string rule)
        {
            // Check if interface is already added as rule
            if (!Configuration.Interfaces.Contains(rule))
            {
                Configuration.Interfaces.Add(rule);
                _terminal.Info("Logging rule added", this);
            }
            else
            {
                _terminal.Error("Logging rule already exists", this);
            }
        }

        /// <summary>
        /// Removes a rule from the list of listened to interfaces
        /// </summary>
        /// <param name="rule">The rule that will be added</param>
        [Command("remove", "rules interface")]
        public void RemoveInterface(string rule)
        {
            // Check if interface is a rule
            if (Configuration.Interfaces.Contains(rule))
            {
                Configuration.Interfaces.Remove(rule);
                _terminal.Info("Logging rule removed", this);
            }
            else
            {
                _terminal.Error("Logging rule does not exist", this);
            }
        }

        /// <summary>
        /// Lists all interfaces that are being monitored
        /// </summary>
        [Command("list", "rules interface")]
        public void ListInterfaces()
        {
            foreach (var rule in Configuration.Interfaces)
            {
                _terminal.Info($" - {rule}", this);
            }
        }

        private void _listener_OnPacketCaptured(object sender, PacketCapturedEventArgs e)
        {
            if (e == null)
            {
                return;
            }

            if (Configuration.Interfaces.Exists(i => i == e.CaptureDevice.Name || i == "any" || i == "*"))
            {
                LogPacket(e);
            }
        }

        public class Config
        {
            /// <summary>
            /// List of interface name REGEX rules that are being monitored.
            /// </summary>
            public List<string> Interfaces { get; set; } = new List<string>();
        }
    }
}
