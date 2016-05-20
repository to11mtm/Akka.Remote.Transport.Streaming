//-----------------------------------------------------------------------
// <copyright file="TaskExtensions.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Akka.Remote.Transport.Streaming.Utils
{
    internal static class TaskExtensions
    {
        public static Task WithTimeout(this Task task, TimeSpan timeout)
        {
            if (timeout == Timeout.InfiniteTimeSpan)
                return task;

            CancellationTokenSource cancel = new CancellationTokenSource(timeout);

            var t = task.WithCancellation(cancel.Token);

            t.ContinueWithSynchronously((_, state) => ((CancellationTokenSource)state).Dispose(), cancel);

            return t;
        }

        public static Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
        {
            if (timeout == Timeout.InfiniteTimeSpan)
                return task;

            CancellationTokenSource cancel = new CancellationTokenSource(timeout);

            var t = task.WithCancellation(cancel.Token);

            t.ContinueWithSynchronously((_, state) => ((CancellationTokenSource)state).Dispose(), cancel);

            return t;
        }

        public static Task WithCancellation(this Task task, CancellationToken ct)
        {
            TaskCompletionSource<object> completion = new TaskCompletionSource<object>();

            if (ct.IsCancellationRequested)
            {
                completion.TrySetCanceled();
            }
            else
            {
                var cancelRegistration = ct.Register(state =>
                {
                    var c = (TaskCompletionSource<object>)state;
                    c.TrySetCanceled();
                }, completion);

                task.ContinueWith(t =>
                {
                    cancelRegistration.Dispose();

                    switch (t.Status)
                    {
                        case TaskStatus.RanToCompletion:
                            completion.TrySetResult(null);
                            break;

                        case TaskStatus.Canceled:
                            completion.TrySetCanceled();
                            break;

                        case TaskStatus.Faulted:
                            completion.TrySetException(t.Exception);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }, ct, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }

            return completion.Task;
        }

        public static Task<T> WithCancellation<T>(this Task<T> task, CancellationToken ct)
        {
            TaskCompletionSource<T> completion = new TaskCompletionSource<T>();

            if (ct.IsCancellationRequested)
            {
                completion.TrySetCanceled();
            }
            else
            {
                var cancelRegistration = ct.Register(state =>
                {
                    var c = (TaskCompletionSource<T>)state;
                    c.TrySetCanceled();
                }, completion);

                task.ContinueWith(t =>
                {
                    cancelRegistration.Dispose();

                    switch (t.Status)
                    {
                        case TaskStatus.RanToCompletion:
                            completion.TrySetResult(t.Result);
                            break;

                        case TaskStatus.Canceled:
                            completion.TrySetCanceled();
                            break;

                        case TaskStatus.Faulted:
                            completion.TrySetException(t.Exception);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }, ct, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }

            return completion.Task;
        }

        public static void IgnoreResult(this Task task)
        { }

        public static void IgnoreResult<T>(this Task<T> task)
        { }

        public static Task ContinueWithSynchronously(this Task task, Action<Task> continuationAction)
        {
            return task.ContinueWith(continuationAction, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        public static Task ContinueWithSynchronously(this Task task, Action<Task, object> continuationAction, object state)
        {
            return task.ContinueWith(continuationAction, state, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
    }
}
