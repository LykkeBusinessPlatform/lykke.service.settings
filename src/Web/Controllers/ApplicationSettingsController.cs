using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core.ApplicationSettings;
using Core.User;
using Lykke.Common.Log;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Core.Extensions;
using Web.Extensions;
using Web.Models;

namespace Web.Controllers
{
    [Authorize]
    public class ApplicationSettingsController : BaseController
    {
        private readonly ILog _log;
        private readonly IApplicationSettingsRepostiory _applicationSettingsRepostiory;

        public ApplicationSettingsController(ILogFactory logFactory, IApplicationSettingsRepostiory applicationSettingsRepostiory,
            IUserActionHistoryRepository userActionHistoryRepository)
            : base(userActionHistoryRepository)
        {
            _log = logFactory.CreateLog(this);
            _applicationSettingsRepostiory = applicationSettingsRepostiory;
        }

        [Route("/ApplicationSettings/Settings")]
        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            try
            {
                var data = await _applicationSettingsRepostiory.GetAsync();
                var model = new ApplicationSettingsModel
                {
                    ETag = data.ETag,
                    RowKey = data.RowKey,
                    AzureClientId = data.AzureClientId,
                    AzureRegionName = data.AzureRegionName,
                    AzureClientKey = data.AzureClientKey,
                    AzureTenantId = data.AzureTenantId,
                    AzureResourceGroupName = data.AzureResourceGroupName,
                    AzureStorageName = data.AzureStorageName,
                    AzureKeyName = data.AzureKeyName,
                    AzureSubscriptionId = data.AzureSubscriptionId,
                    AzureApiKey = data.AzureApiKey,
                    DefaultMongoDBConnStr = data.DefaultMongoDBConnStr,
                    DefaultRabbitMQConnStr = data.DefaultRabbitMQConnStr,
                    DefaultRedisConnStr = data.DefaultRedisConnStr
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return View(new ApplicationSettingsModel());
            }
        }

        [Route("/ApplicationSettings/SaveSettings")]
        [HttpPost]
        public async Task<IActionResult> SaveSettings(ApplicationSettingsModel model)
        {
            try
            {
                var settings = await _applicationSettingsRepostiory.GetAsync();
                if (settings == null)
                {
                    return new JsonResult(new
                    {
                        Result = UpdateSettingsStatus.NotFound
                    });
                }

                foreach (var property in model.GetType().GetProperties())
                {
                    var value = model.GetType().GetProperty(property.Name).GetValue(model, null);
                    if (value != null)
                    {
                        settings.GetType().GetProperty(property.Name).SetValue(settings, value);
                    }
                }

                await _applicationSettingsRepostiory.SaveApplicationSettings(settings);

                return new JsonResult(new
                {
                    Result = UpdateSettingsStatus.Ok,
                    Json = JsonConvert.SerializeObject(settings)
                });
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: model);
                return View(new ApplicationSettingsModel());
            }
        }
    }
}
