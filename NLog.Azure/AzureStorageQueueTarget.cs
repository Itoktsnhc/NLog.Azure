using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using NLog.Azure.Entities.NLog.AzureStorage;
using NLog.Common;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

namespace NLog.Azure
{
    [Target("AzureStorageQueue")]
    public class AzureStorageQueueTarget : TargetWithLayout
    {
        private CloudStorageAccount _account;
        private CloudQueueClient _client;

        [RequiredParameter] public String ConnectionString { get; set; }

        [RequiredParameter] public Layout QueueName { get; set; }

        protected override void InitializeTarget()
        {
            base.InitializeTarget();
            _account = CloudStorageAccount.Parse(ConnectionString);
            _client = _account.CreateCloudQueueClient();
        }

        protected override void WriteAsyncThreadSafe(IList<AsyncLogEventInfo> logEvents)
        {

            WriteToQueueAsync(logEvents.Select(s => GenerateLogDto(s.LogEvent)).ToList()).GetAwaiter().GetResult();
        }

        protected override void Write(LogEventInfo logEvent)
        {
            WriteToQueueAsync(new List<AzureQueueLogDto>
            {
                GenerateLogDto(logEvent)
            }).GetAwaiter().GetResult();
        }

        protected override void WriteAsyncThreadSafe(AsyncLogEventInfo logEvent)
        {
            WriteToQueueAsync(new List<AzureQueueLogDto>
            {
                GenerateLogDto(logEvent.LogEvent)
            }).GetAwaiter().GetResult();
        }

        private AzureQueueLogDto GenerateLogDto(LogEventInfo logEvent)
        {
            return new AzureQueueLogDto
            {
                QueueRef = _client.GetQueueReference(QueueName.Render(logEvent)),
                LogEvent = logEvent
            };
        }

        private async Task WriteToQueueAsync(IList<AzureQueueLogDto> dtos)
        {
            var grouppedDtos = dtos.GroupBy(s => s.QueueRef.Name);
            foreach (var grouppedDto in grouppedDtos)
            {
                var first = grouppedDto.First();
                await first.QueueRef.CreateIfNotExistsAsync();
                foreach (var azureQueueLogDto in dtos)
                    await first.QueueRef.AddMessageAsync(
                        new CloudQueueMessage(azureQueueLogDto.LogEvent.FormattedMessage));
            }
        }
    }
}