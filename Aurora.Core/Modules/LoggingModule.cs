using Aurora.Core.IO;
using Aurora.Core.Modules.Events;
using PacketDotNet;
using PacketDotNet.Ieee80211;
using SharpPcap.LibPcap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

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

            _listener.OnPacketCaptured += _listener_OnPacketCaptured;

            return true;
        }

        /// <summary>
        /// Logs packet details
        /// </summary>
        /// <param name="e">The packet captured event arguments</param>
        private void LogPacket(PacketCapturedEventArgs e)
        {
            var packet = e.Packet.GetPacket();

            var ipPacket = e.Packet.GetPacket().PayloadPacket as IPPacket;

            // Log TCP packet data
            if (ipPacket != null && ipPacket.Protocol == PacketDotNet.ProtocolType.Tcp)
            {
                var tcpPacket = packet.Extract<TcpPacket>();
                _file.Append("logs/network.log", $"Captured on DEV {e.CaptureDevice.Name} [{e.Packet.Timeval.Date}] TCP {ipPacket.SourceAddress}:{tcpPacket.SourcePort} => {ipPacket.DestinationAddress}:{tcpPacket.DestinationPort}{Environment.NewLine}");
                return;
            }

            // Log UDP packet data
            if (ipPacket != null && ipPacket.Protocol == PacketDotNet.ProtocolType.Udp)
            {
                var udpPacket = packet.Extract<UdpPacket>();
                _file.Append("logs/network.log", $"Captured on DEV {e.CaptureDevice.Name} [{e.Packet.Timeval.Date}] UDP {ipPacket.SourceAddress}:{udpPacket.SourcePort} => {ipPacket.DestinationAddress}:{udpPacket.DestinationPort}{Environment.NewLine}");
                return;
            }

            // Log ICMP data
            var icmpPacket = packet.Extract<IcmpV4Packet>();
            if (icmpPacket != null)
            {
                _file.Append("logs/network.log", $"Captured on DEV {e.CaptureDevice.Name} [{e.Packet.Timeval.Date}] ICMP {ipPacket.SourceAddress} - CHECK {icmpPacket.Checksum} SEQ {icmpPacket.Sequence}{Environment.NewLine}");
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
            _terminal.Info("| Source IP Rules", this);
            ListInterfaces();
            _terminal.Info($"| Log ICMP : {Configuration.LogICMP}", this);
        }

        /// <summary>
        /// Adds a rule to the list of listened to interfaces
        /// </summary>
        /// <param name="rule">The rule that will be added</param>
        [Command("add", "rules ip")]
        public void AddIP(string rule)
        {
            // Check if rule is an IP address
            if (IPAddress.TryParse(rule, out var ip))
            {
                // Check rule does not exist
                if (!Configuration.SourceAddresses.Contains(rule))
                {
                    // Add the rule
                    Configuration.SourceAddresses.Add(rule);
                    _terminal.Info("Logging rule added", this);
                }
                else
                {
                    _terminal.Error("Logging rule already exists", this);
                }
            }
            else
            {
                // Rule is not a valid IP address
                _terminal.Error("Input is not a valid IP address", this);
            }

            // Save configuration to file
            Persistence.WriteXml($"config/{ConfigurationFileName}", Configuration);
        }

        /// <summary>
        /// Removes a rule from the list of listened to interfaces
        /// </summary>
        /// <param name="rule">The rule that will be added</param>
        [Command("remove", "rules ip")]
        public void RemoveIP(string rule)
        {
            // Check if rule is an IP address
            if (IPAddress.TryParse(rule, out var ip))
            {
                // Check rule exists
                if (Configuration.SourceAddresses.Contains(rule))
                {
                    // Remove the rule
                    Configuration.SourceAddresses.Remove(rule);
                    _terminal.Info("Logging rule removed", this);
                }
                else
                {
                    _terminal.Error("Logging rule does not exist", this);
                }
            }
            else
            {
                // Rule is not a valid IP address
                _terminal.Error("Input is not a valid IP address", this);
            }

            // Save configuration to file
            Persistence.WriteXml($"config/{ConfigurationFileName}", Configuration);
        }

        /// <summary>
        /// Sets whether ICMP packets should be logged
        /// </summary>
        /// <param name="rule">True or false</param>
        [Command("imcp", "rules")]
        public void SetICMP(string rule)
        {
            // Check if argument is valid boolean value
            if (bool.TryParse(rule, out bool val))
            {
                Configuration.LogICMP = val;
                _terminal.Info("ICMP logging rule updated", this);
            }
            else
            {
                _terminal.Error("Invalid argument; either true or false", this);
            }

            // Save configuration to file
            Persistence.WriteXml($"config/{ConfigurationFileName}", Configuration);
        }

        /// <summary>
        /// Lists all interfaces that are being monitored
        /// </summary>
        [Command("list", "rules ips")]
        public void ListInterfaces()
        {
            foreach (var rule in Configuration.SourceIPAddresses)
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

            // Gets the IP packet of the packet
            var ipPacket = e.Packet.GetPacket().PayloadPacket as IPPacket;

            // Check if IP address needs to be logged due to logging rules
            if (ipPacket != null && Configuration.SourceAddresses.Any(i => i == ipPacket.SourceAddress.ToString()))
            {
                LogPacket(e);
            }

            // Check if packet is ICMP packet and if ICMP packets are logged
            var icmp = e.Packet.GetPacket().Extract<IcmpV4Packet>();
            if (icmp != null && Configuration.LogICMP)
            {
                LogPacket(e);
            }
        }

        public class Config
        {
            /// <summary>
            /// Gets or sets the list of source IP address strings that should be logged when a packet is received
            /// </summary>
            public List<string> SourceAddresses { get; set; } = new List<string>();

            /// <summary>
            /// Gets a list of source IP addresses that should be logged when a packet is received.
            /// </summary>
            [XmlIgnore]
            public IPAddress[] SourceIPAddresses
            {
                get
                {
                    // Returns an array of IPAddress objects parsed from the list of strings SourceAddresses, as
                    // long as the string is parsable as an IPAddress
                    return SourceAddresses
                        .Where(i => IPAddress.TryParse(i, out _))
                        .Select(i => IPAddress.Parse(i))
                        .ToArray();
                }
            }

            /// <summary>
            /// Gets or sets whether ICMP information should be logged.
            /// </summary>
            public bool LogICMP { get; set; } = false;
        }
    }
}
