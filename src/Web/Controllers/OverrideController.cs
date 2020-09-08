using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Core.Entities;
using Core.Models;
using Core.Repositories;
using Lykke.Common.Log;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Web.Models;

namespace Web.Controllers
{
    [Authorize]
    [Route("override")]
    public class OverrideController : BaseController
    {
        private readonly INetworkRepository _networkRepository;
        private readonly IKeyValuesRepository _keyValuesRepository;
        private readonly IKeyValueHistoryRepository _keyValueHistoryRepository;

        public OverrideController(
            ILogFactory logFactory,
            INetworkRepository networkRepository,
            IKeyValuesRepository keyValuesRepository,
            IKeyValueHistoryRepository keyValueHistoryRepository,
            IUserActionHistoryRepository userActionHistoryRepository)
            : base(userActionHistoryRepository, logFactory)
        {
            _networkRepository = networkRepository;
            _keyValuesRepository = keyValuesRepository;
            _keyValueHistoryRepository = keyValueHistoryRepository;
        }

        [HttpPost]
        public async Task<IActionResult> List(string key)
        {
            try
            {
                var networks = await _networkRepository.GetAllAsync();
                var keyValue = await _keyValuesRepository.GetKeyValueAsync(key) ?? new KeyValue();

                if (keyValue.Override == null)
                    keyValue.Override = Array.Empty<OverrideValue>();

                var model = new OverridesModel
                {
                    KeyValue = keyValue,
                    Networks = networks,
                    AvailableNetworks = networks
                        .Where(item => keyValue.Override.All(o => o.NetworkId != item.Id))
                        .Select(n => new SelectListItem { Text = n.Name, Value = n.Id })
                        .OrderBy(item => item.Text)
                        .ToArray()
                };

                return PartialView("List", model);
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: key);
                return PartialView("List", new OverridesModel());
            }
        }

        [HttpPost]
        [Route("add")]
        public async Task<IActionResult> AddOverride(OverrideValueModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Value))
                    return JsonErrorValidationResult("Value is required", "#override_value");

                var overrideValue = new OverrideValue
                {
                    NetworkId = model.NetworkId,
                    Value = model.Value
                };

                var keyValue = await _keyValuesRepository.GetKeyValueAsync(model.Key);

                if (keyValue.Override == null)
                    keyValue.Override = new[] { overrideValue };
                else
                {
                    var list = keyValue.Override.ToList();
                    list.Add(overrideValue);
                    keyValue.Override = list.ToArray();
                }

                await _keyValuesRepository.UpdateKeyValueAsync(new[] { keyValue });

                await AddToHistoryAsync(keyValue);

                return JsonRequestResult("#overrideValues", Url.Action("List"), false, new { key = model.Key });
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: model);
                return JsonRequestResult("#overrideValues", Url.Action("List"), false, new { key = String.Empty });
            }
        }

        [HttpPost]
        [Route("update")]
        public async Task<IActionResult> UpdateOverride(OverrideValueModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Value))
                    return JsonErrorValidationResult("Value is required", $"#{model.NetworkId}");

                var keyValue = await _keyValuesRepository.GetKeyValueAsync(model.Key);

                var existingValue = keyValue.Override?.FirstOrDefault(item => item.NetworkId == model.NetworkId);

                if (existingValue != null)
                    existingValue.Value = model.Value;

                await _keyValuesRepository.UpdateKeyValueAsync(new[] { keyValue });

                await AddToHistoryAsync(keyValue);

                return JsonRequestResult("#overrideValues", Url.Action("List"), false, new { key = model.Key });
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: model);
                return JsonRequestResult("#overrideValues", Url.Action("List"), false, new { key = String.Empty });
            }
        }

        [HttpPost]
        [Route("delete")]
        public async Task<IActionResult> DeleteOverride(OverrideValueModel model)
        {
            try
            {
                var keyValue = await _keyValuesRepository.GetKeyValueAsync(model.Key);

                var list = keyValue.Override?.ToList();

                var overrideValue = list?.FirstOrDefault(item => item.NetworkId == model.NetworkId);

                if (overrideValue != null)
                {
                    list.Remove(overrideValue);
                    keyValue.Override = list.ToArray();
                    await _keyValuesRepository.UpdateKeyValueAsync(new[] { keyValue });
                }

                await AddToHistoryAsync(keyValue);

                return JsonRequestResult("#overrideValues", Url.Action("List"), false, new { key = model.Key });
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: model);
                return JsonRequestResult("#overrideValues", Url.Action("List"), false, new { key = String.Empty });
            }
        }

        private async Task AddToHistoryAsync(IKeyValueEntity keyValue)
        {
            try
            {
                var keyValues = (await _keyValuesRepository.GetKeyValuesAsync(null, keyValue.RepositoryId)).ToList();

                await _keyValueHistoryRepository.SaveKeyValueOverrideHistoryAsync(
                    keyValue.RowKey,
                    keyValue.Override.ToArray().ToJson(),
                    keyValues.ToJson(),
                    UserInfo.UserEmail,
                    UserInfo.Ip);
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: keyValue);
                return;
            }
        }
    }
}
