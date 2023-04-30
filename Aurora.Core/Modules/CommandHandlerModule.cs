﻿using Aurora.Core.Modules.Events;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Aurora.Core.Modules
{
    public class CommandHandlerModule : Module
    {
        public CommandHandlerModule(IServiceProvider services, TerminalModule terminal, DIContainer container)
        {
            _terminal = terminal;
            _container = container;
            _services = services;
        }

        // Service Modules
        private readonly DIContainer _container;
        private readonly TerminalModule _terminal;
        private readonly IServiceProvider _services;

        /// <summary>
        /// Dictionary of command bases and their command tree
        /// </summary>
        private readonly Dictionary<string, CommandBranch> commands = new Dictionary<string, CommandBranch>();

        public override bool Initialise()
        {
            // Register all module's commands to the command tree and dictionary

            // Iterate through each module registered in the dependency container
            foreach (var module in _container.ModuleTypes)
            {
                // Checks if the module has the base command attribute
                if (Attribute.GetCustomAttribute(module, typeof(BaseCommandAttribute)) is BaseCommandAttribute baseAttr)
                {
                    // Adds a new branch to the working command tree
                    commands.Add(baseAttr.CommandBase, new CommandBranch());

                    // Iterate through each method in the module
                    foreach (var method in module.GetMethods())
                    {
                        // Checks if the method has the command attribute
                        if (method.GetCustomAttribute(typeof(CommandAttribute)) is CommandAttribute cmdAttr)
                        {
                            // Add the command to the command tree
                            if (cmdAttr.Group == null)
                            {
                                commands[baseAttr.CommandBase].Commands.Add(cmdAttr.Alias, method);
                            }
                            else
                            {
                                // Creates or locates command branch representing command group

                                string[] splits = cmdAttr.Group.Split(' ');
                                CommandBranch currentBranch = commands[baseAttr.CommandBase];
                                for (int i = 0; i < splits.Length; i++)
                                {
                                    string split = splits[i];
                                    currentBranch.Branches.TryAdd(split, new CommandBranch());
                                    currentBranch = currentBranch.Branches[split];
                                }

                                // Add the command to the group
                                currentBranch.Commands.Add(cmdAttr.Alias, method);
                            }
                        }
                    }
                }
            }

            _terminal.OnInputEntered += ProcessCommand;

            return true;
        }

        private void ProcessCommand(object sender, InputReceivedEventArgs e)
        {
            if (e.Input == string.Empty || e.Input == null)
            {
                return;
            }

            // Check if command base matches any command

            // Splits input into array of words with space as delimiter, unless string is in quotation marks (much like bash terminals)
            var splits = Regex.Matches(e.Input, @"[\""].+?[\""]|[^ ]+")
                .Cast<Match>()
                .Select(m => m.Value)
                .ToArray();

            string[] args = null;
            MethodInfo commandMethod = null;
            if (commands.ContainsKey(splits[0]))
            {
                commandMethod = CheckBranch(commands[splits[0]], splits.Skip(1).ToArray(), out args);
            }

            try
            {
                // Run command method if it exists
                commandMethod?.Invoke(_services.GetService(commandMethod.DeclaringType), args);
            }
            catch (TargetParameterCountException _)
            {
                // Invalid number of parameters in input

                if (args == null)
                {
                    // No arguments received
                    _terminal.Error($"Could not parse input. Expected {commandMethod.GetParameters().Length} arguments, received 0", this);
                }
                else
                {
                    // Invalid number of arguments received
                    _terminal.Error($"Could not parse input. Expected {commandMethod.GetParameters().Length} arguments, received {args.Length}", this);
                }
            }
            //catch (ArgumentException _)
            //{
            //    // Invalid argument received

            //    if (args == null)
            //    {
            //    }
            //}
        }

        /// <summary>
        /// Recursively finds the command from the command tree.
        /// </summary>
        /// <param name="branch">The current branch that is being searched</param>
        /// <param name="splits">The user inputted command split with space as its delimiter</param>
        /// <returns>The <see cref="MethodInfo"/> of the command</returns>
        private MethodInfo CheckBranch(CommandBranch branch, string[] splits, out string[] args)
        {
            args = null;

            if (splits.Length == 0)
            {
                return null;
            }

            if (branch.Commands.ContainsKey(splits[0]))
            {
                if (splits.Length != 1)
                {
                    args = splits.Skip(1).ToArray();
                }
                return branch.Commands[splits[0]];
            }
            else
            {
                if (branch.Branches.ContainsKey(splits[0]))
                {
                    return CheckBranch(branch.Branches[splits[0]], splits.Skip(1).ToArray(), out args);
                }
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        private class CommandBranch
        {
            public Dictionary<string, CommandBranch> Branches { get; } = new Dictionary<string, CommandBranch>();

            public Dictionary<string, MethodInfo> Commands { get; } = new Dictionary<string, MethodInfo>();
        }
    }
}
