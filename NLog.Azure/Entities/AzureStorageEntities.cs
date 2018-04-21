using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Text;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;

namespace NLog.Azure.Entities
{
    namespace NLog.AzureStorage
    {
        public class AzureAppendLogDto
        {
            public CloudAppendBlob BlobRef { get; set; }
            public CloudBlobContainer ContainerRef { get; set; }
            public LogEventInfo LogEvent { get; set; }
        }
        public class AzureQueueLogDto
        {
            public CloudQueue QueueRef { get; set; }
            public LogEventInfo LogEvent { get; set; }
        }
        public class AzureTableLogDto
        {
            public CloudTable TableRef { get; set; }
            public LogEventInfo LogEvent { get; set; }
        }

        public class TableLogEntity : TableEntity
        {
            public String LoggerName { get; set; }
            public String Message { get; set; }
            public String MachineName { get; set; }
            public TableLogEntity(String partitionKey, LogEventInfo logEvent)
            {
                RowKey = $"{DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks:d19}__{ Guid.NewGuid():N}";
                PartitionKey = partitionKey;
                LoggerName = logEvent.LoggerName;
                Message = logEvent.FormattedMessage;
                MachineName = Environment.MachineName;
            }
        }

    }

}
