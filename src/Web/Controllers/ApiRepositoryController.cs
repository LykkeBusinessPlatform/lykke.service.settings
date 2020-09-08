using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureRepositories.Repository;
using Core.Entities;
using Core.Enums;
using Core.Models;
using Core.Repositories;
using Core.Services;
using Lykke.Common.Log;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace Web.Controllers
{
    [Route("api/[controller]")]
    public class ApiRepositoryController : BaseController
    {
        private readonly IRepositoriesRepository _repositoriesRepository;
        private readonly IRepositoryDataRepository _repositoryDataRepository;
        private readonly IKeyValuesRepository _keyValuesRepository;
        public readonly IConnectionUrlHistoryRepository _connectionUrlHistoryRepository;
        private readonly INetworkRepository _networkRepository;
        private readonly IServiceTokenRepository _serviceTokensRepository;
        private readonly IRepositoriesService _repositoriesService;

        #region Constants
        const string MANUAL_FILE_PREFIX = "manual-";
        #endregion

        public ApiRepositoryController(
            ILogFactory logFactory,
            IUserActionHistoryRepository userActionHistoryRepository,
            IRepositoriesRepository repositoriesRepository,
            IRepositoryDataRepository repositoryDataRepository,
            IKeyValuesRepository keyValuesRepository,
            IConnectionUrlHistoryRepository connectionUrlHistoryRepository,
            INetworkRepository networkRepository,
            IServiceTokenRepository serviceTokensRepository,
            IRepositoriesService repositoriesService)
            : base(userActionHistoryRepository, logFactory)
        {
            _repositoriesRepository = repositoriesRepository;
            _repositoryDataRepository = repositoryDataRepository; 
            _keyValuesRepository = keyValuesRepository;
            _connectionUrlHistoryRepository = connectionUrlHistoryRepository;
            _networkRepository = networkRepository;
            _serviceTokensRepository = serviceTokensRepository;
            _repositoriesService = repositoriesService;
        }

        [HttpGet]
        public async Task<IEnumerable<IRepository>> Get()
        {
            try
            {
                var repositories = await _repositoriesRepository.GetAllAsync();
                return repositories;
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return new List<IRepository>();
            }
        }

        [HttpGet("{id}")]
        public async Task<IRepository> Get(string id)
        {
            try
            {
                var repositories = await _repositoriesRepository.GetAsync(x => x.RowKey == id);
                return repositories.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: id);
                return new RepositoryEntity();
            }
        }

        [HttpGet("repositoryFile/{repositoryName}/{tag}/{fileName}")]
        public async Task<string> RepositoryFile(string repositoryName, string tag, string fileName)
        {
            try
            {
                var repositories = await _repositoriesRepository.GetAllAsync();
                var repositoryEntity = repositories.FirstOrDefault(x => (x.OriginalName == repositoryName) && (tag == "" || x.Tag == tag) && (x.FileName == fileName) && (fileName.Contains("_git_") ? x.GitUrl.Contains("github.com") : x.FileName.Contains("_bb_")));
                if (repositoryEntity == null)
                {
                    return null;
                }

                string rowKey = repositoryEntity.RowKey;

                var connectionUrlHistory = new ConnectionUrlHistory
                {
                    RowKey = Guid.NewGuid().ToString(),
                    Ip = UserInfo.Ip,
                    RepositoryId = rowKey,
                    UserAgent = Request.Headers["User-Agent"].FirstOrDefault()
                };
                await _connectionUrlHistoryRepository.SaveConnectionUrlHistory(connectionUrlHistory);

                var repository = await _repositoriesRepository.GetAsync(rowKey);
                if (repository == null)
                {
                    return "Repository not found";
                }

                var correctFileName = repository.UseManualSettings ? MANUAL_FILE_PREFIX + fileName : fileName;

                var jsonData = await _repositoryDataRepository.GetDataAsync(correctFileName);
                var keyValues = await _keyValuesRepository.GetAsync();
                var servTokens = await _serviceTokensRepository.GetAllAsync();

                jsonData = jsonData.SubstituteServiceTokens(servTokens);
                var network = await _networkRepository.GetByIpAsync(UserInfo.Ip);
                var repositoryVersionSeparator = "-";
                jsonData = jsonData.Substitute(
                    keyValues,
                    network?.Id,
                    repositoryVersion: !string.IsNullOrEmpty(repository.Tag)
                        ? repository.Tag + repositoryVersionSeparator
                        : string.Empty);
                //TODO: do we need this?
                jsonData = jsonData.Replace(@"\/", @"/");

                return jsonData;
            }
            catch (Exception ex)
            {
                var data = new { repositoryName, tag, fileName };
                _log.Error(ex, context: data);
                return String.Empty;
            }
        }

        [HttpPost]
        public async Task<RepositoriesServiceModel> Post([FromBody]RepositoryEntity repository)
        {
            return await CreateOrUpdateRepository(repository);
        }

        [HttpPut]
        public async Task<RepositoriesServiceModel> Put([FromBody]RepositoryEntity repository)
        {
            return await CreateOrUpdateRepository(repository);
        }

        #region Private Methods
        private async Task<RepositoriesServiceModel> CreateOrUpdateRepository(RepositoryEntity repository)
        {
            try
            {
                var result = string.IsNullOrWhiteSpace(repository.RowKey)
                    ? await _repositoriesService.CreateRepositoryAsync(
                        repository,
                        UserInfo.UserName,
                        UserInfo.Ip,
                        UserInfo.UserEmail,
                        IS_PRODUCTION)
                    : await _repositoriesService.UpdateRepositoryAsync(
                        repository,
                        UserInfo.UserName,
                        UserInfo.Ip,
                        UserInfo.UserEmail,
                        IS_PRODUCTION);

                return result;
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: new { repository });
                return new RepositoriesServiceModel { Result = UpdateSettingsStatus.InternalError };
            }
        }

        #endregion
    }
}
