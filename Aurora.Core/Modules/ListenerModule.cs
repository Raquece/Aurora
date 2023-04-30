using Aurora.Core.Modules.Events;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Aurora.Core.Modules
{
    public class ListenerModule : Module
    {
        public ListenerModule(TerminalModule terminal, FileIOModule file)
        {
            _terminal = terminal;
            _file = file;
        }

        public event EventHandler<PacketCapturedEventArgs> OnPacketCaptured;

        // Service Modules
        private readonly TerminalModule _terminal;
        private readonly FileIOModule _file;
        
        private ILiveDevice anyDevice;

        public override bool Initialise()
        {
            var devices = CaptureDeviceList.Instance;

            _terminal.Info("Devices detected: " + devices.Aggregate("", (agg, dev) => agg + " " + dev.Name), this);

            anyDevice = devices.FirstOrDefault(dev => dev.Name == "any");

            Listen(anyDevice);

            _terminal.Info("Loaded listener service", this);

            return true;
        }

        public void Listen(ILiveDevice dev)
        {
            _terminal.Info($"Opening listener on device {dev.Name}", this);

            dev.OnPacketArrival += new PacketArrivalEventHandler(ParsePacketCapture);
            dev.Open();
            dev.StartCapture();
        }

        private void ParsePacketCapture(object s, PacketCapture e)
        {
            OnPacketCaptured?.Invoke(s, new PacketCapturedEventArgs()
            {
                CaptureDevice = e.Device,
                Packet = e.GetPacket()
            });
        }
    }
}
