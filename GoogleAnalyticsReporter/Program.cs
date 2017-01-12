using System;

namespace GoogleAnalyticsReporter
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                AnalyticsReporter.Run().Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("ERROR: " + e.Message);
                }
            }
        }
    }
}
