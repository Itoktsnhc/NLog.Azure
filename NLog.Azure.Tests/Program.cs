using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace NLog.Azure.Tests
{
    class Program
    {
        public static Microsoft.Extensions.Logging.ILogger _logger;
        static void Main(string[] args)
        {

            var loggerFactory = new LoggerFactory().AddNLog();
            _logger = loggerFactory.CreateLogger("test");

            foreach (var index in Enumerable.Range(0, 10000))
            {
                _logger.LogInformation($"info : {index}");
            }

            Console.WriteLine($"{DateTime.Now} Log Finished");
            Console.ReadLine();
        }
    }
}
