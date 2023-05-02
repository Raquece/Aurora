using Aurora.Core.Modules.Events;
using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Aurora.Core.Modules
{
    [BaseCommand("listener")]
    public class ListenerModule : Module
    {
        public ListenerModule(TerminalModule terminal, FileIOModule file)
        {
            _terminal = terminal;
            _file = file;
        }
        
        /// <summary>
        /// Fired whenever a packet is captured.
        /// </summary>
        public event EventHandler<PacketCapturedEventArgs> OnPacketCaptured;

        /// <summary>
        /// Represents the dev
        /// </summary>
        public ILiveDevice anyDevice;

        // Service Modules
        private readonly TerminalModule _terminal;
        private readonly FileIOModule _file;

        /// <summary>
        /// Dictionary containing every current device that is being dumped to a pcap file
        /// </summary>
        private Dictionary<string, CaptureFileWriterDevice> capturingDevices = new Dictionary<string, CaptureFileWriterDevice>();
        
        public override bool Initialise()
        {
            // Gets the list of network devices
            var devices = CaptureDeviceList.Instance;
            _terminal.Info("Devices detected: " + devices.Aggregate("", (agg, dev) => agg + " " + dev.Name), this);

            // Gets the device representing any device
            anyDevice = devices.FirstOrDefault(dev => dev.Name == "any");

            // Open a listener to listen for any network packet
            Listen(anyDevice);

            _terminal.Info("Loaded listener service", this);

            return true;
        }

        /// <summary>
        /// Starts listening for packets on a device
        /// </summary>
        /// <param name="dev">The device to listen on</param>
        public void Listen(ILiveDevice dev)
        {
            _terminal.Info($"Opening listener on device {dev.Name}", this);

            dev.OnPacketArrival += new PacketArrivalEventHandler(ParsePacketCapture);
            dev.Open();
            dev.StartCapture();
        }

        /// <summary>
        /// Starts outputting a network device's traffic to a file.
        /// </summary>
        /// <param name="dev">The device to output</param>
        public void StartInterfaceCapture(string dev)
        {
            // Get the device from the name
            var devices = CaptureDeviceList.Instance;
            var device = devices.FirstOrDefault(d => d.Name == dev);

            // Checks if device exists
            if (device != null)
            {
                device.OnPacketArrival += HandleCapturePacketArrival;

                device.Open();

                var writer = new CaptureFileWriterDevice($"logs/{dev}.pcap");
                writer.Open(device);

                device.StartCapture();
                _terminal.Info($"Started capturing on device {dev}", this);

                capturingDevices.Add(device.Name, writer);
            }
            else
            {
                _terminal.Error($"No such device {dev}", this);
            }
        }

        /// <summary>
        /// Stops outputting a network device's traffic and saves the file.
        /// </summary>
        /// <param name="dev">The device to output</param>
        public void StopInterfaceCapture(string dev)
        {
            // Get the device from the name
            var devices = CaptureDeviceList.Instance;
            var device = devices.FirstOrDefault(d => d.Name == dev);

            // Check if the device could be found
            if (device == null)
            {
                _terminal.Error($"No such device {dev}", this);
                return;
            }

            // Check network device is currently being captured
            if (capturingDevices.ContainsKey(dev))
            {
                // Stops the capture and disposes of the resources
                device.StopCapture();
                capturingDevices[dev].Dispose();

                _terminal.Info($"Stopped capturing on device {dev}", this);
                capturingDevices.Remove(dev);
            }
            else
            {
                _terminal.Error($"Device {dev} is not capturing", this);
            }
        }

        /// <summary>
        /// Lists all devices that are currently being dumped.
        /// </summary>
        /// <param name="dev"></param>
        [Command("list")]
        public void ListCapturedDevices()
        {
            // Iterate through each device being captured
            foreach (var device in capturingDevices.Keys)
            {
                _terminal.Info($" - {device}", this);
            }
        }

        /// <summary>
        /// Event handler for an incoming packet
        /// </summary>
        private void ParsePacketCapture(object s, PacketCapture e)
        {
            // Fire this module's event
            OnPacketCaptured?.Invoke(s, new PacketCapturedEventArgs()
            {
                CaptureDevice = e.Device,
                Packet = e.GetPacket()
            });
        }

        /// <summary>
        /// Event handler for when a captured network device receives a packet.
        /// </summary>
        private void HandleCapturePacketArrival(object sender, PacketCapture e)
        {
            var dev = (ILiveDevice)sender;

            // Write the packet to the PCAP file
            capturingDevices[dev.Name].Write(e.GetPacket());
        }
    }
}
