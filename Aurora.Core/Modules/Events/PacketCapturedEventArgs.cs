using SharpPcap;
using System;
using System.Collections.Generic;
using System.Text;

namespace Aurora.Core.Modules.Events
{
    /// <summary>
    /// Event arguments for when a packet is captured
    /// </summary>
    public class PacketCapturedEventArgs : EventArgs
    {
        /// <summary>
        /// The packet that has been captured
        /// </summary>
        public RawCapture Packet { get; set; }

        /// <summary>
        /// The listening device that has captured the packet
        /// </summary>
        public ICaptureDevice CaptureDevice { get; set; }
    }
}
