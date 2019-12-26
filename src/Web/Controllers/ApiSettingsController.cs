using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core.KeyValue;
using Lykke.Common.Log;
using Microsoft.AspNetCore.Mvc;
using Web.Extensions;

namespace Web.Controllers
{
    [Route("api/[controller]")]
    public class ApiSettingsController : Controller
    {
        private readonly IKeyValuesRepository _keyValuesRepository;
        private readonly ILog _log;

        public ApiSettingsController(ILogFactory logFactory, IKeyValuesRepository keyValuesRepository )
        {
            _log = logFactory.CreateLog(this);
            _keyValuesRepository = keyValuesRepository;
        }

        [HttpGet("AzureTableList")]
        public async Task<IActionResult> GetAzureTableConnStringsAsync()
        {
            try
            {
                var keyValues = await _keyValuesRepository.GetAsync(x =>  x.Types != null && x.Types.Contains(KeyValueTypes.AzureTableStorage));

                var tableConnStrList = keyValues.Select(x => x.Value).Distinct();

                return new JsonResult(tableConnStrList);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return null;
            }
        }

        [HttpGet("SqlTableList")]
        public async Task<IActionResult> GetSqlConnStringAsync()
        {
            try
            {
                var keyValues = await _keyValuesRepository.GetAsync(x => x.Types != null && x.Types.Contains(KeyValueTypes.SqlDB));

                var tableConnStrList = keyValues.Select(x => x.Value).Distinct();

                return new JsonResult(tableConnStrList);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return null;
            }
        }

        [HttpGet("SearchKeyValues")]
        public async Task<IActionResult> SearchKeyValuesAsync(string search)
        {
            if (string.IsNullOrWhiteSpace(search))
                throw new ArgumentNullException();

            try
            {
                search = search.ToLower();

                List<IKeyValueEntity> keyValues = new List<IKeyValueEntity>();
                IEnumerable<IKeyValueEntity> regularKeyValues = await _keyValuesRepository.GetKeyValuesAsync(i => FilterKeyValue(i, null, search));

                keyValues.AddRange(regularKeyValues);

                foreach (var keyValue in keyValues)
                {
                    if (!keyValue.UseNotTaggedValue.HasValue || !keyValue.UseNotTaggedValue.Value)
                        continue;

                    var originalKeyValue = keyValues.FirstOrDefault(k => k.RowKey == keyValue.RowKey.SubstringFromString(keyValue.Tag + "-"));
                    if (originalKeyValue != null)
                        keyValue.Value = originalKeyValue.Value;
                }

                return new JsonResult(keyValues);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return new JsonResult(new List<IKeyValueEntity>());
            }
        }

        private bool FilterKeyValue(IKeyValueEntity entity, string filter, string search)
        {
            if (!string.IsNullOrWhiteSpace(filter))
            {
                if (entity.RepositoryNames == null)
                    return false;

                if (!entity.RepositoryNames.Select(repo => repo.ToLower()).Contains(filter))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                if (entity.RowKey.ToLower().Contains(search)
                    || !string.IsNullOrWhiteSpace(entity.Value) && entity.Value.ToLower().Contains(search)
                    || entity.Override != null && string.Join("", entity.Override.Select(x => x.Value?.ToLower() ?? string.Empty)).Contains(search))
                    return true;
                return false;
            }

            return true;
        }
    }
}