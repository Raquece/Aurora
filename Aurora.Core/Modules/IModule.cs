using System;
using System.Collections.Generic;
using System.Text;

namespace Aurora.Core.Modules
{
    /// <summary>
    /// Interface declaring the required properties and methods of a module.
    /// </summary>
    public interface IModule
    {
        /// <summary>
        /// Initialises the module instance.
        /// </summary>
        /// <returns>Whether or not initialisation was successful.</returns>
        bool Initialise();
    }
}
