using Microsoft.WindowsAzure.Storage.Blob;
namespace NLog.Azure.Entities
{
    namespace NLog.AzureStorage
    {
        public class AzureLogDto
        {
            public CloudAppendBlob BlobRef { get; set; }
            public CloudBlobContainer ContainerRef { get; set; }
            public LogEventInfo LogEvent { get; set; }
        }
    }

}
