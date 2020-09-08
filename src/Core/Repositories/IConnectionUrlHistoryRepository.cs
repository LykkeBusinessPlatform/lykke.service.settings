using Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IConnectionUrlHistoryRepository
    {
        Task<IConnectionUrlHistory> GetAsync(string connectionUrlHistoryId);
        Task<IEnumerable<IConnectionUrlHistory>> GetAllAsync();
        Task<IEnumerable<IConnectionUrlHistory>> GetAllAsync(Func<IConnectionUrlHistory, bool> filter);
        Task SaveConnectionUrlHistory(IConnectionUrlHistory entity);
    }
}
