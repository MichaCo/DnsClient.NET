namespace System.Threading.Tasks
{
    internal static class TaskExtensions
    {
        //public static async Task TimeoutAfter(this Task task, TimeSpan timeout, CancellationToken parentToken)
        //{
        //    using (var cts = new CancellationTokenSource())
        //    using (var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, parentToken))
        //    {
        //        var linkedToken = linkedSource.Token;
        //        int millis = timeout.TotalMilliseconds > int.MaxValue ? int.MaxValue : (int)timeout.TotalMilliseconds;

        //        if (task == await Task.WhenAny(task, Task.Delay(millis, linkedToken)).ConfigureAwait(false))
        //        {
        //            linkedSource.Cancel();
        //            await task.ConfigureAwait(false);
        //        }
        //        else
        //        {
        //            if (parentToken.IsCancellationRequested)
        //            {
        //                throw new TaskCanceledException();
        //            }

        //            throw new TimeoutException();
        //        }
        //    }
        //}

            // too slow
        ////public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
        ////{
        ////    int millis = timeout.TotalMilliseconds > int.MaxValue ? int.MaxValue : (int)timeout.TotalMilliseconds;

        ////    if (task == await Task.WhenAny(task, Task.Delay(millis)).ConfigureAwait(false))
        ////    {
        ////        return task.Result;
        ////    }
        ////    else
        ////    {
        ////        throw new TimeoutException();
        ////    }
        ////}

        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            
            using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
            {
                if (task != await Task.WhenAny(task, tcs.Task).ConfigureAwait(false))
                {
                    throw new OperationCanceledException(cancellationToken);
                }
            }

            return await task.ConfigureAwait(false);
        }
    }
}