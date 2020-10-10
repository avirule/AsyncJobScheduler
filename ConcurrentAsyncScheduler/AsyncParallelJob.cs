#region

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// ReSharper disable UnusedType.Global
// ReSharper disable MemberCanBePrivate.Global

#endregion

namespace ConcurrentAsyncScheduler
{
    /// <summary>
    ///     A modified <see cref="AsyncJob" /> type for efficiently handling the enumeration
    ///     and processing of a data set by index.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Ideally, this is a very TPL-friendly type to use when enumerating indexed data. However, consideration must be
    ///         taken that the overhead of instantiating many batches (and therefore many individual tasks) is non-negligible.
    ///         So if performance is a serious concern, care must be taken to ensure the batch length is not excessively small
    ///         in relation to the total data set length.
    ///     </para>
    ///     <para>
    ///         Additionally, there is flexibility with when the indexed data is enumerated in the <see cref="Process" />
    ///         function. For instance, the <see cref="Process" /> function can be overriden to first prepare the data set, and
    ///         then call <see cref="BatchTasksAndAwaitAll" /> explicitly to then process the batched data set. This
    ///         flexibility means that instead of replacing the functionality of the basic <see cref="AsyncJob" />, it is
    ///         instead extended to efficiently cover the processing indexed data.
    ///     </para>
    /// </remarks>
    public abstract class AsyncParallelJob : AsyncJob
    {
        /// <summary>
        ///     Total length of the data set that is enumerated.
        /// </summary>
        public int Length { get; }

        /// <summary>
        ///     Length of each batch of operations.
        /// </summary>
        public int BatchLength { get; }

        /// <summary>
        ///     Total number of batches, rounded up-away-from-zero to a whole number.
        /// </summary>
        public int TotalBatches { get; }

        protected AsyncParallelJob(int length, int batchLength)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater than zero.");
            }
            else if (batchLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(batchLength), "Batch length must be greater than zero.");
            }

            Length = length;
            BatchLength = batchLength;
            // round up to not lose the remainder
            TotalBatches = (int)Math.Ceiling((double)Length / BatchLength);
        }

        /// <summary>
        ///     Default method for batching tasks to enumerate a data set by index and wait on their completion.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This method can be overriden to provide specific functionality requiring the data to be prepared or
        ///         synchronously (*) modified after batched enumeration.
        ///     </para>
        ///     <para>
        ///         * Synchronously within the context of the method.
        ///     </para>
        /// </remarks>
        /// <returns>
        ///     A <see cref="Task" /> representing the completion of all batches.
        /// </returns>
        protected override async Task Process() => await BatchTasksAndAwaitAll().ConfigureAwait(false);

        /// <summary>
        ///     Used to process each individual index.
        /// </summary>
        /// <param name="index">Index the current batch's iteration is at.</param>
        protected abstract void ProcessIndex(int index);

        /// <summary>
        ///     Batches a set of tasks representing the enumeration of indexes equal to the provided <see cref="Length" />.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This method can be called directly to wait on the index enumeration within a custom control flow.
        ///     </para>
        ///     <para>
        ///         Additionally, the batched tasks are waited on with 'ConfigureAwait' set to <c>false</c>.
        ///     </para>
        /// </remarks>
        /// <returns>
        ///     <see cref="Task" /> representing all completed index enumerations.
        /// </returns>
        protected async Task BatchTasksAndAwaitAll()
        {
            IEnumerable<Task> GetBatchedTasks()
            {
                Task ProcessIndexes(int startIndex, int endIndex)
                {
                    for (int index = startIndex; index < endIndex; index++)
                    {
                        ProcessIndex(index);
                    }

                    return Task.CompletedTask;
                }

                int currentStartIndex = 0, batchIndex = 0;
                for (; batchIndex < TotalBatches; batchIndex++, currentStartIndex = batchIndex * BatchLength)
                {
                    yield return ProcessIndexes(currentStartIndex, Math.Min(Length, currentStartIndex + BatchLength));
                }
            }

            await Task.WhenAll(GetBatchedTasks()).ConfigureAwait(false);
        }
    }
}
