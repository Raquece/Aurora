using Aurora.Core.IO;
using Aurora.Core.Modules.Events;
using PacketDotNet;
using PacketDotNet.Ieee80211;
using SharpPcap.LibPcap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
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

        public override bool Initialise()
        {
            // Opens the network log file reader.
            _file.OpenReader("logs/network.log", FileIOModule.ThreadPersistence.Dedicated);

            return true;
        }

        /// <summary>
        /// Logs packet details
        /// </summary>
        /// <param name="e">The packet captured event arguments</param>
        private void LogPacket(PacketCapturedEventArgs e)
        {
            var packet = e.Packet.GetPacket();

            // Log TCP packet data
            var tcpPacket = packet.Extract<TcpPacket>();
            if (tcpPacket != null)
            {
                var ipPacket = (IPPacket)tcpPacket.ParentPacket;
                _file.Append("logs/network.log", $"Captured on DEV {e.CaptureDevice.Name} [{e.Packet.Timeval.Date}] TCP {ipPacket.SourceAddress}:{tcpPacket.SourcePort} => {ipPacket.DestinationAddress}:{tcpPacket.DestinationPort}{Environment.NewLine}");
                return;
            }

            // Log UDP packet data
            var udpPacket = packet.Extract<UdpPacket>();
            if (udpPacket != null)
            {
                var ipPacket = (IPPacket)udpPacket.ParentPacket;
                _file.Append("logs/network.log", $"Captured on DEV {e.CaptureDevice.Name} [{e.Packet.Timeval.Date}] UDP {ipPacket.SourceAddress}:{udpPacket.SourcePort} => {ipPacket.DestinationAddress}:{udpPacket.DestinationPort}{Environment.NewLine}");
                return;
            }
        }

        /// <summary>
        /// Starts the listener
        /// </summary>
        [Command("start")]
        public void Start(string device)
        {
            _listener.StartInterfaceCapture(device);
        }

        /// <summary>
        /// Stops the listener
        /// </summary>
        [Command("stop")]
        public void Stop(string device)
        {
            _listener.StopInterfaceCapture(device);
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

        /// <summary>
        /// Event subscriber for when a packet is captured. Logs packet information if configuration requires logging.
        /// </summary>
        private void _listener_OnPacketCaptured(object sender, PacketCapturedEventArgs e)
        {
            // Checks event arguments are not null
            if (e == null)
            {
                return;
            }

            // Log packets if configuration dictates that the packet should be logged.

            // Check if interfaces is required to be monitored.
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
