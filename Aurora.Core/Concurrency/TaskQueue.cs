using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Aurora.Core.Concurrency
{
    /// <summary>
    /// Abstract class for implementation of asynchronous task queues.
    /// </summary>
    /// <typeparam name="T">The return type of the tasks</typeparam>
    public abstract class TaskQueue<T>
    {
        public TaskQueue()
        {
            Start();
        }

        /// <summary>
        /// Adds a task to the queue of tasks.
        /// </summary>
        /// <param name="task">The task to enqueue.</param>
        public abstract void Enqueue(Task<T> task);

        /// <summary>
        /// Ran on thread start.
        /// </summary>
        public abstract void OnStart();

        /// <summary>
        /// Initialises and starts the object.
        /// </summary>
        protected abstract void Start();
    }
}
