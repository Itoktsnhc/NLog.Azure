using System;
using System.Linq;
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
            Logger = loggerFactory.CreateLogger("test");

            foreach (var index in Enumerable.Range(0, 10000)) Logger.LogInformation($"info : {index}");

            Console.WriteLine($"{DateTime.Now} Log Finished");
            Console.ReadLine();
        }
    }
}