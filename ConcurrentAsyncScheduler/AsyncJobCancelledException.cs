#region

using System;

#endregion

namespace ConcurrentAsyncScheduler
{
    /// <summary>
    ///     <see cref="Exception" /> thrown when an <see cref="AsyncJob" /> is cancelled, and that
    ///     state interferes with its immediate usage.
    /// </summary>
    public class AsyncJobCancelledException : Exception
    {
        /// <summary>
        ///     Subject <see cref="AsyncJob" /> of the <see cref="Exception" />.
        /// </summary>
        public AsyncJob AsyncJob { get; }

        /// <summary>
        ///     Instantiates a new <see cref="AsyncJobCancelledException" />.
        /// </summary>
        /// <param name="message">Message for the <see cref="Exception" />.</param>
        /// <param name="asyncJob">Subject <see cref="AsyncJob" /> of the <see cref="Exception" />.</param>
        public AsyncJobCancelledException(string message, AsyncJob asyncJob)
            : base(message) => AsyncJob = asyncJob;

        /// <summary>
        ///     Instantiates a new <see cref="AsyncJobCancelledException" />.
        /// </summary>
        /// <param name="message">Message for the <see cref="Exception" />.</param>
        /// <param name="innerException">Internal <see cref="Exception" /> this follows.</param>
        /// <param name="asyncJob">Subject <see cref="AsyncJob" /> of the <see cref="Exception" />.</param>
        public AsyncJobCancelledException(string message, Exception innerException, AsyncJob asyncJob)
            : base(message, innerException) => AsyncJob = asyncJob;
    }
}
