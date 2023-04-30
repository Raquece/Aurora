using Aurora.Core.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurora.Core
{
    /// <summary>
    /// Abstract form of a dependency container
    /// </summary>
    public abstract class DIContainer
    {
        /// <summary>
        /// List of every service type provided by the container.
        /// </summary>
        public abstract Type[] ServiceTypes { get; }

        /// <summary>
        /// List of every module type provided by the container.
        /// </summary>
        public Type[] ModuleTypes => ServiceTypes.Where(t => typeof(IModule).IsAssignableFrom(t)).ToArray();
    }
}
