using SharpPcap;
using System;
using System.Collections.Generic;
using System.Text;

namespace Aurora.Core.Modules.Events
{
    public class PacketCapturedEventArgs : EventArgs
    {
        public RawCapture Packet { get; set; }

        public ICaptureDevice CaptureDevice { get; set; }
    }
}
