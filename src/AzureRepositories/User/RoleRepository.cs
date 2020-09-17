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
            return list;
        }

        public async Task SaveAsync(IRoleEntity roleEntity)
        {
            if (roleEntity is RoleEntity role)
            {
                role.PartitionKey = RoleEntity.GeneratePartitionKey();
                role.RowKey = roleEntity.RoleId;
            }
            else
            {
                var pk = RoleEntity.GeneratePartitionKey();
                var rk = RoleEntity.GenerateRowKey(roleEntity.RoleId);
                role = await _tableStorage.GetDataAsync(pk, rk)
                    ?? new RoleEntity { PartitionKey = pk, RowKey = rk };

                role.RoleId = roleEntity.RoleId;
                role.Name = roleEntity.Name;
                role.KeyValues = roleEntity.KeyValues;
            }

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
