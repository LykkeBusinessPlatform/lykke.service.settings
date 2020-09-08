using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Core.Blob;
using Core.KeyValue;
using Core.Lock;
using Core.ServiceToken;
using Core.Token;
using Core.User;
using Web.Settings;

namespace Web.Code
{
    public class SelfTestService
    {
        private IJsonDataRepository _jsonDataRepository;
        private ITokensRepository _tokensRepository;
        private IServiceTokenRepository _serviceTokensRepository;
        private IKeyValuesRepository _keyValuesRepository;
        private ILockRepository _lockRepository;
        private IAccessDataRepository _accessDataRepository;
        private AppSettings _appSettings;

        private IUserRepository _userRepository;

        public SelfTestService(
            AppSettings appSettings,
            IUserRepository userRepository,
            IJsonDataRepository jsonDataRepository,
            ITokensRepository tokensRepository,
            IServiceTokenRepository serviceTokensRepository,
            IKeyValuesRepository keyValuesRepository,
            ILockRepository lockRepository,
            IAccessDataRepository accessDataRepository)
        {
            _userRepository = userRepository;
            _appSettings = appSettings;
            _jsonDataRepository = jsonDataRepository;
            _tokensRepository = tokensRepository;
            _serviceTokensRepository = serviceTokensRepository;
            _keyValuesRepository = keyValuesRepository;
            _lockRepository = lockRepository;
            _accessDataRepository = accessDataRepository;
        }

        public async Task SelfTestAsync()
        {
            TestSettings();

            await TestDatabaseAsync();
        }

        private Task TestDatabaseAsync()
        {
            var tasks = new List<Task>
            {
                _jsonDataRepository.ExistsAsync(),
                _accessDataRepository.ExistsAsync(),
                _accessDataRepository.ExistsAsync(),

                _userRepository.GetTopUserRecordAsync(),
                _keyValuesRepository.GetTopRecordAsync(),
                _tokensRepository.GetTopRecordAsync(),
                _serviceTokensRepository.GetTopRecordAsync(),
                _lockRepository.GetJsonPageLockAsync(),
            };

            return Task.WhenAll(tasks);
        }

        private void TestSettings()
        {
            TestSettingString(nameof(_appSettings.Db.UserConnectionString), _appSettings.Db.UserConnectionString);
            TestSettingString(nameof(_appSettings.Db.ConnectionString), _appSettings.Db.ConnectionString);
            TestSettingString(nameof(_appSettings.DefaultPassword), _appSettings.DefaultPassword);
            TestSettingString(nameof(_appSettings.DefaultUserEmail), _appSettings.DefaultUserEmail);
            TestSettingString(nameof(_appSettings.GoogleApiClientId), _appSettings.GoogleApiClientId);
            TestSettingRx(nameof(_appSettings.AvailableEmailsRegex), _appSettings.AvailableEmailsRegex);
        }

        private void TestSettingRx(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(key);

            try
            {
                new Regex(value);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Key '{key}' has inncorrect value '{value}' and throws the following exception: {e}");
            }
        }

        private void TestSettingString(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(key);
        }
    }
}
