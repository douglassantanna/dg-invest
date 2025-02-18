using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace functions
{
    public class marketData
    {
        private readonly ILogger _logger;

        public marketData(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<marketData>();
        }

        [Function("marketData")]
        public void Run([TimerTrigger("*/1 * * * * *")] MyInfo myTimer)
        {
            var counter = 0;
            _logger.LogInformation($"C# Timer trigger function executed at: {counter++}");
        }
    }

    public class MyInfo
    {
        public MyScheduleStatus ScheduleStatus { get; set; }

        public bool IsPastDue { get; set; }
    }

    public class MyScheduleStatus
    {
        public DateTime Last { get; set; }

        public DateTime Next { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
