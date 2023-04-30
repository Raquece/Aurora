using Aurora.Core.Modules;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Principal;
using Module = Aurora.Core.Modules.Module;
using System.Runtime.InteropServices;
using System.IO;
using Aurora.Core.IO;

namespace Aurora
{
    internal class Program
    {
        public static void Main(string[] args)
            => new Program().MainAsync();

        public async void MainAsync()
        {
            // Checks user has elevated privileges.
            if (!CheckPrivileges())
            {
                Console.WriteLine("Application must be run with elevated privileges");

                return;
            }

            Console.ReadLine();

            // Builds the service provider
            Container container = new Container();
            IServiceProvider services = container.BuildServiceProvider();

            // Iterate through each service type
            foreach (var serviceType in container.ServiceTypes)
            {
                // Checks if service is a module
                if (typeof(IModule).IsAssignableFrom(serviceType))
                {
                    var module = services.GetService(serviceType) as Module;

                    try
                    {
                        // Check if module uses generic arguments, if it does, load its configuration from the class
                        var genericArgs = serviceType.BaseType.GetGenericArguments();
                        if (genericArgs.Length != 0)
                        {
                            // Gets the path of the configuration file
                            string path = Path.Combine("config", serviceType.GetProperty("ConfigurationFileName").GetValue(module) as string);

                            if (File.Exists(path))
                            {
                                try
                                {
                                    // Configuration exists, attempt to read it

                                    // Gets the method for reading the XML file of the generic argument type
                                    MethodInfo readXmlMethod = typeof(Persistence).GetMethod(nameof(Persistence.ReadXml));
                                    readXmlMethod = readXmlMethod.MakeGenericMethod(genericArgs[0]);

                                    var config = readXmlMethod.Invoke(null, new object[] { path });

                                    // Sets the module's configuration property to the config variable
                                    serviceType.GetProperty("Configuration").SetValue(module, config);
                                }
                                catch (Exception _)
                                {
                                    // Something failed, create and save a new configuration file

                                    // Gets the method for writing the XML file of the generic argument type
                                    MethodInfo writeXmlMethod = typeof(Persistence).GetMethod(nameof(Persistence.WriteXml));
                                    writeXmlMethod = writeXmlMethod.MakeGenericMethod(genericArgs[0]);

                                    // Create a new configuration instance
                                    var config = Activator.CreateInstance(genericArgs[0]);

                                    writeXmlMethod.Invoke(null, new object[] { path, config });

                                    // Sets the module's configuration property to the config variable
                                    serviceType.GetProperty("Configuration").SetValue(module, config);
                                }
                            }
                            else
                            {
                                // Configuration does not exist, create and save a new configuration file
                                Directory.CreateDirectory(Directory.GetParent(path).FullName);
                                using var writer = File.Create(path);
                                writer.Close();

                                // Gets the method for writing the XML file of the generic argument type
                                MethodInfo writeXmlMethod = typeof(Persistence).GetMethod(nameof(Persistence.WriteXml));
                                writeXmlMethod = writeXmlMethod.MakeGenericMethod(genericArgs[0]);

                                // Create a new configuration instance
                                var config = Activator.CreateInstance(genericArgs[0]);

                                writeXmlMethod.Invoke(null, new object[] { path, config });

                                // Sets the module's configuration property to the config variable
                                serviceType.GetProperty("Configuration").SetValue(module, config);
                            }
                        }

                        module?.Initialise();
                    }
                    catch (Exception ex)
                    {
                        // Module failed to initialise, display an error

                        var col = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[CRIT] Service {serviceType.Name} failed to initialise.");
                        Console.ForegroundColor = col;
                    }
                }
            }

            // Block execution of the main thread
            await Task.Delay(-1);
        }

        public bool CheckPrivileges()
        {
            // Checks if program is running windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Check if user is administrator
                return new WindowsPrincipal(WindowsIdentity.GetCurrent())
                    .IsInRole(WindowsBuiltInRole.Administrator);
            }
            else
            {
                // Check program is running as root
                return geteuid() == 0;
            }
        }

        /// <summary>
        /// Gets the user ID on UNIX systems
        /// </summary>
        /// <returns>The user id</returns>
        [DllImport("libc", SetLastError = true)]
        internal static extern uint geteuid();
    }
}
