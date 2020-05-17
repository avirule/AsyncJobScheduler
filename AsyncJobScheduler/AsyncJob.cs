#region

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global

#endregion

namespace AsyncJobScheduler
{
    /// <summary>
    ///     Used for
    /// </summary>
    public abstract class AsyncJob
    {
        public enum State : long
        {
            Idle,
            Working,
            Finished,
            Cancelled
        }

        private long _State;

        /// <summary>
        ///     <see cref="Stopwatch" /> used to track internal processing times.
        /// </summary>
        protected Stopwatch _Stopwatch { get; }

        /// <summary>
        ///     Token that can be passed into constructor to allow jobs to observe cancellation.
        /// </summary>
        protected CancellationToken _CancellationToken { get; }

        /// <summary>
        ///     Unique identity of the <see cref="AsyncJob" />.
        /// </summary>
        public Guid Identity { get; }

        /// <summary>
        ///     Represents the state of execution of the <see cref="AsyncJob" />.
        /// </summary>
        public State ExecutionState
        {
            get => (State)Interlocked.Read(ref _State);
            private set => Interlocked.Exchange(ref _State, (long)value);
        }

        /// <summary>
        ///     Elapsed execution time of the <see cref="Process" /> function.
        /// </summary>
        public TimeSpan ProcessTime { get; private set; }

        /// <summary>
        ///     Elapsed execution time of job.
        /// </summary>
        public TimeSpan ExecutionTime { get; private set; }

        /// <summary>
        ///     Event fired when the <see cref="AsyncJob" /> finishes its execution successfully.
        /// </summary>
        /// <remarks>
        ///     If the work is not completed successfully, this event will not fire. However, regardless
        ///     of cancellation state, its subscriptors will be dereferenced.
        /// </remarks>
        public event AsyncJobEventHandler? WorkFinished;

        /// <summary>
        ///     Instantiates a new instance of the <see cref="AsyncJob" /> class.
        /// </summary>
        /// <remarks>
        ///     With no <see cref="CancellationToken" /> specified, the <see cref="AsyncJobScheduler" />'s
        ///     abort token will be observed by default.
        /// </remarks>
        protected AsyncJob()
        {
            _Stopwatch = new Stopwatch();
            _CancellationToken = AsyncJobScheduler.AbortToken;

            // create new, unique job identity
            Identity = Guid.NewGuid();

            ExecutionState = State.Idle;
        }

        /// <summary>
        ///     Instantiates a new instance of the <see cref="AsyncJob" /> class with a <see cref="CancellationToken" />
        ///     to observe.
        /// </summary>
        /// <param name="cancellationToken">
        ///     <see cref="CancellationToken" /> to observe.
        /// </param>
        /// <remarks>
        ///     With a custom cancellation token specified, a linked token source will be created with
        ///     the <see cref="AsyncJobScheduler" />'s abort token.
        /// </remarks>
        protected AsyncJob(CancellationToken cancellationToken)
        {
            _Stopwatch = new Stopwatch();

            // combine AsyncJobScheduler's cancellation token with the given token, to effectively observe both
            _CancellationToken = CancellationTokenSource.CreateLinkedTokenSource(AsyncJobScheduler.AbortToken, cancellationToken).Token;

            // create new, unique job identity
            Identity = Guid.NewGuid();

            ExecutionState = State.Idle;
        }

        /// <summary>
        ///     Begins executing the <see cref="AsyncJob" />.
        /// </summary>
        internal async Task Execute()
        {
            try
            {
                // observe cancellation token
                if (_CancellationToken.IsCancellationRequested)
                {
                    Cancel();
                    return;
                }

                _Stopwatch.Restart();

                // set state to working
                ExecutionState = State.Working;

                // using ConfigureAwait(false) for performance concerns
                await Process().ConfigureAwait(false);

                ProcessTime = _Stopwatch.Elapsed;

                // again, using ConfigureAwait(false) for performance concerns
                await ProcessFinished().ConfigureAwait(false);

                _Stopwatch.Stop();

                ExecutionTime = _Stopwatch.Elapsed;

                // update state to finished
                ExecutionState = State.Finished;

                // signal WorkFinished event
                WorkFinished?.Invoke(this, this);
            }
            catch (Exception)
            {
                // update state to reflect cancellation status
                ExecutionState = State.Cancelled;
                throw;
            }
            finally
            {
                // dereference any subscriptors to avoid memory leaks
                WorkFinished = null!;
            }
        }

        /// <summary>
        ///     Initial method executed when <see cref="AsyncJob" /> is run.
        /// </summary>
        /// <remarks>
        ///     This method is run using with 'ConfigureAwait' set to <c>false</c>.
        /// </remarks>
        protected virtual Task Process() => Task.CompletedTask;

        /// <summary>
        ///     Last method executed, run after <see cref="Process" /> finishes.
        /// </summary>
        protected virtual Task ProcessFinished() => Task.CompletedTask;

        /// <summary>
        ///     Called whenever the <see cref="AsyncJob" /> has been cancelled.
        /// </summary>
        /// <remarks>
        ///     This method should should only ever expect be called once.
        /// </remarks>
        protected virtual void Cancelled() { }

        /// <summary>
        ///     Used to halt <see cref="AsyncJob" /> execution and set <see cref="ExecutionState" />
        ///     to <see cref="State.Cancelled" />.
        /// </summary>
        public void Cancel()
        {
            // don't call Cancelled() multiple times.
            if (ExecutionState != State.Cancelled)
            {
                ExecutionState = State.Cancelled;
                Cancelled();
            }
        }
    }
}
