using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using NLog.Azure.Entities.NLog.AzureStorage;
using NLog.Common;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

namespace NLog.Azure
{
    [Target("AzureEventHub")]
    public class AzureEventHubTarget : TargetWithLayout
    {
        private EventHubClient _client;

        [RequiredParameter]
        public String ConnectionString { get; set; }

        protected override void InitializeTarget()
        {
            base.InitializeTarget();
            _client = EventHubClient.CreateFromConnectionString(ConnectionString);
        }

        protected override void WriteAsyncThreadSafe(IList<AsyncLogEventInfo> logEvents)
        {
#if DEBUG
            Console.WriteLine($"{DateTime.Now} Trigged WriteAsyncThreadSafe {logEvents.Count}");
#endif

            PushBatchToEventHubAsync(logEvents).ConfigureAwait(false).GetAwaiter().GetResult();

        }

        protected override void Write(LogEventInfo logEvent)
        {
#if DEBUG
            Console.WriteLine($"{DateTime.Now} Trigged Write");
#endif
            PushToEventHubAsync(logEvent).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        protected override void WriteAsyncThreadSafe(AsyncLogEventInfo logEvent)
        {
#if DEBUG
            Console.WriteLine($"{DateTime.Now} WriteAsyncThreadSafe ");
#endif
            PushToEventHubAsync(logEvent.LogEvent).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private async Task PushToEventHubAsync(LogEventInfo logEvent)
        {

            await _client.SendAsync(new EventData(Encoding.UTF8.GetBytes(logEvent.FormattedMessage)));
        }

        private async Task PushBatchToEventHubAsync(IList<AsyncLogEventInfo> logEvents)
        {
            var batch = _client.CreateBatch();
            foreach (var logEvent in logEvents)
            {
                batch.TryAdd(new EventData(Encoding.UTF8.GetBytes(logEvent.LogEvent.FormattedMessage)));
            }
            await _client.SendAsync(batch);
        }


    }
}
