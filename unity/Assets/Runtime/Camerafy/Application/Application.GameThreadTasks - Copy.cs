using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Camerafy.Application
{
    /// <summary>
    /// GameThreadTasks can be run by run from any thread and will be ensured to be executed in Unitys 
    /// main gamethread. This is often required when busniess code is calling Unity code, which can only
    /// be accessed from main gamethread. GameThreadTasks can be awaited.
    /// </summary>

    public class GameThreadTask : INotifyCompletion
    {
        private SynchronizationContext SyncContext;
        private Action Task;

        public GameThreadTask(Action InTask, SynchronizationContext InSyncContext)
        {
            this.SyncContext = InSyncContext;
            this.Task = InTask;
        }

        public bool IsCompleted { get { return false; } }

        public void OnCompleted(Action continuation)
        {
            if (this.SyncContext != null)
            {
                this.SyncContext.Post(s =>
                {
                    try { ((GameThreadTask)s).Task(); } catch (Exception e) { Logger.Error(e); }
                    continuation();
                }, this);
            }
            else
            {
                try { this.Task(); } catch (Exception e) { Logger.Error(e); }
                continuation();
            }
        }

        public void GetResult() { }

        public GameThreadTask GetAwaiter() { return new GameThreadTask(this.Task, this.SyncContext); }
    }

    public class GameThreadTask<TResult> : INotifyCompletion
    {
        private SynchronizationContext SyncContext;
        private Func<TResult> Task;
        private TResult Result = default;

        public GameThreadTask(Func<TResult> InTask, SynchronizationContext InSyncContext)
        {
            this.SyncContext = InSyncContext;
            this.Task = InTask;
        }

        public bool IsCompleted { get { return false; } }

        public void OnCompleted(Action continuation)
        {
            if (this.SyncContext != null)
            {
                this.SyncContext.Post(async s =>
                {
                    try
                    {
                        var waiter = (GameThreadTask<TResult>)s;
                        waiter.Result = waiter.Task();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }

                    continuation();
                }, this);
            }
            else
            {
                try
                {
                    this.Result = this.Task();
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }

                continuation();
            }
        }

        public TResult GetResult() { return this.Result; }

        public GameThreadTask<TResult> GetAwaiter() { return new GameThreadTask<TResult>(this.Task, this.SyncContext); }
    }

    public class GameThreadTaskAsync : INotifyCompletion
    {
        private SynchronizationContext SyncContext;
        private Func<Task> Task;

        public GameThreadTaskAsync(Func<Task> InTask, SynchronizationContext InSyncContext)
        {
            this.SyncContext = InSyncContext;
            this.Task = InTask;
        }

        public bool IsCompleted { get { return false; } }

        public async void OnCompleted(Action continuation)
        {
            if (this.SyncContext != null)
            {
                this.SyncContext.Post(async s =>
                {
                    try
                    {
                        var waiter = (GameThreadTaskAsync)s;
                        await waiter.Task();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }

                    continuation();
                }, this);
            }
            else
            {
                try
                {
                    await this.Task();
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }

                continuation();
            }
        }

        public void GetResult() { }

        public GameThreadTaskAsync GetAwaiter() { return new GameThreadTaskAsync(this.Task, this.SyncContext); }
    }

    public class GameThreadTaskAsync<TResult> : INotifyCompletion
    {
        private SynchronizationContext SyncContext;
        private Func<Task<TResult>> Task;
        private TResult Result = default;

        public GameThreadTaskAsync(Func<Task<TResult>> InTask, SynchronizationContext InSyncContext)
        {
            this.SyncContext = InSyncContext;
            this.Task = InTask;
        }

        public bool IsCompleted { get { return false; } }

        public async void OnCompleted(Action continuation)
        {
            if (this.SyncContext != null)
            {
                this.SyncContext.Post(async s =>
                {
                    try
                    {
                        var waiter = (GameThreadTaskAsync<TResult>)s;
                        waiter.Result = await waiter.Task();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                    }

                    continuation();
                }, this);
            }
            else
            {
                try
                {
                    this.Result = await this.Task();
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }

                continuation();
            }
        }

        public TResult GetResult() { return this.Result; }

        public GameThreadTaskAsync<TResult> GetAwaiter() { return new GameThreadTaskAsync<TResult>(this.Task, this.SyncContext); }
    }


    public partial class Application
    {
        /// <summary>
        /// Holds the synchronisation context of the main game thread.
        /// </summary>
        private SynchronizationContext GamethreadSyncContext = null;

        /// <summary>
        /// Creates a new awaitable synchronous gamethread task that has no return value.
        /// </summary>
        /// <param name="InTask"></param>
        /// <returns></returns>
        public GameThreadTask CreateGamethreadTask(Action InTask) { return new GameThreadTask(InTask, this.GamethreadSyncContext); }

        /// <summary>
        /// Creates a new awaitable synchronous gamethread task that has a return value.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="InTask"></param>
        /// <returns></returns>
        public GameThreadTask<TResult> CreateGamethreadTask<TResult>(Func<TResult> InTask) { return new GameThreadTask<TResult>(InTask, this.GamethreadSyncContext); }

        /// <summary>
        /// Creates a new awaitable asynchronous gamethread task that has no return value.
        /// </summary>
        /// <param name="InTask"></param>
        /// <returns></returns>
        public GameThreadTaskAsync CreateGamethreadTask(Func<Task> InTask) { return new GameThreadTaskAsync(InTask, this.GamethreadSyncContext); }

        /// <summary>
        /// Creates a new awaitable asynchronous gamethread task that has a return value.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="InTask"></param>
        /// <returns></returns>
        public GameThreadTaskAsync<TResult> CreateGamethreadTask<TResult>(Func<Task<TResult>> InTask) { return new GameThreadTaskAsync<TResult>(InTask, this.GamethreadSyncContext); }

    }
}
