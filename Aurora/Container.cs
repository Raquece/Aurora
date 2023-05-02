using Aurora.Core;
using Aurora.Core.Modules;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Aurora
{
    public class Container : DIContainer
    {
        /// <summary>
        /// Initialises a new instance of the container.
        /// </summary>
        public Container()
        {
            List<IModule> modules = new List<IModule>();
            List<Type> serviceTypes = new List<Type>();

            // Adds this instance of the container to the service collection
            services.AddSingleton<DIContainer, Container>(_ => this);
            serviceTypes.Add(typeof(Container));

            // Adds all modules found in the assembly that IModule is found in (Core)
            AddModules(Assembly.GetAssembly(typeof(IModule)), ref serviceTypes);

            // Gets the plugin folder
            var pluginsDirectory = new DirectoryInfo("plugins");

            if (!pluginsDirectory.Exists)
            {
                // If folder does not exist, create it

                pluginsDirectory.Create();
            }
            else
            {
                // If plugins folder does exist, iterate through each file

                // Iterate through each file in the plugin directory
                foreach (var file in pluginsDirectory.GetFiles())
                {
                    try
                    {
                        // Attempt to load assembly through file
                        Assembly plugin = Assembly.LoadFrom(file.FullName);

                        // Add the modules found in the assembly
                        AddModules(plugin, ref serviceTypes);
                    } 
                    catch (BadImageFormatException _)
                    {
                        // File is not assembly file; ignore
                    }
                }
            }

            ServiceTypes = serviceTypes.ToArray();
        }

        /// <summary>
        /// Service collection object
        /// </summary>
        internal ServiceCollection services = new ServiceCollection();

        /// <summary>
        /// Collection of every service types used in the container.
        /// </summary>
        public override Type[] ServiceTypes { get; }

        /// <summary>
        /// Builds the service provider for use during dependency injection.
        /// </summary>
        /// <returns></returns>
        public IServiceProvider BuildServiceProvider()
            => services.BuildServiceProvider();

        /// <summary>
        /// Iterates through every type in assembly and add to service collection if it is a module.
        /// </summary>
        /// <param name="assembly">The assembly to search</param>
        /// <param name="serviceTypes">Working list of services</param>
        private void AddModules(Assembly assembly, ref List<Type> serviceTypes)
        {
            // Iterates through each module class found in assembly
            foreach (Type type in
                assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && !t.IsSubclassOf(typeof(IModule))))
            {
                // Add the service to the collection and list.
                serviceTypes.Add(type);
                services.AddSingleton(type);
            }
        }
    }
}
