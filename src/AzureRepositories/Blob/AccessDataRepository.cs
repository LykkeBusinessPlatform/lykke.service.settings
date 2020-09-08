using Core.Blob;
using Core.Repositories;

namespace AzureRepositories.Blob
{
    public class AccessDataRepository : BlobDataRepository, IAccessDataRepository
    {
        public AccessDataRepository(IBlobStorage blobStorage, string container, string historyContainer, string file)
            : base(blobStorage, container, historyContainer, file)
        {
        }
    }
}
