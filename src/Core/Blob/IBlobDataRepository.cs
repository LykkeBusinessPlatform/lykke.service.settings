using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Blob
{
    public interface IBlobDataRepository
    {
        Task<string> GetDataAsync(string file = null);
        Task<Tuple<string, string>> GetDataWithMetaAsync(string file = null);
        Task UpdateBlobAsync(string json, string userName, string ipAddress, string file = null);
        string GetETag(string file = null);
        Task<string> GetLastModified(string file = null);
        Task DelBlobAsync(string file = null);
        Task<IEnumerable<AzureBlobResult>> GetBlobFilesDataAsync();
        Task<List<string>> GetExistingFileNames();
    }
}
