using Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IRoleRepository
    {
        Task<IRoleEntity> GetAsync(string roleId);
        Task<IEnumerable<IRoleEntity>> GetAllAsync();
        Task<IEnumerable<IRoleEntity>> GetAllAsync(Func<IRoleEntity, bool> filter);
        Task SaveAsync(IRoleEntity entity);
        Task RemoveAsync(string roleId);
    }
}
