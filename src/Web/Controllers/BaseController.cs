using System;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AzureRepositories.User;
using Common;
using Core.Blob;
using Core.KeyValue;
using Core.Lock;
using Core.ServiceToken;
using Core.Token;
using Core.User;
using Lykke.Common.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Web.Code;
using Web.Extensions;
using Shared.Settings;
using System.Net;
using Common.Log;
using Lykke.Common.Log;

namespace Web.Controllers
{
    public class BaseController : Controller
    {
        private readonly IUserActionHistoryRepository _userActionHistoryRepository;

        private ILog _log;
        private IJsonDataRepository _jsonDataRepository;
        private ITokensRepository _tokensRepository;
        private IServiceTokenRepository _serviceTokensRepository;
        private IKeyValuesRepository _keyValuesRepository;
        private ILockRepository _lockRepository;
        private IAccessDataRepository _accessDataRepository;
        private AppSettings _appSettings;

        protected IUserRepository _userRepository;

        public readonly bool IS_PRODUCTION = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToLower() == "production";

        #region Constatnts
        private const string API_KEY = "BU3Nkbkqg2HOo5sRJ8c";
        #endregion

        protected UserInfo UserInfo { get; private set; }

        public BaseController(IUserActionHistoryRepository userActionHistoryRepository)
        {
            _userActionHistoryRepository = userActionHistoryRepository;
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
                    await _userActionHistoryRepository.SaveUserActionHistoryAsync(new UserActionHistoryEntity
                    {
                        UserEmail = UserInfo.UserEmail,
                        ActionDate = DateTime.UtcNow,
                        ActionName = actionDescription.ActionName,
                        ControllerName = actionDescription.ControllerName,
                        ETag = "*",
                        IpAddress = UserInfo.Ip,
                        Params = filterContext.ActionArguments.Count > 0 ? JsonConvert.SerializeObject(filterContext.ActionArguments) : string.Empty,
                    });
                });
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        protected async Task<string> SelfTest(AppSettings appSettings, IUserRepository userRepository, IJsonDataRepository jsonDataRepository, ITokensRepository tokensRepository, IServiceTokenRepository serviceTokensRepository, IKeyValuesRepository keyValuesRepository, ILockRepository lockRepository, IAccessDataRepository accessDataRepository, ILog log)
        {
            _userRepository = userRepository;
            _log = log;
            _appSettings = appSettings;
            _jsonDataRepository = jsonDataRepository;
            _tokensRepository = tokensRepository;
            _serviceTokensRepository = serviceTokensRepository;
            _keyValuesRepository = keyValuesRepository;
            _lockRepository = lockRepository;
            _accessDataRepository = accessDataRepository;

            var sb = new StringBuilder();
            AddCaption(sb, "Self testing for the system.");
            AddHeader(sb, "Check Settings");

            if (TestSettings(sb))
            {
                AddHeader(sb, "Check DataBase connections");
                if (await TestDataBases(sb))
                {
                    AddSuccess(sb, "Self Testing successfully completed");
                }
            }

            return sb.ToString();
        }

        protected virtual void AddSuccess(StringBuilder sb, string selfTestingSuccessfulCompleted)
        {
            sb.AppendLine($"<div class='sfSuccess'>{selfTestingSuccessfulCompleted}</div>");
        }

        protected virtual void AddError(StringBuilder sb, string selfTestingError)
        {
            sb.AppendLine($"<div class='sfError'>{selfTestingError}</div>");
        }

        protected virtual void AddText(StringBuilder sb, string selfTestingText)
        {
            sb.AppendLine($"<div>{selfTestingText}</div>");
        }

        private async Task<bool> TestDataBases(StringBuilder sb)
        {
            return await TestUserRepository(sb) &&
                   await TestJsonDataRepository(sb) &&
                   await TestTokensRepository(sb) &&
                   await TestAccountTokenHistoryRepository(sb) &&
                   await TestServiceTokenRepository(sb) &&
                   await TestKeyValuesRepository(sb) &&
                   await TestLockRepository(sb) &&
                   await TestAccessDataRepository(sb);
        }

        private async Task<bool> TestAccessDataRepository(StringBuilder sb)
        {
            try
            {
                await _accessDataRepository.GetDataAsync();
            }
            catch (Exception e)
            {
                AddError(sb, $"The 'AccessDataRepository' throws an error {e}.");
                return true;
            }

            AddText(sb, "The 'AccessDataRepository' checked.");
            return true;
        }

        private async Task<bool> TestLockRepository(StringBuilder sb)
        {
            try
            {
                await _lockRepository.GetJsonPageLockAsync();
            }
            catch (Exception e)
            {
                AddError(sb, $"The 'LockRepository' throws an error {e}.");
                return true;
            }

            AddText(sb, "The 'LockRepository' checked.");
            return true;
        }

