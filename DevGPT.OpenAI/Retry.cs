public class Retry
{
    public static T Run<T>(Func<T> func, int retries = 3, int initialWait = 200, int waitMultiplier = 5)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                return func();
            }
            catch (Exception)
            {
                attempt++;
                if (attempt > retries)
                    throw;
                Thread.Sleep((int)Math.Pow(attempt, waitMultiplier) * initialWait);
            }
        }
    }
}
