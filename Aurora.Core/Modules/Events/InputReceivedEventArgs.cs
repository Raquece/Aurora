using System;
using System.Collections.Generic;
using System.Text;

namespace Aurora.Core.Modules.Events
{
    /// <summary>
    /// Event arguments for when an input is received on the terminal
    /// </summary>
    public class InputReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// The input entered by the user
        /// </summary>
        public string Input { get; set; }
    }
}
