using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using NLog.Azure.Entities.NLog.AzureStorage;
using NLog.Common;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

namespace NLog.Azure
{
    [Target("AzureAppendBlob")]
    public class AzureAppendBlobTarget : TargetWithLayout
    {
        private CloudBlobClient _client;
        private CloudStorageAccount _account;

        [RequiredParameter]
        public String ConnectionString { get; set; }
        [RequiredParameter]
        public Layout Container { get; set; }
        [RequiredParameter]
        public Layout SubFolderPath { get; set; }
        [RequiredParameter]
        public Layout BlobName { get; set; }

        protected override void InitializeTarget()
        {
            base.InitializeTarget();
            _account = CloudStorageAccount.Parse(ConnectionString);
            _client = _account.CreateCloudBlobClient();
        }
        protected override void WriteAsyncThreadSafe(IList<AsyncLogEventInfo> logEvents)
        {
#if DEBUG

            Console.WriteLine($"{DateTime.Now} Trigged WriteAsyncThreadSafe {logEvents.Count}");
#endif
            if (!logEvents.Any() || _client == null)
                return;

            var appendList = (from asyncLogEventInfo in logEvents
                              select asyncLogEventInfo.LogEvent
                into logEvent
                              select GenerateLogDto(logEvent)).ToList();
            WriteToAppendBlobAsync(appendList).GetAwaiter().GetResult();
        }
        protected override void Write(LogEventInfo logEvent)
        {
#if DEBUG

            Console.WriteLine($"{DateTime.Now} Trigged Write");
#endif
            var azureLogDto = GenerateLogDto(logEvent);
            WriteToAppendBlobAsync(new List<AzureAppendLogDto>()
            {
                azureLogDto
            }).GetAwaiter().GetResult();
        }
        protected override void WriteAsyncThreadSafe(AsyncLogEventInfo logEvent)
        {
#if DEBUG
            Console.WriteLine($"{DateTime.Now} WriteAsyncThreadSafe ");
#endif
            var azureLogDto = GenerateLogDto(logEvent.LogEvent);
            WriteToAppendBlobAsync(new List<AzureAppendLogDto>()
            {
                azureLogDto
            }).GetAwaiter().GetResult();
        }
        private AzureAppendLogDto GenerateLogDto(LogEventInfo logEvent)
        {
            var azureLogDto =
                new AzureAppendLogDto { ContainerRef = _client.GetContainerReference(Container.Render(logEvent)) };
            azureLogDto.BlobRef = azureLogDto.ContainerRef.GetAppendBlobReference(Path.Combine(SubFolderPath.Render(logEvent), BlobName.Render(logEvent)));
            azureLogDto.LogEvent = logEvent;
            return azureLogDto;
        }
        private async Task WriteToAppendBlobAsync(IList<AzureAppendLogDto> logList)
        {
            foreach (var containerGroup in logList.GroupBy(s => s.ContainerRef.Name))
            {
                var containerFirst = containerGroup.First();
                await containerFirst.ContainerRef.CreateIfNotExistsAsync();

                foreach (var blobGroup in containerGroup.GroupBy(s => s.BlobRef.Name))
                {
                    var sb = new StringBuilder();
                    var blobFirst = blobGroup.First();
                    if (!await blobFirst.BlobRef.ExistsAsync())
                    {
                        blobFirst.BlobRef.Properties.ContentType = "text/plain";
                        await blobFirst.BlobRef.CreateOrReplaceAsync(AccessCondition.GenerateEmptyCondition(), null, null);
                        await blobFirst.BlobRef.SetPropertiesAsync();
                    }
                    foreach (var azureLogDto in blobGroup)
                    {
                        sb.AppendLine(azureLogDto.LogEvent.FormattedMessage);
                    }
                    using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString())))
                    {
                        await blobFirst.BlobRef.AppendBlockAsync(ms);
                    }
                }
            }
        }
    }
}
