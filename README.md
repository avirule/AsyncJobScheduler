# AsyncJobScheduler
 An asynchronous scheduler used to execute jobs on the .NET ThreadPool.

## When should I use it?
 This framework is best used when you want to queue work that should allow continuous processes to go uninterrupted.
 For example, in a game, if you were to scheduled several hundred tasks with `Task.Run()` directly, the CPU core that
 the main and rendering threads are running on would be summarily hogged by the ThreadPool threads handling all of this work.

 The `AsyncJobScheduler` however, utilizes a semaphore to ensure that the CPU always has free cores to continue operating
 on the aforementioned continuous workloads. Additionally, if you've got an operation that will be run many times with
 only the data being worked on changing, in performance-sensitive contexts it can be beneficial to derive from the
 `AsyncJob` base class. This allows the work to be pooled, meaning the .NET GC won't wreck your process collecting implicit
 lambda declarations (in the case of `Task.Run()`).

## How do I use it?

### General Use

 Use of the framework is simple, as an example class:

 ```csharp
 public class MyAsyncJob : AsyncJob
 {
    public MyAsyncJob()
    {
        /* construct object */
    }

    public override Task Process()
    {
        /* execute work here */
    }
 }
 ```

 In the case of a pooled job, for example, you might have a member method like this:

 ```csharp
 public void SetData(int newId, string[] newPeopleNames)
 {
    /* reset member variables to given values */
 }
 ```

 In the case of such an operation, it may be prudent to additionally override the `Task ProcessFinished()` method, to
 for example clean up any data that lingers around after processing finished.

 If you're enumerating a fixed length data set, using the `AsyncParallelJob` would likely be very beneficial. This can be
 used like so:

 ```csharp
 public class AddOneAsyncParallelJob : AsyncParallelJob
 {
    public int[] Numbers { get; }

    public AddOneAsyncParallelJob(int[] startingNumbers)
    {
        Numbers = startingNumbers;
    }

    public override void ProcessIndex(int index)
    {
        Numbers[index] += 1;
    }
 }
 ```

 You'll notice the `ProcessIndex(int index)` method is return type `void` instead of `Task`. This is because the method
 is already collected into processable bits, and utilizing the TPL for each invocation (which, in the case of an even
 medium-sized data set, could be thousands) would end up being very inefficient.

 The above example is useful, although it's possible you need to do some specific preparations for the data set to be ready
 for enumeration. An example of how to achieve this is adding this method to the body of your derived class:

 ```csharp
 public override Task Process()
 {
    for (int index = 0; index < Numbers.Length; index++)
    {
        if (Numbers[index] < 0)
        {
            throw new Exception("All given numbers must be above or equal to 0!");
        }
    }

    await BatchTasksAndAwaitAll().ConfigureAwait(false);
 }
 ```

 While not a particularly inspired example, hopefully it communicates the idea. The flexibility offered here is, at least
 in my cases, extremely convenient.

 These example classes can then be queued with the following code:

 ```csharp
 AsyncJobScheduler.QueueAsyncJob(myAsyncJob);
 ```

 And voilà, we've just scheduled a very useful job!

 *footnote: you can also queue bare invocations (think `Task`-returning lambdas) with `AsyncJobScheduler.QueueAsyncInvocation(myTaskMethod);`*

 ### Events

 Of course, the jobs have a `WorkFinished` event. You can subscribe to this for handling the job once completed. In the
 case of a pooled job, this may be where you clean up the job and push it back your object pool.

 The static `AsyncJobScheduler` also offers some event handlers, such as `JobQueued`, `JobStarted`, and `JobFinished`.