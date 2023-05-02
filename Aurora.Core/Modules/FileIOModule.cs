using Aurora.Core.Concurrency;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aurora.Core.Modules
{
    /// <summary>
    /// Provides services to handle file I/O operations asynchronously.
    /// </summary>
    public class FileIOModule : Module
    {
        public override bool Initialise()
        {
            return true;
        }

        /// <summary>
        /// Dictionary containing each file and its associated task queue
        /// </summary>
        private Dictionary<string, TaskQueue<byte[]>> taskDictionary = new Dictionary<string, TaskQueue<byte[]>>();

        /// <summary>
        /// Opens a task queue on a file ready to receive tasks.
        /// </summary>
        /// <param name="file">The file that is being modified or read</param>
        /// <param name="persistence">The persistence of the thread queue</param>
        public void OpenReader(string file, ThreadPersistence persistence)
        {
            var fileInfo = new FileInfo(file);

            if (!fileInfo.Exists)
            {
                Directory.CreateDirectory(fileInfo.DirectoryName);
                using var stream = fileInfo.Create();
                stream.Close();
                stream.Dispose();
            }

            if (!taskDictionary.ContainsKey(fileInfo.FullName))
            {
                if (persistence == ThreadPersistence.Dedicated)
                {
                    taskDictionary.Add(fileInfo.FullName, new DedicatedTaskQueue<byte[]>());
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        /// <summary>
        /// Reads all the text in a file
        /// </summary>
        /// <param name="file">The file to read</param>
        /// <returns>The contents of the file</returns>
        public Task<byte[]> ReadAllText(string file)
        {
            // Gets the file information
            var fileInfo = new FileInfo(file);

            // Create the task
            var task = new Task<byte[]>(() => Encoding.ASCII.GetBytes(File.ReadAllText(fileInfo.FullName)));

            // Check if reader has been opened
            if (taskDictionary.ContainsKey(fileInfo.FullName))
            {
                // Add the task to the task queue.
                taskDictionary[fileInfo.FullName].Enqueue(task);
            }
            else
            {
                // File reader has not been opened yet

                throw new ArgumentException("File does not have an associated reader");
            }

            return task;
        }

        /// <summary>
        /// Appends text to a file
        /// </summary>
        /// <param name="file">The file to add to</param>
        /// <param name="text">The text to add</param>
        /// <returns>The task</returns>
        public Task Append(string file, string text)
        {
            return Append(file, Encoding.ASCII.GetBytes(text));
        }

        /// <summary>
        /// Appends bytes to a file
        /// </summary>
        /// <param name="file">The file to add to</param>
        /// <param name="bytes">The bytes to add</param>
        /// <returns>The task</returns>
        public Task Append(string file, byte[] bytes)
        {
            // Gets the file information
            var fileInfo = new FileInfo(file);

            // Create the task
            var task = new Task<byte[]>(() =>
            {
                // Open the file stream
                using var stream = File.Open(file, FileMode.Append);

                // Write the bytes to the file
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush();

                // Free resources
                stream.Close();
                return null;
            });

            // Check if reader has been opened
            if (taskDictionary.ContainsKey(fileInfo.FullName))
            {
                // Add the task to the task queue.
                taskDictionary[fileInfo.FullName].Enqueue(task);
            }
            else
            {
                // File reader has not been opened yet

                throw new ArgumentException("File does not have an associated reader");
            }

            return task;
        }

        /// <summary>
        /// Denotes how long a thread should be kept alive
        /// </summary>
        public enum ThreadPersistence
        {
            Dedicated,
            Ephemeral
        }
    }
}
