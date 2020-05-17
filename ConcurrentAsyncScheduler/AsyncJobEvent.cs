#region

using System;

#endregion

namespace ConcurrentAsyncScheduler
{
    /// <summary>
    ///     General purpose <see cref="EventHandler" /> for <see cref="AsyncJob" /> related events.
    /// </summary>
    /// <param name="sender">Invocator of the event.</param>
    /// <param name="asyncJob">Subject <see cref="AsyncJob" /> of the event.</param>
    public delegate void AsyncJobEventHandler(object sender, AsyncJob asyncJob);

    /// <summary>
    ///     General-purpose <see cref="EventArgs" /> type for handling events regarding a <see cref="AsyncJob" />.
    /// </summary>
    public class AsyncJobEventArgs : EventArgs
    {
        /// <summary>
        ///     Subject <see cref="AsyncJob" /> of the event arguments.
        /// </summary>
        public AsyncJob AsyncJob { get; }

        /// <summary>
        ///     Instantiates a new <see cref="AsyncJobEventArgs" />.
        /// </summary>
        /// <param name="asyncJob">Subject <see cref="AsyncJob" /> of the event arguments.</param>
        public AsyncJobEventArgs(AsyncJob asyncJob) => AsyncJob = asyncJob;
    }
}
