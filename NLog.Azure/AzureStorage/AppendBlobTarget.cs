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

namespace NLog.Azure.AzureStorage
{
    [Target("AzureAppendBlob")]
    public class AppendBlobTarget : TargetWithLayout
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
            Console.WriteLine($"{DateTime.Now} Trigged Flush {logEvents.Count}");
            if (!logEvents.Any() || _client == null)
                return;

            var appendList = (from asyncLogEventInfo in logEvents
                              select asyncLogEventInfo.LogEvent
                into logEvent
                              let blobName = Path.Combine(SubFolderPath.Render(logEvent), BlobName.Render(logEvent))
                              let containerRef = _client.GetContainerReference(Container.Render(logEvent))
                              let blobRef = containerRef.GetAppendBlobReference(blobName)
                              select new AzureLogDto()
                              {
                                  LogEvent = logEvent,
                                  BlobRef = blobRef,
                                  ContainerRef = containerRef,
                              }).ToList();
            WriteToAppendBlobAsync(appendList).GetAwaiter().GetResult();
        }
        private async Task WriteToAppendBlobAsync(IList<AzureLogDto> logList)
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
