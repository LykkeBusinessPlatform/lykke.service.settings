using System.Threading.Tasks;
using Common.Log;
using Core.Blob;
using Core.KeyValue;
using Core.Lock;
using Core.ServiceToken;
using Core.Token;
using Core.User;
using Microsoft.AspNetCore.Mvc;
using Web.Code;
using Shared.Settings;
using Lykke.Common.Log;

namespace Web.Controllers
{
    [IgnoreLogAction]
    public class CheckListController : BaseController
    {
        private readonly ILog _log;
        private readonly IUserRepository _userRepository;
        private readonly AppSettings _appSettings;
        private readonly IJsonDataRepository _jsonDataRepository;
        private readonly ITokensRepository _tokensRepository;
        private readonly IServiceTokenRepository _serviceTokensRepository;
        private readonly IKeyValuesRepository _keyValuesRepository;
        private readonly ILockRepository _lockRepository;
        private readonly IAccessDataRepository _accessDataRepository;


        public CheckListController(ILogFactory logFactory, IUserRepository userRepository, AppSettings appSettings,
            IJsonDataRepository jsonDataRepository, ITokensRepository tokensRepository, IServiceTokenRepository serviceTokensRepository,
            IKeyValuesRepository keyValuesRepository, ILockRepository lockRepository, IAccessDataRepository accessDataRepository,
            IUserActionHistoryRepository userActionHistoryRepository) : base(userActionHistoryRepository)
        {
            _log = logFactory.CreateLog(this);
            _userRepository = userRepository;
            _appSettings = appSettings;
            _jsonDataRepository = jsonDataRepository;
            _tokensRepository = tokensRepository;
            _serviceTokensRepository = serviceTokensRepository;
            _keyValuesRepository = keyValuesRepository;
            _lockRepository = lockRepository;
            _accessDataRepository = accessDataRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Check()
        {
            ViewBag.Result = await SelfTest(_appSettings, _userRepository, _jsonDataRepository, _tokensRepository, _serviceTokensRepository,
                _keyValuesRepository, _lockRepository, _accessDataRepository, _log);
            return View();
        }
    }
}
