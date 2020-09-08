using Core.Blob;
using Core.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace AzureRepositories.Blob
{
    public class RepositoryDataRepository:BlobDataRepository, IRepositoryDataRepository
    {
        public RepositoryDataRepository(IBlobStorage blobStorage, string container, string historyContainer) : base(blobStorage, container, historyContainer)
        {

        }
    }
}
