using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using Core.Entities;
using Core.Repositories;

namespace AzureRepositories.User
{
    public class RoleRepository:IRoleRepository
    {
        private readonly INoSQLTableStorage<RoleEntity> _tableStorage;

        public RoleRepository(INoSQLTableStorage<RoleEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IRoleEntity> GetAsync(string roleId)
        {
            var pk = RoleEntity.GeneratePartitionKey();
            var rk = RoleEntity.GenerateRowKey(roleId);

            return await _tableStorage.GetDataAsync(pk, rk);
        }

        public async Task<IEnumerable<IRoleEntity>> GetAllAsync()
        {
            var pk = RoleEntity.GeneratePartitionKey();
            return await _tableStorage.GetDataAsync(pk);
        }

        public async Task<IEnumerable<IRoleEntity>> GetAllAsync(Func<IRoleEntity, bool> filter)
        {
            var pk = RoleEntity.GeneratePartitionKey();
            var list = await _tableStorage.GetDataAsync(pk, filter: filter);
            return list as IEnumerable<IRoleEntity>;
        }

        public async Task SaveAsync(IRoleEntity roleEntity)
        {
            if (!(roleEntity is RoleEntity role))
            {
                role = (RoleEntity)await GetAsync(roleEntity.RowKey) ?? new RoleEntity();

                role.ETag = roleEntity.ETag;
                role.Name = roleEntity.Name;
                role.KeyValues = roleEntity.KeyValues;
            }
            role.PartitionKey = RoleEntity.GeneratePartitionKey();
            role.RowKey = roleEntity.RowKey;
            await _tableStorage.InsertOrMergeAsync(role);
        }

        public async Task RemoveAsync(string roleId)
        {
            var role = await GetAsync(roleId);
            if (role != null)
            {
                await _tableStorage.DeleteAsync(role as RoleEntity);
            }
        }
    }
}
