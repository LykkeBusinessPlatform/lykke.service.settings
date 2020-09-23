using Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IBlobDataRepository
    {
        Task<string> GetDataAsync(string file = null);
        Task UpdateBlobAsync(string json, string userName, string ipAddress, string file = null);
        Task DelBlobAsync(string file = null);
        Task<bool> ExistsAsync(string file = null);
        Task<IEnumerable<BlobResult>> GetBlobFilesDataAsync();
        Task<List<string>> GetExistingFileNamesAsync();
    }
}
