using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core.KeyValue;
using Core.Networks;
using Core.User;
using Lykke.Common.Extensions;
using Lykke.Common.Log;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Web.Models;

namespace Web.Controllers
{
    [Authorize]
    [Route("networks")]
    public class NetworksController : BaseController
    {
        private readonly ILog _log;
        private readonly INetworkRepository _networkRepository;
        private readonly IKeyValuesRepository _keyValuesRepository;

        public NetworksController(
            ILogFactory logFactory,
            INetworkRepository networkRepository,
            IKeyValuesRepository keyValuesRepository,
            IUserActionHistoryRepository userActionHistoryRepository
            ) : base(userActionHistoryRepository)
        {
            _log = logFactory.CreateLog(this);
            _networkRepository = networkRepository;
            _keyValuesRepository = keyValuesRepository;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> List()
        {
            try
            {
                var networks = await _networkRepository.GetAllAsync();
                return PartialView("List", networks);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return PartialView("List", new Network[] { });
            }
        }

        [HttpPost]
        [Route("add")]
        public async Task<IActionResult> AddNetwork(Network model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Name))
                    return JsonErrorValidationResult("Network name is required", "#network_name");

                if (string.IsNullOrEmpty(model.Ip))
                    return JsonErrorValidationResult("Ip range is required", "#network_ip");

                if (!model.IsValidIps())
                    return JsonErrorValidationResult("Wrong ip", "#network_ip");

                await _networkRepository.AddAsync(model);

                return JsonRequestResult(".editItems", Url.Action("List"));
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: model);
                return JsonRequestResult(".editItems", Url.Action("List"));
            }
        }

        [HttpPost]
        [Route("update")]
        public async Task<IActionResult> UpdateNetwork(Network model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Ip))
                    return JsonErrorValidationResult("Ip range is required", $"#{model.Id}");

                if (!model.IsValidIps())
                    return JsonErrorValidationResult("Wrong ip", $"#{model.Id}");

                await _networkRepository.UpdateAsync(model);

                return JsonRequestResult(".editItems", Url.Action("List"));
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: model);
                return JsonRequestResult(".editItems", Url.Action("List"));
            }
        }

        [HttpPost]
        [Route("delete")]
        public async Task<IActionResult> DeleteNetwork(string id)
        {
            try
            {
                if (!string.IsNullOrEmpty(id))
                {
                    await _networkRepository.DeleteAsync(id);
                    await _keyValuesRepository.RemoveNetworkOverridesAsync(id);
                }

                return JsonRequestResult(".editItems", Url.Action("List"));
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: id);
                return JsonRequestResult(".editItems", Url.Action("List"));
            }
        }
    }
}
