using Aurora.Core.Concurrency;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aurora.Core.Modules
{
    public class FileIOModule : Module
    {
        public override bool Initialise()
        {
            return true;
        }

        private Dictionary<string, TaskQueue<byte[]>> taskDictionary = new Dictionary<string, TaskQueue<byte[]>>();

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

        public Task<byte[]> ReadAllText(string file)
        {
            var fileInfo = new FileInfo(file);
            var task = new Task<byte[]>(() => Encoding.ASCII.GetBytes(File.ReadAllText(fileInfo.FullName)));
            if (taskDictionary.ContainsKey(fileInfo.FullName))
            {
                taskDictionary[fileInfo.FullName].Enqueue(task);
            }
            else
            {
                taskDictionary.Add(fileInfo.FullName, new DedicatedTaskQueue<byte[]>());
                taskDictionary[fileInfo.FullName].Enqueue(task);
            }

            return task;
        }

        public Task Append(string file, string text)
        {
            var fileInfo = new FileInfo(file);
            var task = new Task<byte[]>(() => 
            {
                File.AppendAllLines(file, new string[] { text });
                return null;
            });

            if (taskDictionary.ContainsKey(fileInfo.FullName))
            {
                taskDictionary[fileInfo.FullName].Enqueue(task);
            }
            else
            {
                taskDictionary.Add(fileInfo.FullName, new DedicatedTaskQueue<byte[]>());
                taskDictionary[fileInfo.FullName].Enqueue(task);
            }

            return task;
        }

        public enum ThreadPersistence
        {
            Dedicated,
            Ephemeral
        }
    }
}
