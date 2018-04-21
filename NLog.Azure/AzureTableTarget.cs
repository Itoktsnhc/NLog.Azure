using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using NLog.Azure.Entities.NLog.AzureStorage;
using NLog.Common;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

namespace NLog.Azure
{
    [Target("AzureTable")]
    public class AzureTableTarget : TargetWithLayout
    {
        private CloudStorageAccount _account;
        private CloudTableClient _client;

        [RequiredParameter] public String ConnectionString { get; set; }

        [RequiredParameter] public Layout TableName { get; set; }

        [RequiredParameter] public Layout PartitionKey { get; set; }


        protected override void InitializeTarget()
        {
            base.InitializeTarget();
            _account = CloudStorageAccount.Parse(ConnectionString);
            _client = _account.CreateCloudTableClient();
        }

        protected override void WriteAsyncThreadSafe(IList<AsyncLogEventInfo> logEvents)
        {
#if DEBUG
            Console.WriteLine($"{DateTime.Now} Trigged WriteAsyncThreadSafe {logEvents.Count}");
#endif
            WriteToTableAsync(logEvents.Select(s => GenerateLogDto(s.LogEvent)).ToList()).GetAwaiter().GetResult();
        }

        protected override void Write(LogEventInfo logEvent)
        {
#if DEBUG
            Console.WriteLine($"{DateTime.Now} Trigged Write");
#endif
            WriteToTableAsync(new List<AzureTableLogDto>
            {
                GenerateLogDto(logEvent)
            }).GetAwaiter().GetResult();
        }

        protected override void WriteAsyncThreadSafe(AsyncLogEventInfo logEvent)
        {
#if DEBUG
            Console.WriteLine($"{DateTime.Now} WriteAsyncThreadSafe ");
#endif
            WriteToTableAsync(new List<AzureTableLogDto>
            {
                GenerateLogDto(logEvent.LogEvent)
            }).GetAwaiter().GetResult();
        }

        private AzureTableLogDto GenerateLogDto(LogEventInfo logEvent)
        {
            return new AzureTableLogDto
            {
                TableRef = _client.GetTableReference(TableName.Render(logEvent)),
                LogEvent = logEvent
            };
        }

        private async Task WriteToTableAsync(IList<AzureTableLogDto> dtos)
        {
            var grouppedDtos = dtos.GroupBy(s => s.TableRef.Name);
            foreach (var grouppedDto in grouppedDtos)
            {
                var first = grouppedDto.First();
                await first.TableRef.CreateIfNotExistsAsync();
                foreach (var innerGroupedDtos in Helper.SplitList(grouppedDto.ToList(), 99))
                {
                    var batchOption = new TableBatchOperation();
                    foreach (var azureTableLogDto in innerGroupedDtos)
                    {
                        var tableItem = new TableLogEntity(PartitionKey.Render(azureTableLogDto.LogEvent),
                            azureTableLogDto.LogEvent);
                        batchOption.Add(TableOperation.InsertOrMerge(tableItem));
                    }

                    await first.TableRef.ExecuteBatchAsync(batchOption);
                }
            }
        }
    }
}