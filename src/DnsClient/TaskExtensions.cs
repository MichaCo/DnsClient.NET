namespace System.Threading.Tasks
{
    internal static class TaskExtensions
    {
        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();

            using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
            {
                if (task != await Task.WhenAny(task, tcs.Task).ConfigureAwait(false))
                {
                    // observe the exception to avoid "System.AggregateException: A Task's exception(s) were
                    // not observed either by Waiting on the Task or accessing its Exception property."
                    _ = task.ContinueWith(t => t.Exception);
                    throw new OperationCanceledException(cancellationToken);
                }
            }

            return await task.ConfigureAwait(false);
        }
    }
}
