using Core.Blob;

namespace AzureRepositories.Blob
{
    public class JsonDataRepository : BlobDataRepository, IJsonDataRepository
    {
        public JsonDataRepository(IBlobStorage blobStorage, string container, string historyContainer, string file) : base(blobStorage, container, historyContainer, file)
        {
        }
    }
}
