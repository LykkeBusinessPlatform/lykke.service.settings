using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core.Models;
using Core.Repositories;
using Lykke.Common.Extensions;
using Lykke.Common.Log;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Services.Extensions;
using Web.Code;
using Web.Extensions;

namespace Web.Controllers
{
    public class BaseController : Controller
    {
        private readonly IUserActionHistoryRepository _userActionHistoryRepository;

        protected readonly ILog _log;

        public readonly bool IS_PRODUCTION = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToLower() == "production";

        #region Constatnts
        private const string API_KEY = "BU3Nkbkqg2HOo5sRJ8c";
        #endregion

        protected UserInfo UserInfo { get; private set; }

        public BaseController(
            IUserActionHistoryRepository userActionHistoryRepository,
            ILogFactory logFactory)
        {
            _userActionHistoryRepository = userActionHistoryRepository;
            _log = logFactory.CreateLog(this);
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {
                UserInfo = new UserInfo
                {
                    Ip = Request.HttpContext.GetIp(),
                    UserEmail = Request.HttpContext.GetUserEmail() ?? "anonymous",
                    UserName = Request.HttpContext.GetUserName(),
                    IsAdmin = Request.HttpContext.IsAdmin()
                };

                var isApiRequest = HttpContext.Request.Path.StartsWithSegments(new Microsoft.AspNetCore.Http.PathString("/api"));
                if (isApiRequest)
                {
                    var apiKey = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                    if (apiKey == null || (apiKey != null && apiKey != API_KEY))
                    {
                        HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                        filterContext.Result = new JsonResult(new { status = (int)HttpStatusCode.Forbidden, message = "Incorrect Api Key" });
                        return;
                    }
                }

                if (!(filterContext.ActionDescriptor is ControllerActionDescriptor actionDescription))
                    return;

                if (actionDescription.ControllerTypeInfo.GetCustomAttribute(typeof(IgnoreLogActionAttribute)) != null ||
                    actionDescription.MethodInfo.GetCustomAttribute(typeof(IgnoreLogActionAttribute)) != null)
                    return;

                Task.Factory.StartNew(async () =>
                {
                    await _userActionHistoryRepository.SaveUserActionHistoryAsync(
                        new UserActionHistory
                        {
                            UserEmail = UserInfo.UserEmail,
                            ActionDate = DateTime.UtcNow,
                            ActionName = actionDescription.ActionName,
                            ControllerName = actionDescription.ControllerName,
                            IpAddress = UserInfo.Ip,
                            Params = filterContext.ActionArguments.Count > 0
                                ? JsonConvert.SerializeObject(filterContext.ActionArguments)
                                : string.Empty,
                        });
                });
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        protected JsonResult JsonErrorValidationResult(string message, string field)
        {
            return new JsonResult(new { status = "ErrorValidation", msg = message, field });
        }

        protected JsonResult JsonErrorMessageResult(string message, string field)
        {
            return new JsonResult(new { status = "ErrorMessage", msg = message, field });
        }

        protected JsonResult JsonRequestResult(string div, string url, bool showLoading = false, object model = null)
        {
            if (model == null)
                return new JsonResult(new { div, refreshUrl = url, showLoading });

            var modelAsString = model as string ?? model.ToUrlParamString();
            return new JsonResult(new { div, refreshUrl = url, prms = modelAsString, showLoading });
        }
    }
}
