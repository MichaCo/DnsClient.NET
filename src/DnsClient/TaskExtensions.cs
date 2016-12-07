namespace System.Threading.Tasks
{
    internal static class TaskExtensions
    {
        public static async Task TimeoutAfter(this Task task, TimeSpan timeout)
        {
            var cts = new CancellationTokenSource();

            if (task == await Task.WhenAny(task, Task.Delay((int)timeout.TotalMilliseconds, cts.Token)).ConfigureAwait(false))
            {
                cts.Cancel();
                await task.ConfigureAwait(false);
            }
            else
            {
                throw new TimeoutException();
            }
        }

        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            var cts = new CancellationTokenSource();

            if (task == await Task.WhenAny(task, Task.Delay((int)timeout.TotalMilliseconds, cts.Token)).ConfigureAwait(false))
            {
                cts.Cancel();
                return await task.ConfigureAwait(false);
            }
            else
            {
                throw new TimeoutException();
            }
        }
    }
}