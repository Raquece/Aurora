using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aurora.Core.Concurrency
{
    /// <summary>
    /// Asynchronous task queue implementation for queues with dedicated threads.
    /// </summary>
    /// <typeparam name="T">The return type of the tasks</typeparam>
    public class DedicatedTaskQueue<T> : TaskQueue<T>
    {
        /// <summary>
        /// Main worker thread for this object
        /// </summary>
        private Thread thread;

        /// <summary>
        /// Collection of each task
        /// </summary>
        private readonly BlockingCollection<Task<T>> tasks = new BlockingCollection<Task<T>>();

        /// <summary>
        /// Adds a task to the queue of tasks.
        /// </summary>
        /// <param name="task">The task to enqueue.</param>
        public override void Enqueue(Task<T> task)
        {
            tasks.Add(task);
        }

        /// <summary>
        /// Ran on thread start.
        /// </summary>
        public override void OnStart()
        {
            foreach (var task in tasks.GetConsumingEnumerable(CancellationToken.None))
            {
                task.RunSynchronously();
            }
        }

        /// <summary>
        /// Initialises and starts the object.
        /// </summary>
        protected override void Start()
        {
            thread = new Thread(new ThreadStart(OnStart))
            {
                IsBackground = true
            };

            thread.Start();
        }
    }
}
