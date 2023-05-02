using Aurora.Core.Modules.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Aurora.Core.Modules
{
    /// <summary>
    /// Controls usage of terminal
    /// </summary>
    [Command("console")]
    public class TerminalModule : Module
    {
        /// <summary>
        /// Event that fires when a line is entered into the terminal.
        /// </summary>
        public event EventHandler<InputReceivedEventArgs> OnInputEntered;

        /// <summary>
        /// Initialises the module.
        /// </summary>
        /// <returns>Initialisation result.</returns>
        public override bool Initialise()
        {
            // Create a new thread to take user's input
            Thread inputHandler = new Thread(ReadLoop)
            { 
                IsBackground = true,
                Name = "input-loop",
            };
            inputHandler.Start();

            return true;
        }

        /// <summary>
        /// Writes a line to the terminal
        /// </summary>
        /// <param name="text">Text to be written in the terminal</param>
        public void WriteLine(string text)
        {
            Console.WriteLine(text);
        }

        /// <summary>
        /// Displays a text information
        /// </summary>
        public void Info(string text, Module module)
        {
            Console.Write("[INFO] ");
            Console.WriteLine($"{module.Name} >> {text}");
        }

        /// <summary>
        /// Displays a text warning
        /// </summary>
        public void Warn(string text, Module module)
        {
            var col = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("[WARN] ");
            Console.ForegroundColor = col;
            Console.WriteLine($"{module.Name} >> {text}");
        }

        /// <summary>
        /// Displays a text error
        /// </summary>
        public void Error(string text, Module module)
        {
            var col = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("[ERRR] ");
            Console.ForegroundColor = col;
            Console.WriteLine($"{module.Name} >> {text}");
        }

        /// <summary>
        /// Constantly takes input form the console
        /// </summary>
        public void ReadLoop()
        {
            while(true)
            {
                var input = ReadLine();

                // Fire event corresponding to when an input is received
                OnInputEntered?.Invoke(this, new InputReceivedEventArgs()
                {
                    Input = input
                });
            }
        }

        /// <summary>
        /// Displayed when reading a line from the console
        /// </summary>
        /// <returns>The user input</returns>
        public string ReadLine()
        {
            Console.Write(" > ");
            return Console.ReadLine();
        }

        /// <summary>
        /// Clears the terminal
        /// </summary>
        [Command("clear")]
        public void Clear()
        {
            Console.Clear();
        }
    }
}
