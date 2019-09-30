using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureRepositories.KeyValue;
using Common.Log;
using Core.Blob;
using Core.Extensions;
using Core.KeyValue;
using Core.User;
using Lykke.Common.Log;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Services;
using Web.Models;

namespace Web.Controllers
{
    [Route("api/[controller]")]
    public class ApiOverrideController : BaseController
    {
        private readonly ILog _log;
        private readonly IKeyValuesRepository _keyValuesRepository;
        private readonly IKeyValueHistoryRepository _keyValueHistoryRepository;
        private readonly IRepositoryDataRepository _repositoryDataRepository;

        public ApiOverrideController(
            ILogFactory logFactory,
            IUserActionHistoryRepository userActionHistoryRepository,
            IKeyValuesRepository keyValuesRepository,
            IRepositoryDataRepository repositoryDataRepository,
            IKeyValueHistoryRepository keyValueHistoryRepository)
            : base(userActionHistoryRepository)
        {
            _log = logFactory.CreateLog(this);
            _keyValuesRepository = keyValuesRepository;
            _keyValueHistoryRepository = keyValueHistoryRepository;
            _repositoryDataRepository = repositoryDataRepository;
        }

        [HttpGet]
        public async Task<IEnumerable<IKeyValueEntity>> Get()
        {
            try
            {
                var keyValues = await _keyValuesRepository.GetKeyValuesAsync();
                return keyValues;
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return new List<IKeyValueEntity>();
            }
        }

        [HttpGet("{id}")]
        public async Task<IKeyValueEntity> Get(string id)
        {
            try
            {
                var keyValues = await _keyValuesRepository.GetAsync(x => x.RowKey == id);
                return keyValues.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: id);
                return new KeyValueEntity();
            }
        }

        [HttpGet("collectionKeys")]
        public async Task<IEnumerable<string>> GetKeysFromCollection()
        {
            try
            {
                var keyValues = await _keyValuesRepository.GetKeyValuesAsync();
                return keyValues.Select(x => x.RowKey);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return new List<string>();
            }
        }

        [HttpGet("blobKeys")]
        public async Task<IEnumerable<string>> GetKeysFromBlob()
        {
            try
            {
                var placeholders = await GetPlaceholdersList();
                return placeholders.Select(x => x.RowKey).Distinct();
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return new List<string>();
            }
        }

        [HttpPut]
        public async Task<ApiOverrideModel> Put([FromBody]KeyValueToUpdate entity)
        {
            try
            {
                var keyValues = await _keyValuesRepository.GetKeyValuesAsync();
                var keyValue = keyValues.FirstOrDefault(x => x.RowKey == entity.RowKey);
                if (keyValue == null)
                {
                    return new ApiOverrideModel
                    {
                        Status = UpdateSettingsStatus.NotFound
                    };
                }

                var duplicatedKeys = keyValues.Where(x => x.RowKey != keyValue.RowKey && x.Value == entity.Value).ToList();
                if (entity.Forced == false && IS_PRODUCTION)
                {
                    if (duplicatedKeys.Count > 0)
                    {
                        return new ApiOverrideModel
                        {
                            Status = UpdateSettingsStatus.HasDuplicated,
                            DuplicatedKeys = duplicatedKeys.Select(x => x.RowKey)
                        };
                    }
                }

                var keyValueEntity = new KeyValueEntity
                {
                    RowKey = keyValue.RowKey,
                    Value = entity.Value,
                    IsDuplicated = duplicatedKeys.Count > 0,
                    Override = keyValue.Override,
                    Types = keyValue.Types,
                    RepositoryNames = keyValue.RepositoryNames,
                    EmptyValueType = keyValue.EmptyValueType
                };
                var entitiesToUpload = new List<IKeyValueEntity>() { keyValueEntity };

                if (duplicatedKeys.Any())
                {
                    var duplicationsToUpload = duplicatedKeys.Where(x => !x.IsDuplicated.HasValue || !x.IsDuplicated.Value);
                    duplicationsToUpload.ToList().ForEach(item =>
                    {
                        item.IsDuplicated = true;
                        entitiesToUpload.Add(item);
                    });
                }

                var oldDuplications = keyValues.Where(x => x.RowKey != keyValue.RowKey && x.Value == keyValue.Value);
                if (oldDuplications.Count() == 1)
                {
                    var oldDuplication = oldDuplications.First();
                    oldDuplication.IsDuplicated = false;
                    entitiesToUpload.Add(oldDuplication);
                }

                var result = await _keyValuesRepository.UpdateKeyValueAsync(entitiesToUpload);
                string strObj = JsonConvert.SerializeObject(keyValues);
                await _keyValueHistoryRepository.SaveKeyValueHistoryAsync(
                    keyValueEntity?.RowKey,
                    keyValueEntity?.Value,
                    strObj,
                    UserInfo.UserEmail,
                    UserInfo.Ip);

                var updatedKeyValues = await _keyValuesRepository.GetKeyValuesAsync();
                return new ApiOverrideModel
                {
                    Status = result ? UpdateSettingsStatus.Ok : UpdateSettingsStatus.InternalError,
                    KeyValues = updatedKeyValues
                };
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: entity);
                return new ApiOverrideModel { Status = UpdateSettingsStatus.InternalError };
            }
        }

        [HttpDelete("{id}")]
        public async Task<bool> DeleteKeyValue(string id)
        {
            try
            {
                var keyValues = await _keyValuesRepository.GetKeyValuesAsync();
                var keyValue = keyValues.FirstOrDefault(x => x.RowKey == id);
                if (keyValue == null || string.IsNullOrWhiteSpace(keyValue.Value))
                    return false;

                List<IKeyValueEntity> keysToUpdate = new List<IKeyValueEntity>();
                // check for duplications. if duplicatedKeys == 1, then change isDuplicated property to false
                var duplicatedKeys = keyValues.Where(x => x.RowKey != keyValue.RowKey && x.Value == keyValue.Value).ToList();
                if (duplicatedKeys.Count == 1)
                {
                    var duplicatedKey = duplicatedKeys.First();
                    duplicatedKey.IsDuplicated = false;
                    keysToUpdate.Add(duplicatedKey);
                }

                keyValue.Value = null;
                // this key has no values, so it is not duplicated anymore
                keyValue.IsDuplicated = false;

                keysToUpdate.Add(keyValue);

                await _keyValuesRepository.ReplaceKeyValueAsync(keysToUpdate.ToArray());
                string strObj = JsonConvert.SerializeObject(keyValues);
                await _keyValueHistoryRepository.SaveKeyValueHistoryAsync(
                    keyValue?.RowKey,
                    keyValue?.Value,
                    strObj,
                    UserInfo.UserEmail,
                    UserInfo.Ip);
                return true;
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: id);
                return false;
            }
        }

        #region Private Methods
        private async Task<IEnumerable<IKeyValueEntity>> GetPlaceholdersList()
        {
            try
            {
                var jsonDatas = await _repositoryDataRepository.GetBlobFilesDataAsync();
                var jsonKeys = new List<IKeyValueEntity>();
                foreach (var jsonData in jsonDatas)
                {
                    jsonKeys.AddRange(jsonData.AsString().PlaceholderList());
                }
                return jsonKeys.Distinct().ToList();
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return new List<IKeyValueEntity>();
            }
        }
        #endregion
    }
}
