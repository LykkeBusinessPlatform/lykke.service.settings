using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AzureRepositories.Extensions;
using Core.Blob;
using Core.Models;
using Core.Repositories;

namespace AzureRepositories.Blob
{
    public class BlobDataRepository : IBlobDataRepository
    {
        private readonly IBlobStorage _blobStorage;
        private readonly string _container;
        private readonly string _historyContainer;
        private readonly string _file;

        public BlobDataRepository(IBlobStorage blobStorage, string container, string historyContainer, string file)
        {
            _blobStorage = blobStorage;
            _container = container;
            _historyContainer = historyContainer;
            _file = file;
        }

        public BlobDataRepository(IBlobStorage blobStorage, string container, string historyContainer)
        {
            _blobStorage = blobStorage;
            _container = container;
            _historyContainer = historyContainer;
        }

        public async Task<string> GetDataAsync(string file = null)
        {
            var fileName = GetFileName(file);
            try
            {
                var result = await _blobStorage.GetAsync(_container, fileName);
                return result.AsString();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public async Task UpdateBlobAsync(string json, string userName, string ipAddress, string file = null)
        {
            var fileName = GetFileName(file);
            var data = Encoding.UTF8.GetBytes(json);
            await _blobStorage.SaveBlobAsync(_container, fileName, data);
            if (!string.IsNullOrEmpty(_historyContainer))
                await _blobStorage.SaveBlobAsync(
                    _historyContainer,
                    $"{fileName}_{DateTime.UtcNow.StorageString()}_{userName}_{ipAddress}",
                    data);
        }

        public async Task<List<string>> GetExistingFileNamesAsync()
        {
            return await _blobStorage.GetExistingFileNames(_container);
        }

        public Task<bool> ExistsAsync(string file = null)
        {
            var fileName = GetFileName(file);
            return _blobStorage.HasBlobAsync(_container, fileName);
        }

        public async Task DelBlobAsync(string file = null)
        {
            try
            {
                await _blobStorage.DelBlobAsync(_container, file);
            }
            catch (Exception)
            {
            }
        }

        public async Task<IEnumerable<AzureBlobResult>> GetBlobFilesDataAsync()
        {
            return await _blobStorage.GetBlobFilesDataAsync(_container);
        }

        private string GetFileName(string file)
        {
            return string.IsNullOrWhiteSpace(file) ? _file : file;
        }
    }
}
