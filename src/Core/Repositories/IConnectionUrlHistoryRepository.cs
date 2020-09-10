using Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IConnectionUrlHistoryRepository
    {
        Task<(IEnumerable<IConnectionUrlHistory>, int)> GetPageAsync(int pageNum, int pageSize);
        Task<IEnumerable<IConnectionUrlHistory>> GetByRepositoryIdAsync(string repositoryId);
        Task SaveConnectionUrlHistoryAsync(
            string repositoryId,
            string ip,
            string userAgent);
    }
}