        private async Task<bool> TestKeyValuesRepository(StringBuilder sb)
        {
            try
            {
                await _keyValuesRepository.GetAsync();
            }
            catch (Exception e)
            {
                AddError(sb, $"The 'KeyValuesRepository' throws an error {e}.");
                return true;
            }

            AddText(sb, "The 'KeyValuesRepository' checked.");
            return true;
        }

        private async Task<bool> TestServiceTokenRepository(StringBuilder sb)
        {
            try
            {
                await _serviceTokensRepository.GetAllAsync();
            }
            catch (Exception e)
            {
                AddError(sb, $"The 'ServiceTokenRepository' throws an error {e}.");
                return true;
            }

            AddText(sb, "The 'ServiceTokenRepository' checked.");
            return true;
        }

        private async Task<bool> TestAccountTokenHistoryRepository(StringBuilder sb)
        {
            try
            {
                await _accessDataRepository.GetDataAsync();
            }
            catch (Exception e)
            {
                AddError(sb, $"The 'AccessDataRepository' throws an error {e}.");
                return true;
            }

            AddText(sb, "The 'AccessDataRepository' checked.");
            return true;
        }

        private async Task<bool> TestTokensRepository(StringBuilder sb)
        {
            try
            {
                await _tokensRepository.GetAllAsync();
            }
            catch (Exception e)
            {
                AddError(sb, $"The 'TokensRepository' throws an error {e}.");
                return true;
            }

            AddText(sb, "The 'TokensRepository' checked.");
            return true;
        }

        private async Task<bool> TestJsonDataRepository(StringBuilder sb)
        {
            try
            {
                await _jsonDataRepository.GetDataAsync();
            }
            catch (Exception e)
            {
                AddError(sb, $"The 'JsonDataRepository' throws an error {e}.");
                return true;
            }

            AddText(sb, "The 'JsonDataRepository' checked.");
            return true;
        }

        private async Task<bool> TestUserRepository(StringBuilder sb)
        {
            try
            {
                await _userRepository.GetUsers();
            }
            catch (Exception e)
            {
                AddError(sb, $"The 'UserRepository' throws an error {e}.");
                return true;
            }

            AddText(sb, "The 'UserRepository' checked.");
            return true;
        }

        private bool TestSettings(StringBuilder sb)
        {
            return !TestSettingString(sb, nameof(_appSettings.UserConnectionString), _appSettings.UserConnectionString) &&
                   !TestSettingString(sb, nameof(_appSettings.ConnectionString), _appSettings.ConnectionString) &&
                   !TestSettingString(sb, nameof(_appSettings.DefaultPassword), _appSettings.DefaultPassword) &&
                   !TestSettingString(sb, nameof(_appSettings.DefaultUserEmail), _appSettings.DefaultUserEmail) &&
                   !TestSettingString(sb, nameof(_appSettings.DefaultUserFirstName), _appSettings.DefaultUserFirstName) &&
                   !TestSettingString(sb, nameof(_appSettings.DefaultUserLastName), _appSettings.DefaultUserLastName) &&
                   !TestSettingString(sb, nameof(_appSettings.ApiClientId), _appSettings.ApiClientId) &&
                   TestSettingRx(sb, nameof(_appSettings.AvailableEmailsRegex), _appSettings.AvailableEmailsRegex);
        }

        private bool TestSettingRx(StringBuilder sb, string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                AddError(sb, $"Key '{key}' is empty");
                return false;
            }

            try
            {
                new Regex(value);
            }
            catch (Exception e)
            {
                AddError(sb, $"Key '{key}' has inncorrect value '{value}' and throws the following exception: {e}");
                return false;
            }

            return true;
        }

        private bool TestSettingString(StringBuilder sb, string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                AddError(sb, $"Key '{key}' is empty");
                return false;
            }

            AddText(sb, $"Setting '{key}' checked.");
            return true;
        }

        protected virtual void AddHeader(StringBuilder sb, string checkSettingsHeader)
        {
            sb.AppendLine($"<div class='sfHeader'>{checkSettingsHeader}</div>");
        }

        protected virtual void AddCaption(StringBuilder sb, string checkSettingsCaption)
        {
            sb.AppendLine($"<div class='sfCaption'>{checkSettingsCaption}</div>");
        }

        protected JsonResult JsonErrorValidationResult(string message, string field)
        {
            return new JsonResult(new { status = "ErrorValidation", msg = message, field = field });
        }

        protected JsonResult JsonErrorMessageResult(string message, string field)
        {
            return new JsonResult(new { status = "ErrorMessage", msg = message, field = field });
        }

        protected JsonResult JsonRequestResult(string div, string url, bool showLoading = false, object model = null)
        {
            if (model == null)
                return new JsonResult(new { div, refreshUrl = url, showLoading = showLoading });

            var modelAsString = model as string ?? model.ToUrlParamString();
            return new JsonResult(new { div, refreshUrl = url, prms = modelAsString, showLoading = showLoading });
        }
    }
}
