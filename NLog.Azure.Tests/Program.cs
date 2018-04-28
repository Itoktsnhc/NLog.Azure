using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace NLog.Azure.Tests
{
    internal class Program
    {
        public static Microsoft.Extensions.Logging.ILogger Logger;

        private static void Main()
        {
            var loggerFactory = new LoggerFactory().AddNLog();
            Logger = loggerFactory.CreateLogger<Program>();
            foreach (var i in Enumerable.Range(0, 1000))
            {
                Logger.LogInformation($"Info Logged{i}");
            }
            MutiThreadLogToTarget();

            Console.ReadLine();
        }

        private static void MutiThreadLogToTarget()
        {
            var task1 = Task.Run(() =>
            {
                foreach (var index in Enumerable.Range(0, 12)) Logger.LogInformation($"task1 info : {index}");
            });
            var task2 = Task.Run(() =>
            {
                foreach (var index in Enumerable.Range(0, 12)) Logger.LogInformation($"task2 info : {index}");
            });
            Task.WaitAll(task1, task2);
            Console.WriteLine($"{DateTime.Now} Log Finished");
        }
    }
}