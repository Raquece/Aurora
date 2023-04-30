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
            Thread inputHandler = new Thread(ReadLoop);
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

        public void Info(string text, Module module)
        {
            Console.Write("[INFO] ");
            Console.WriteLine($"{module.Name} >> {text}");
        }

        public void Warn(string text, Module module)
        {
            var col = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("[WARN] ");
            Console.ForegroundColor = col;
            Console.WriteLine($"{module.Name} >> {text}");
        }

        public void Error(string text, Module module)
        {
            var col = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("[ERRR] ");
            Console.ForegroundColor = col;
            Console.WriteLine($"{module.Name} >> {text}");
        }

        public void ReadLoop()
        {
            while(true)
            {
                var input = ReadLine();

                OnInputEntered?.Invoke(this, new InputReceivedEventArgs()
                {
                    Input = input
                });
            }
        }

        public string ReadLine()
        {
            Console.Write(" > ");
            return Console.ReadLine();
        }
    }
}
