using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AzureRepositories.KeyValue;
using AzureRepositories.Lock;
using AzureRepositories.Repository;
using AzureRepositories.ServiceToken;
using AzureRepositories.Token;
using Common;
using Common.Log;
using Core.Blob;
using Core.Extensions;
using Core.KeyValue;
using Core.Lock;
using Core.Networks;
using Core.Repository;
using Core.ServiceToken;
using Core.Token;
using Core.User;
using Lykke.Common.Log;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Services;
using Shared.Settings;
using Web.Code;
using Web.Extensions;
using Web.Models;

namespace Web.Controllers
{
    [Authorize]
    public class HomeController : BaseController
    {
        private readonly ILog _log;
        private readonly IJsonDataRepository _jsonDataRepository;
        private readonly ITokensRepository _tokensRepository;
        private readonly IAccountTokenHistoryRepository _tokenHistoryRepository;
        private readonly IServiceTokenRepository _serviceTokensRepository;
        private readonly IKeyValuesRepository _keyValuesRepository;
        private readonly ILockRepository _lockRepository;
        private readonly AppSettings _appSettings;
        private readonly IKeyValueHistoryRepository _keyValueHistoryRepository;
        private readonly IServiceTokenHistoryRepository _serviceTokenHistoryRepository;
        private readonly IAccessDataRepository _accessDataRepository;
        private readonly IRepositoriesRepository _repositoriesRepository;
        private readonly IRepositoryDataRepository _repositoryDataRepository;
        private readonly INetworkRepository _networkRepository;
        private readonly IConnectionUrlHistoryRepository _connectionUrlHistoryRepository;
        private readonly IRepositoriesUpdateHistoryRepository _repositoriesUpdateHistoryRepository;
        private readonly ISecretKeyValuesRepository _secretKeyValuesRepository;
        private readonly IRepositoriesService _repositoriesService;

        private static readonly object Lock = new object();

        #region Constants
        private const string MANUAL_FILE_PREFIX = "manual-";
        private const string HISTORY_FILE_PREFIX = "history-";
        private const string FILENAME = "settings_";
        private const string FILE_FORMAT_ON_GIT = ".yaml";
        private const string FILE_FORMAT = ".txt";
        private const string GITHUB_URL = "github.com";
        private const int PAGE_SIZE = 10;
        #endregion

        public HomeController(
            ILogFactory logFactory,
            IJsonDataRepository jsonDataRepository,
            ITokensRepository tokensRepository,
            IServiceTokenRepository serviceTokensRepository,
            IKeyValuesRepository keyValuesRepository,
            ILockRepository lockRepository,
            AppSettings appSettings,
            IAccountTokenHistoryRepository tokenHistoryRepository,
            IKeyValueHistoryRepository keyValueHistoryRepository,
            IServiceTokenHistoryRepository serviceTokenHistoryRepository,
            IAccessDataRepository accessDataRepository,
            IUserActionHistoryRepository userActionHistoryRepository,
            IUserRepository userRepository,
            IRepositoriesRepository repositoriesRepository,
            IRepositoryDataRepository repositoryDataRepository,
            IConnectionUrlHistoryRepository connectionUrlHistoryRepository,
            INetworkRepository networkRepository,
            IRepositoriesUpdateHistoryRepository repositoriesUpdateHistoryRepository,
            ISecretKeyValuesRepository secretKeyValuesRepository,
            IRepositoriesService repositoriesService)
            : base(userActionHistoryRepository)
        {
            _log = logFactory.CreateLog(this);
            _jsonDataRepository = jsonDataRepository;
            _tokensRepository = tokensRepository;
            _serviceTokensRepository = serviceTokensRepository;
            _keyValuesRepository = keyValuesRepository;
            _lockRepository = lockRepository;
            _appSettings = appSettings;
            _tokenHistoryRepository = tokenHistoryRepository;
            _keyValueHistoryRepository = keyValueHistoryRepository;
            _serviceTokenHistoryRepository = serviceTokenHistoryRepository;
            _accessDataRepository = accessDataRepository;
            _repositoriesRepository = repositoriesRepository;
            _repositoryDataRepository = repositoryDataRepository;
            _connectionUrlHistoryRepository = connectionUrlHistoryRepository;
            _networkRepository = networkRepository;
            _repositoriesUpdateHistoryRepository = repositoriesUpdateHistoryRepository;
            _userRepository = userRepository;
            _secretKeyValuesRepository = secretKeyValuesRepository;
            _repositoriesService = repositoriesService;

            lock (Lock)
            {
                if (_isSeflTestRan) return;
                _isSeflTestRan = true;
            }

            //TODO: do we need to run it everytime?
            Task.Run(async () =>
            {
                Console.Write(await SelfTest(
                    _appSettings, userRepository, _jsonDataRepository, _tokensRepository,
                    _serviceTokensRepository,
                    _keyValuesRepository, _lockRepository, _accessDataRepository, _log
                ));
            }).GetAwaiter().GetResult();
        }

        [HttpPost("/Home/UploadJson")]
        public async Task<ActionResult> UploadJsonChanges(JsonModel jsonModel)
        {
            try
            {
                var repository = await _repositoriesRepository.GetAsync(jsonModel.RepositoryId);
                if (repository == null)
                {
                    return new JsonResult(new
                    {
                        Result = UpdateSettingsStatus.NotFound
                    });
                }

                var fileData = await GetFileDataForManualEdit(repository.FileName);

                if (fileData != jsonModel.Json)
                {
                    // if updating file, we must not create new name for it
                    var blobFileName = MANUAL_FILE_PREFIX + repository.FileName;
                    await _repositoryDataRepository.UpdateBlobAsync(jsonModel.Json, UserInfo.UserName, UserInfo.Ip, blobFileName);
                    await _lockRepository.ResetJsonPageLockAsync();

                    // update key values
                    var placeholders = fileData.PlaceholderList();
                    var keyValues = new List<KeyValueEntity>();

                    if (!string.IsNullOrEmpty(repository.Tag))
                        foreach (var item in placeholders)
                        {
                            if (item.Types != null)
                            {
                                item.RowKey = repository.Tag + "-" + item.RowKey;
                                item.Tag = repository.Tag;
                            }
                        }

                    // save key values history
                    foreach (var keyValue in placeholders)
                    {
                        IKeyValueEntity keyValueEntity = await _keyValuesRepository.GetKeyValueAsync(keyValue.RowKey);
                        var keyRepoName = !string.IsNullOrEmpty(repository?.Tag) ? repository?.Tag + "-" + repository?.OriginalName : repository?.OriginalName;
                        if (keyValueEntity == null)
                        {
                            keyValueEntity = new KeyValueEntity
                            {
                                RowKey = keyValue.RowKey,
                                RepositoryNames = new [] { keyRepoName }
                            };
                        }

                        keyValues.Add(keyValueEntity as KeyValueEntity);
                    }
                    await _repositoriesService.SaveKeyValuesAsync(keyValues, UserInfo.UserEmail, UserInfo.Ip, IS_PRODUCTION);
                }

                // update repository
                repository.UseManualSettings = jsonModel.UseManualSettings;
                await _repositoriesRepository.SaveRepositoryAsync(repository);

                //get initial commit if exists
                var repositoryHistory = await _repositoriesUpdateHistoryRepository.GetAsync(jsonModel.RepositoryId);

                //Adding data to history repository
                await _repositoriesService.AddToHistoryRepository(repository, jsonModel.Json, repositoryHistory.InitialCommit, true, UserInfo.UserName, UserInfo.Ip);

                var repositoriesModel = await _repositoriesService.GetPaginatedRepositories();
                var repositories = repositoriesModel.Data as PaginatedList<IRepository>;

                return new JsonResult(new
                {
                    status = UpdateSettingsStatus.Ok,
                    data = fileData,
                    repositories = JsonConvert.SerializeObject(repositories),
                    pageIndex = repositories.PageIndex,
                    totalPages = repositories.TotalPages
                });
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: jsonModel);
                return new JsonResult(new { status = UpdateSettingsStatus.InternalError });
            }
        }

        [HttpGet("/Home/Repository")]
        public async Task<IActionResult> Repository(string search, int? page = 1)
        {
            string hostUrl = Request.Host.ToString();

            if (!hostUrl.StartsWith("http"))
                hostUrl = Request.IsHttps ? "https://" : "http://" + hostUrl;

            try
            {
                var repositoriesModel = await _repositoriesService.GetPaginatedRepositories(search, page);

                var repositories = repositoriesModel.Data as PaginatedList<IRepository>;

                var repositoryNames = repositoriesModel.CollectionData as List<string>;

                ViewData["repositoryNames"] = JsonConvert.SerializeObject(repositoryNames);
                ViewData["timeToEditInMinutes"] = _appSettings.LockTimeInMinutes;

                return View(new RepositoryModel
                {
                    Repositories = repositories,
                    ServiceUrlForViewMode = hostUrl
                });
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return View(new RepositoryModel
                {
                    Repositories = PaginatedList<IRepository>.CreateAsync(new List<IRepository>(), 1, PAGE_SIZE),
                    ServiceUrlForViewMode = hostUrl
                });
            }
        }

        [HttpGet("/Home/RepositoryJson/{page:int?}/{search?}")]
        public async Task<IActionResult> RepositoryJson(int? page = 1, string search="")
        {
            try
            {
                var repositoriesModel = await _repositoriesService.GetPaginatedRepositories(search, page);

                var repositories = repositoriesModel.Data as PaginatedList<IRepository>;

                var repositoryNames = repositoriesModel.CollectionData as List<string>;

                return new JsonResult(new
                {
                    status = UpdateSettingsStatus.Ok,
                    repositories = JsonConvert.SerializeObject(repositories),
                    pageIndex = repositories.PageIndex,
                    totalPages = repositories.TotalPages,
                    repositoryNames = repositoryNames
                });
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return new JsonResult(new
                {
                    status = UpdateSettingsStatus.InternalError,
                    message = ex.ToString()
                });
            }
        }

        [HttpPost("/Home/RepositoryFileJson")]
        public async Task<IActionResult> RepositoryFileJson(string repositoryId)
        {
            try
            {
                var repository = await _repositoriesRepository.GetAsync(repositoryId);
                if (repository == null)
                {
                    return new JsonResult(new
                    {
                        Result = UpdateSettingsStatus.NotFound
                    });
                }

                var fileData = await GetFileDataForManualEdit(repository.FileName);

                return new JsonResult(new { Result = UpdateSettingsStatus.Ok, Json = fileData });
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: repositoryId);
                return new JsonResult(new { Result = UpdateSettingsStatus.InternalError });
            }
        }

        [HttpPost("/Home/RepositoryFileJsonClear")]
        public async Task<IActionResult> RepositoryFileJsonClear(string repositoryId)
        {
            try
            {
                var repository = await _repositoriesRepository.GetAsync(repositoryId);
                if (repository == null)
                {
                    return new JsonResult(new
                    {
                        Result = UpdateSettingsStatus.NotFound
                    });
                }

                var fileData = await _repositoriesService.GetFileData(repository.FileName);

                await UploadJsonChanges(new JsonModel()
                {
                    Json = fileData,
                    RepositoryId = repository.RowKey,
                    UseManualSettings = repository.UseManualSettings,
                    UserName = repository.UserName
                });

                fileData = fileData.ReleaseFromComments();
                return new JsonResult(new { Result = UpdateSettingsStatus.Ok, Json = fileData });
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: repositoryId);
                return new JsonResult(new { Result = UpdateSettingsStatus.InternalError });
            }
        }

        [HttpPost("/Home/RepositoryFileJsonView")]
        public async Task<IActionResult> RepositoryFileJsonView(string repositoryId)
        {
            try
            {
                var repositoryHistory = await _repositoriesUpdateHistoryRepository.GetAsync(repositoryId);
                if (repositoryHistory == null)
                {
                    return new JsonResult(new
                    {
                        Result = UpdateSettingsStatus.NotFound
                    });
                }
                var fileName = HISTORY_FILE_PREFIX + FILENAME + repositoryId + FILE_FORMAT;

                var fileData = await _repositoriesService.GetFileData(fileName);

                fileData = fileData.ReleaseFromComments();

                return new JsonResult(new { Result = UpdateSettingsStatus.Ok, Json = fileData });
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: repositoryId);
                return new JsonResult(new { Result = UpdateSettingsStatus.InternalError });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveRepository(RepositoryEntity repository)
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

                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: repository);
                return new JsonResult(new
                {
                    Result = UpdateSettingsStatus.InternalError
                });
            }
        }

        [HttpPost("Home/UpdateRepository")]
        public async Task<IActionResult> UpdateRepository(
            RepositoryEntity repository,
            string search = null)
        {
            try
            {
                var result = await _repositoriesService.UpdateRepositoryAsync(
                    repository,
                    UserInfo.UserName,
                    UserInfo.Ip,
                    UserInfo.UserEmail,
                    IS_PRODUCTION,
                    search);

                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: repository);
                return new JsonResult(new
                {
                    Result = UpdateSettingsStatus.InternalError
                });
            }
        }

        [AllowAnonymous]
        [HttpGet("/Home/RepositoryFile/{repositoryId}/{repositoryName}")]
        public async Task<IActionResult> RepositoryFile(string repositoryId, string repositoryName)
        {
            return await RepositoryFile(repositoryId, "", repositoryName);
        }

        [AllowAnonymous]
        [HttpGet("/Home/RepositoryFile/{repositoryId}/{tag}/{repositoryName}")]
        public async Task<IActionResult> RepositoryFile(string repositoryId, string tag, string repositoryName)
        {
            try
            {
                var repositoryUpdateHistoryEntities = (await _repositoriesUpdateHistoryRepository.GetAsyncByInitialCommit(repositoryId))
                    .ToArray();

                var entitiesToOrder = repositoryUpdateHistoryEntities.Where(x => x.IsManual == false).ToArray();

                if (!entitiesToOrder.Any())
                    entitiesToOrder = repositoryUpdateHistoryEntities;

                var lastUpdate = entitiesToOrder
                    .OrderByDescending(x => x.CreatedAt ?? ((RepositoryUpdateHistory)x).Timestamp)
                    .FirstOrDefault();

                if (lastUpdate == null)
                    return Content("Repository not found");

                // get repository data

                var repositoryEntity = await _repositoriesRepository.GetAsync(lastUpdate.RowKey);
                if (repositoryEntity == null)
                {
                    foreach (var history in entitiesToOrder)
                    {
                        repositoryEntity = await _repositoriesRepository.GetAsync(history.RowKey);
                        if (repositoryEntity != null)
                        {
                            ((RepositoryUpdateHistory)history).CreatedAt = DateTime.UtcNow;
                            await _repositoriesUpdateHistoryRepository.SaveRepositoryUpdateHistory(history);
                            break;
                        }
                    }

                    if(repositoryEntity == null)
                        return Content("Repository not found");
                }

                var connectionUrlHistory = new ConnectionUrlHistory
                {
                    RowKey = Guid.NewGuid().ToString(),
                    Ip = UserInfo.Ip,
                    RepositoryId = repositoryEntity.RowKey,
                    UserAgent = Request.Headers["User-Agent"].FirstOrDefault()
                };

#pragma warning disable 4014
                // this should not be awaited
                _connectionUrlHistoryRepository.SaveConnectionUrlHistory(connectionUrlHistory);
#pragma warning restore 4014

                var correctFileName = repositoryEntity.UseManualSettings ? MANUAL_FILE_PREFIX + repositoryEntity.FileName : repositoryEntity.FileName;

                var jsonData = await _repositoryDataRepository.GetDataAsync(correctFileName);
                var placeholders = jsonData.PlaceholderList();
                var keyValueKeys = placeholders.Select(p => p.RowKey);
                if (!string.IsNullOrEmpty(tag))
                    keyValueKeys = keyValueKeys.Concat(placeholders.Select(p => $"{tag}-{p.RowKey}"));
                var keyValues = await _keyValuesRepository.GetKeyValuesAsync(keyValueKeys);

                foreach (var keyValue in keyValues)
                {
                    if (string.IsNullOrEmpty(tag)
                        || !keyValue.Value.UseNotTaggedValue.HasValue
                        || !keyValue.Value.UseNotTaggedValue.Value)
                        continue;

                    string searchStr = keyValue.Value.RowKey.SubstringFromString(keyValue.Value.Tag + "-");
                    var originalKeyValue = keyValues.FirstOrDefault(k => k.Value.RowKey == searchStr);
                    if (originalKeyValue.Value != null)
                        keyValue.Value.Value = originalKeyValue.Value.Value;
                }

                var repositoryVersion = string.IsNullOrEmpty(repositoryEntity.Tag) ? string.Empty : $"{repositoryEntity.Tag}-";
                var network = await _networkRepository.GetByIpAsync(UserInfo.Ip);
                jsonData = jsonData.Substitute(
                    keyValues,
                    network?.Id,
                    repositoryVersion);

                //TODO: do we need this?
                jsonData = jsonData.Replace(@"\/", @"/");
                return Content(jsonData, MediaTypeHeaderValue.Parse("application/json"));
            }
            catch (Exception ex)
            {
                var data = new { repositoryId, tag, repositoryName };
                _log.Error(ex, context: data);
                return Content(string.Empty);
            }
        }

        [HttpGet("/Home/ConnectionUrlHistory/{page:int?}")]
        public async Task<IActionResult> ConnectionUrlHistory(int? page = 1)
        {
            try
            {
                var data = await _connectionUrlHistoryRepository.GetAllAsync();
                var connectionUrlHistory = data.OrderByDescending(x => ((ConnectionUrlHistory)x).Timestamp).Select(x => new ConnectionUrlHistoryModel
                {
                    RowKey = x.RowKey,
                    Ip = x.Ip,
                    RepositoryId = x.RepositoryId,
                    Timestamp = ((ConnectionUrlHistory)x).Timestamp.ToString("g"),
                    UserAgent = x.UserAgent
                });

                return View(PaginatedList<ConnectionUrlHistoryModel>.CreateAsync(connectionUrlHistory, page ?? 1, PAGE_SIZE));
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: page?.ToString());
                return View(PaginatedList<ConnectionUrlHistoryModel>.CreateAsync(new List<ConnectionUrlHistoryModel>(), page ?? 1, PAGE_SIZE));
            }
        }

        [HttpGet("/Home/ConnectionUrlHistoryJson/{repositoryId?}")]
        public async Task<IActionResult> ConnectionUrlHistoryJson(string repositoryId = "")
        {
            try
            {
                var data = await _connectionUrlHistoryRepository.GetAllAsync(x => x.RepositoryId == repositoryId);
                var connectionUrlHistory = data.OrderByDescending(x => ((ConnectionUrlHistory)x).Timestamp).Select(x => new ConnectionUrlHistoryModel
                {
                    RowKey = x.RowKey,
                    Ip = x.Ip,
                    RepositoryId = x.RepositoryId,
                    Timestamp = ((ConnectionUrlHistory)x).Timestamp.ToString("g"),
                    UserAgent = x.UserAgent
                });

                return new JsonResult(new { Result = UpdateSettingsStatus.Ok, Json = connectionUrlHistory });
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: repositoryId);
                return new JsonResult(new { Result = UpdateSettingsStatus.InternalError });
            }
        }

        [HttpPost("/Home/ChangeRepositoryName")]
        public async Task<IActionResult> ChangeRepositoryName(RepositoryEntity repository)
        {
            try
            {
                var repositoryEntity = await _repositoriesRepository.GetAsync(repository.RowKey);
                if (repositoryEntity != null)
                {
                    if (repositoryEntity.Name != repository.Name)
                    {
                        repositoryEntity.Name = repository.Name;
                        await _repositoriesRepository.SaveRepositoryAsync(repositoryEntity);
                        var repositoriesModel = await _repositoriesService.GetPaginatedRepositories();
                        var repositories = repositoriesModel.Data as PaginatedList<IRepository>;

                        return new JsonResult(new
                        {
                            Result = UpdateSettingsStatus.Ok,
                            Json = JsonConvert.SerializeObject(repositories),
                            pageIndex = repositories.PageIndex,
                            totalPages = repositories.TotalPages
                        });
                    }

                    return new JsonResult(new { Result = UpdateSettingsStatus.JsonFormarIncorrrect });
                }

                return new JsonResult(new { Result = UpdateSettingsStatus.NotFound });
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: repository);
                return new JsonResult(new { Result = UpdateSettingsStatus.InternalError });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetRevertRepositoryList(string repositoryId)
        {
            try
            {
                //getting list of commits
                var repositoryHistoryEntity = await _repositoriesUpdateHistoryRepository.GetAsync(repositoryId);

                if (repositoryHistoryEntity == null)
                    return new JsonResult(new { Result = UpdateSettingsStatus.NotFound });

                var repositories = await _repositoriesUpdateHistoryRepository.GetAsyncByInitialCommit(repositoryHistoryEntity.InitialCommit);

                if (repositories != null)
                {
                    repositories = repositories.OrderByDescending(x => x.CreatedAt ?? ((RepositoryUpdateHistory)x).Timestamp);
                    return new JsonResult(new
                    {
                        Result = UpdateSettingsStatus.Ok,
                        Json = JsonConvert.SerializeObject(repositories, new JsonSerializerSettings
                        {
                            DateFormatString = "dd/MM/yyy hh:mm:ss tt"
                        })
                    });
                }

                return new JsonResult(new { Result = UpdateSettingsStatus.NotFound });
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: repositoryId);
                return new JsonResult(new { Result = UpdateSettingsStatus.InternalError });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RevertRepository(string repositoryId)
        {
            try
            {
                //get needed commit
                var repositoryUpdateHistory = await _repositoriesUpdateHistoryRepository.GetAsync(repositoryId);

                if (repositoryUpdateHistory != null)
                {

                    var repositoryUpdateHistoryEntity = await _repositoriesUpdateHistoryRepository.GetAsyncByInitialCommit(repositoryUpdateHistory.InitialCommit);
                    var lastUpdate = repositoryUpdateHistoryEntity
                        .Where(x => x.IsManual == false)
                        .OrderByDescending(x => x.CreatedAt ?? ((RepositoryUpdateHistory)x).Timestamp)
                        .FirstOrDefault();

                    // get repository data and commit file data
                    var repositoryEntity = await _repositoriesRepository.GetAsync(lastUpdate.RowKey);
                    var existingBlob = await _repositoriesService.GetFileData(HISTORY_FILE_PREFIX + "settings_" + repositoryId + FILE_FORMAT);

                    //generate name
                    var fileName = repositoryUpdateHistory.IsManual ? MANUAL_FILE_PREFIX + repositoryEntity.FileName : repositoryEntity.FileName;

                    //updating main settinga file with new data
                    await _repositoryDataRepository.UpdateBlobAsync(existingBlob, UserInfo.UserName, UserInfo.Ip, fileName);

                    // removing old and adding same with current date
                    await _repositoriesUpdateHistoryRepository.RemoveRepositoryUpdateHistoryAsync(repositoryUpdateHistory.RowKey);
                    await _repositoriesUpdateHistoryRepository.SaveRepositoryUpdateHistory(repositoryUpdateHistory);

                    //update repository table
                    if (!repositoryUpdateHistory.IsManual)
                    {
                        await _repositoriesRepository.RemoveRepositoryAsync(lastUpdate.RowKey);
                        repositoryEntity.RowKey = repositoryId;
                        await _repositoriesRepository.SaveRepositoryAsync(repositoryEntity);
                    }

                    var repositoriesModel = await _repositoriesService.GetPaginatedRepositories();
                    var repositories = repositoriesModel.Data as PaginatedList<IRepository>;
                    return new JsonResult(new
                    {
                        Result = UpdateSettingsStatus.Ok,
                        Json = JsonConvert.SerializeObject(repositories),
                        pageIndex = repositories.PageIndex,
                        totalPages = repositories.TotalPages
                    });
                }

                return new JsonResult(new { Result = UpdateSettingsStatus.NotFound });
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: repositoryId);
                return new JsonResult(new { Result = UpdateSettingsStatus.InternalError });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRepository(string repositoryId)
        {
            try
            {
                var repository = await _repositoriesRepository.GetAsync(repositoryId);
                if (repository != null)
                {
                    var fileName = repository.FileName;
                    await _repositoryDataRepository.DelBlobAsync(fileName);
                    await _repositoryDataRepository.DelBlobAsync(MANUAL_FILE_PREFIX + fileName);
                    await _repositoriesRepository.RemoveRepositoryAsync(repositoryId);

                    //delete data from history repository
                    var repositoryUpdateHistory = await _repositoriesUpdateHistoryRepository.GetAsync(repositoryId);
                    var repositoriesUpdateHistory = await _repositoriesUpdateHistoryRepository.GetAsyncByInitialCommit(repositoryUpdateHistory.InitialCommit);
                    if (repositoriesUpdateHistory != null)
                    {
                        //delete each commit file
                        foreach (var repoHist in repositoriesUpdateHistory)
                        {
                            await _repositoryDataRepository.DelBlobAsync(HISTORY_FILE_PREFIX + FILENAME + repoHist.RowKey + FILE_FORMAT);
                        }
                        await _repositoriesUpdateHistoryRepository.RemoveRepositoryUpdateHistoryAsync(repositoriesUpdateHistory);
                    }

                    var keyRepoName = !string.IsNullOrEmpty(repository.Tag) ? repository.Tag + "-" + repository.OriginalName : repository.OriginalName;
                    // remove repositoryName from all related keyValues repositoryName
                    var relatedKeyValues = await _keyValuesRepository.GetKeyValuesAsync(x => x.RepositoryNames != null && x.RepositoryNames.Contains(keyRepoName));
                    if (relatedKeyValues != null)
                    {
                        foreach (var keyValue in relatedKeyValues)
                        {
                            var tempRepoNames = keyValue.RepositoryNames.ToList();
                            tempRepoNames.Remove(keyRepoName);
                            keyValue.RepositoryNames = tempRepoNames != null && tempRepoNames.Count > 0 ? tempRepoNames.ToArray() : null;
                        }

                        // await _keyValuesRepository.UpdateKeyValueAsync(relatedKeyValues);
                        await _repositoriesService.SaveKeyValuesAsync(relatedKeyValues, UserInfo.UserEmail, UserInfo.Ip, IS_PRODUCTION);
                    }

                    var repositories = await _repositoriesService.GetAllRepositories();
                    var repositoryNameDuplications = repositories.Where(x => x.OriginalName == repository.OriginalName);
                    if (!repositoryNameDuplications.Any())
                        await DeleteKeyValuesByRepositoryName(repository.OriginalName);

                    var paginatedRepositories = PaginatedList<IRepository>.CreateAsync(repositories, 1, PAGE_SIZE);
                    return new JsonResult(new
                    {
                        Result = UpdateSettingsStatus.Ok,
                        Json = JsonConvert.SerializeObject(paginatedRepositories),
                        pageIndex = paginatedRepositories.PageIndex,
                        totalPages = paginatedRepositories.TotalPages
                    });
                }

                return new JsonResult(new
                {
                    Result = UpdateSettingsStatus.NotFound
                });
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: repositoryId);
                return new JsonResult(new
                {
                    Result = UpdateSettingsStatus.InternalError
                });
            }
        }

        [HttpGet("/Home/KeyValue")]
        public async Task<IActionResult> GetKeyValue(
            string filter = null,
            string search = null,
            string repositoryId = null)
        {
            filter = filter?.ToLower();
            search = search?.ToLower();

            try
            {
                ViewData["timeToEditInMinutes"] = _appSettings.LockTimeInMinutes;
                ViewBag.RabbitConn = false;
                ViewData["isProduction"] = IS_PRODUCTION;

                List<KeyValueModel> keyValueModels = null;
                await Task.WhenAll(
                    Task.Run(async () => 
                    {
                        IEnumerable<IRepository> repositories = await _repositoriesRepository.GetAllAsync();
                        ViewData["repositoryNames"] = JsonConvert.SerializeObject(repositories.Select(x =>
                            !string.IsNullOrEmpty(x.Tag) ? x.Tag + "-" + x.OriginalName : x.OriginalName).Distinct());
                    }),
                    Task.Run(async () =>
                    {
                        IEnumerable<IKeyValueEntity> keyValues = await GetKeyValuesAsync(i => FilterKeyValue(i, filter, search), repositoryId);
                        ViewBag.KeyValuesWithJsonTypes = JsonConvert.SerializeObject(
                            from r in keyValues
                            where r.Types != null && (r.Types.Contains(KeyValueTypes.Json) || r.Types.Contains(KeyValueTypes.JsonArray))
                            select new { Key = r.RowKey, r.Value });
                        ViewData["keyValueNames"] = JsonConvert.SerializeObject(keyValues.Select(key => key.RowKey).Distinct());

                        var keyValueEntities = keyValues as IKeyValueEntity[] ?? keyValues.ToArray();
                        ViewData["errorInputs"] = keyValueEntities.Any()
                            ? $", {string.Join(", ", keyValueEntities.Select(ok => $@"input[id=""{ok}""]"))}"
                            : string.Empty;

                        keyValueModels = KeyValueModel.WithOld(keyValueEntities);
                    }));

                return View("~/Views/Home/KeyValuesV2.cshtml", keyValueModels);
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: new { filter, search });
                return View("~/Views/Home/KeyValuesV2.cshtml", KeyValueModel.WithOld(new IKeyValueEntity[] { }));
            }
        }

        [HttpPost("/Home/DeleteKeyValue")]
        public async Task<ActionResult> DeleteKeyValue(
            string keyValueId,
            string filter = null,
            string search = null,
            string repositoryId = null)
        {
            filter = filter?.ToLower();
            search = search?.ToLower();

            try
            {
                var keyValues = await GetKeyValuesAsync(i => FilterKeyValue(i, filter, search), repositoryId);
                var keyValue = keyValues.FirstOrDefault(oe => oe.RowKey.Equals(keyValueId));
                if (keyValue == null)
                {
                    return new JsonResult(new
                    {
                        status = UpdateSettingsStatus.NotFound
                    });
                } 

                if (keyValue.RepositoryNames == null)
                {
                    try
                    {
                        var tempKeyValues = keyValues.ToList();
                        tempKeyValues.Remove(keyValue);
                        keyValues = tempKeyValues;

                        await _keyValuesRepository.DeleteKeyValueWithHistoryAsync(keyValueId, $"Removing '{keyValueId}'", UserInfo.UserEmail, UserInfo.Ip);

                        var duplicatedKeys = keyValues
                            .Where(x => x.RowKey != keyValue.RowKey && !string.IsNullOrEmpty(x.Value) && x.Value == keyValue.Value && string.IsNullOrEmpty(x.Tag))
                            .ToList();
                        if (duplicatedKeys.Count == 1)
                        {
                            var duplicatedKey = duplicatedKeys.First();
                            duplicatedKey.IsDuplicated = false;
                            await _repositoriesService.SaveKeyValuesAsync(new[] { duplicatedKey }, UserInfo.UserEmail, UserInfo.Ip, IS_PRODUCTION);
                        }

                        return new JsonResult(new
                        {
                            status = 0,
                            data = JsonConvert.SerializeObject(KeyValueModel.WithOld(keyValues))
                        });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        return new JsonResult(new
                        {
                            status = 1,
                            data = JsonConvert.SerializeObject(KeyValueModel.WithOld(keyValues))
                        });
                    }
                }

                return new JsonResult(new
                {
                    status = 2,
                    data = JsonConvert.SerializeObject(KeyValueModel.WithOld(keyValues))
                });
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: keyValueId);
                return new JsonResult(new
                {
                    status = 3
                });
            }
        }

        [HttpPost("/Home/UploadKeyValue")]
        public async Task<ActionResult> UploadKeyValueChanges(
            KeyValueModel entity,
            bool? forced = null,
            bool sendToAll = false,
            string search = "",
            string filter = "",
            string repositoryId = null)
        {
            filter = filter?.ToLower();
            search = search?.ToLower();
            try
            {
                if (entity.HasFullAccess.HasValue && !entity.HasFullAccess.Value)
                    return new JsonResult(new
                    {
                        status = UpdateSettingsStatus.InvalidRequest
                    });

                var keyValues = await GetKeyValuesAsync(null, repositoryId);
                var keyValue = keyValues.FirstOrDefault(x => x.RowKey == entity.RowKey);
                if (keyValue == null)
                    return new JsonResult(new
                    {
                        status = UpdateSettingsStatus.NotFound
                    });

                var duplicatedKeys = keyValues
                    .Where(x => x.RowKey != entity.RowKey && !string.IsNullOrEmpty(x.Value) && x.Value == entity.Value && string.IsNullOrEmpty(x.Tag))
                    .ToList();
                if ((!forced.HasValue || !forced.Value) && IS_PRODUCTION && duplicatedKeys.Count > 0)
                    return new JsonResult(new
                    {
                        status = UpdateSettingsStatus.HasDuplicated,
                        duplicatedKeys = duplicatedKeys.Select(x => x.RowKey)
                    });

                var keyValueEntity = new KeyValueEntity
                {
                    RowKey = entity.RowKey,
                    Value = entity.Value,
                    UseNotTaggedValue = entity.UseNotTaggedValue,
                    IsDuplicated = duplicatedKeys.Count > 0,
                    Override = keyValue.Override,
                    Types = keyValue.Types,
                    Tag = keyValue.Tag,
                    RepositoryId = keyValue.RepositoryId,
                    RepositoryNames = keyValue.RepositoryNames,
                    EmptyValueType = entity.EmptyValueType
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

                var result = await _repositoriesService.SaveKeyValuesAsync(
                    entitiesToUpload,
                    UserInfo.UserEmail,
                    UserInfo.Ip,
                    IS_PRODUCTION);

                var updatedKeyValues = await GetKeyValuesAsync(i => FilterKeyValue(i, filter, search), repositoryId);

                var jsonTypedKeyValues = new List<IKeyValueEntity>();
                foreach (var valueEntity in updatedKeyValues)
                {
                    if ((valueEntity.Types?.All(t => t != KeyValueTypes.Json && t != KeyValueTypes.JsonArray) ?? true))
                        continue;

                    jsonTypedKeyValues.Add(valueEntity);
                    valueEntity.Value = valueEntity.Value?.Replace("\"", "&quot;");
                }

                ViewBag.KeyValuesWithJsonTypes = JsonConvert.SerializeObject(jsonTypedKeyValues.Select(r => new { Key = r.RowKey, r.Value }));

                return new JsonResult(new
                {
                    status = result ? UpdateSettingsStatus.Ok : UpdateSettingsStatus.InternalError,
                    data = JsonConvert.SerializeObject(KeyValueModel.WithOld(updatedKeyValues))
                });
            }
            catch (Exception ex)
            {
                var data = new { entity, forced, sendToAll };
                _log.Error(ex, context: data);
                return new JsonResult(new
                {
                    status = UpdateSettingsStatus.InternalError
                });
            }
        }

        [IgnoreLogAction]
        [HttpGet("/Home/LockTime")]
        public async Task<IActionResult> LockTime()
        {
            try
            {
                var lockData = await _lockRepository.GetJsonPageLockAsync();

                if (!(lockData is LockEntity locInfo))
                {
                    return new JsonResult(new
                    {
                        diff = _appSettings.LockTimeInMinutes + 1
                    });
                }

                return new JsonResult(new
                {
                    diff = (DateTime.Now.ToUniversalTime() - locInfo.DateTime).TotalMinutes,
                    userName = locInfo.UserName,
                    userEmail = locInfo.UserEmail,
                    ipAddress = locInfo.IpAddress,
                });
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return new JsonResult(string.Empty);
            }
        }

        [IgnoreLogAction]
        [HttpPost("/Home/GetKvHistory")]
        public async Task<IActionResult> GetKvHistory(string keyValueId)
        {
            try
            {
                var historyData = await _keyValueHistoryRepository.GetHistoryByKeyValueAsync(keyValueId);

                return new JsonResult(new
                {
                    history = historyData.Select(h => new
                    {
                        name = h.UserName,
                        date = ((KeyValueHistory)h).Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                        ip = h.UserIpAddress,
                        value = h.NewValue
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: keyValueId);
                return new JsonResult(string.Empty);
            }
        }

        [IgnoreLogAction]
        [HttpPost("/Home/GetKV")]
        public async Task<IActionResult> GetKV(string keyValueId, bool useNotTagged)
        {
            try
            {
                Console.WriteLine(keyValueId);
                Console.WriteLine(useNotTagged);

                IKeyValueEntity keyValue;
                if (useNotTagged)
                {
                    keyValueId = keyValueId.SubstringFromString("-");
                    keyValue = await _keyValuesRepository.GetKeyValueAsync(keyValueId);
                }
                else
                {
                    keyValue = await _keyValuesRepository.GetKeyValueAsync(keyValueId);
                }

                Console.WriteLine(keyValue.Value);

                return new JsonResult(new
                {
                    keyValue.Value
                });
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: keyValueId);
                return new JsonResult(string.Empty);
            }
        }

        [HttpGet("/Home/SetLockTime")]
        public async Task<IActionResult> SetLockTime()
        {
            try
            {
                await _lockRepository.SetJsonPageLockAsync(UserInfo.UserEmail, UserInfo.UserName, UserInfo.Ip);
                return new JsonResult(new { Result = UpdateSettingsStatus.Ok });
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return new JsonResult(new { Result = UpdateSettingsStatus.InternalError });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UnlockJson()
        {
            try
            {
                await _lockRepository.ResetJsonPageLockAsync();
                return new JsonResult(new { Result = "Ok" });
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return new JsonResult(new { Result = "InternalError" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> AccessToken()
        {
            try
            {
                string hostUrl = Request.Host.ToString();

                if (!hostUrl.StartsWith("http"))
                    hostUrl = Request.IsHttps ? "https://" : "http://" + hostUrl;

                return View(new AccessTokenModel
                {
                    Tokens = await GetAllTokens(),
                    ServiceUrlForViewMode = hostUrl,
                });
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return View(new AccessTokenModel { Tokens = new List<IToken>(), ServiceUrlForViewMode = string.Empty });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ServiceToken()
        {
            return View(new ServiceTokenModel
            {
                Tokens = await GetAllServiceTokens()
            });
        }

        [HttpPost]
        public async Task<IActionResult> SaveAccessToken(TokenEntity token)
        {
            try
            {
                token.ETag = WebUtility.UrlDecode(token.ETag);
                token.IpList = token.IpList ?? string.Empty;
                var origToken = await _tokensRepository.GetAsync(token.RowKey);

                if (origToken != null && !token.ETag.Equals(origToken.ETag))
                {
                    return new JsonResult(new
                    {
                        Result = UpdateSettingsStatus.OutOfDate,
                        Json = JsonConvert.SerializeObject(await GetAllTokens())
                    });
                }

                if (origToken == null)
                {
                    token.ETag = "*";
                }

                await _tokensRepository.SaveTokenAsync(token);
                await _tokenHistoryRepository.SaveTokenHistoryAsync(token, UserInfo.UserEmail, UserInfo.Ip);

                return new JsonResult(new
                {
                    Result = UpdateSettingsStatus.Ok,
                    Json = JsonConvert.SerializeObject(await GetAllTokens())
                });
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: token);
                return new JsonResult(new { Result = UpdateSettingsStatus.InternalError });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ForceSaveAccessToken(TokenEntity token)
        {
            try
            {
                token.ETag = "*";
                token.IpList = token.IpList ?? string.Empty;
                await _tokensRepository.SaveTokenAsync(token);
                await _tokenHistoryRepository.SaveTokenHistoryAsync(token, UserInfo.UserEmail, UserInfo.Ip);

                return new JsonResult(new
                {
                    Result = UpdateSettingsStatus.Ok,
                    Json = JsonConvert.SerializeObject(await GetAllTokens())
                });
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: token);
                return new JsonResult(new { Result = UpdateSettingsStatus.InternalError });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveAccessToken(string tokenId)
        {
            try
            {
                await _tokensRepository.RemoveTokenAsync(tokenId);
                var token = new TokenEntity
                {
                    RowKey = tokenId,
                    AccessList = "[WAS DELETED]",
                    IpList = "[WAS DELETED]"
                };
                await _tokenHistoryRepository.SaveTokenHistoryAsync(token, UserInfo.UserEmail, UserInfo.Ip);

                return new JsonResult(new
                {
                    Result = UpdateSettingsStatus.Ok,
                    Json = JsonConvert.SerializeObject(await GetAllTokens())
                });
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: tokenId);
                return new JsonResult(new { Result = UpdateSettingsStatus.InternalError });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveServiceToken(ServiceTokenEntity token)
        {
            try
            {
                token.ETag = WebUtility.UrlDecode(token.ETag);
                var origToken = await _serviceTokensRepository.GetAsync(token.RowKey);

                if (origToken != null && !token.ETag.Equals(origToken.ETag))
                {
                    return new JsonResult(new
                    {
                        Result = UpdateSettingsStatus.OutOfDate,
                        Json = JsonConvert.SerializeObject(await GetAllServiceTokens())
                    });
                }

                if (origToken == null)
                {
                    token.ETag = "*";
                    token.SecurityKeyOne = Guid.NewGuid().ToString();
                    token.SecurityKeyTwo = Guid.NewGuid().ToString();
                }

                await _serviceTokensRepository.SaveAsync(token);
                await _serviceTokenHistoryRepository.SaveTokenHistoryAsync(token, UserInfo.UserEmail, UserInfo.Ip);

                return new JsonResult(new
                {
                    Result = UpdateSettingsStatus.Ok,
                    Json = JsonConvert.SerializeObject(await GetAllServiceTokens())
                });
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: token);
                return new JsonResult(new { Result = UpdateSettingsStatus.InternalError });
            }
        }

        [HttpPost]
        public IActionResult GenerateNewServiceToken(ServiceTokenEntity token)
        {
            return new JsonResult(new
            {
                Result = UpdateSettingsStatus.Ok,
                Code = Guid.NewGuid().ToString()
            });
        }

        [HttpPost]
        public async Task<IActionResult> ForceSaveServiceToken(ServiceTokenEntity token)
        {
            try
            {
                token.ETag = "*";

                await _serviceTokensRepository.SaveAsync(token);
                await _serviceTokenHistoryRepository.SaveTokenHistoryAsync(token, UserInfo.UserEmail, UserInfo.Ip);

                return new JsonResult(new
                {
                    Result = UpdateSettingsStatus.Ok,
                    Json = JsonConvert.SerializeObject(await GetAllServiceTokens())
                });
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: token);
                return new JsonResult(new { Result = UpdateSettingsStatus.InternalError });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveServiceToken(string tokenId)
        {
            try
            {
                await _serviceTokensRepository.RemoveAsync(tokenId);
                return new JsonResult(new
                {
                    Result = UpdateSettingsStatus.Ok,
                    Json = JsonConvert.SerializeObject(await GetAllServiceTokens())
                });
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: tokenId);
                return new JsonResult(new { Result = UpdateSettingsStatus.InternalError });
            }
        }

        #region private funcitons

        private async Task<List<IServiceTokenEntity>> GetAllServiceTokens()
        {
            try
            {
                return (from t in await _serviceTokensRepository.GetAllAsync()
                        orderby t.RowKey
                        select t).ToList();
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return new List<IServiceTokenEntity>();
            }
        }

        private async Task<List<IToken>> GetAllTokens()
        {
            try
            {
                return (from t in await _tokensRepository.GetAllAsync()
                        orderby t.RowKey
                        select t).ToList();
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return new List<IToken>();
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

        private async Task DeleteKeyValuesByRepositoryName(string repositoryName)
        {
            try
            {
                var keyValues = await GetKeyValuesAsync(kv => kv.RepositoryNames != null && kv.RepositoryNames.Contains(repositoryName));
                var keyValueEntities = keyValues as IKeyValueEntity[] ?? keyValues.ToArray();

                foreach (var keyValueEntity in keyValueEntities)
                {
                    if (keyValueEntity.RepositoryNames.Length == 1)
                    {
                        keyValueEntity.RepositoryNames = null;
                    }
                    else
                    {
                        var repositoryNames = keyValueEntity.RepositoryNames.ToList();
                        repositoryNames.Remove(repositoryName);
                        keyValueEntity.RepositoryNames = repositoryNames.ToArray();
                    }
                }

                await _keyValuesRepository.UpdateKeyValueAsync(keyValueEntities);
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: repositoryName);
            }
        }

        private async Task<string> GetFileDataForManualEdit(string originalFileName)
        {
            try
            {
                // get json data from blob. if manual file does not exists, get original json instead
                var listOfNames = await _repositoryDataRepository.GetExistingFileNames();

                var fileName = listOfNames.Contains(MANUAL_FILE_PREFIX + originalFileName) ? MANUAL_FILE_PREFIX + originalFileName : originalFileName;

                var fileData = await _repositoriesService.GetFileData(fileName);

                // fileData = fileData.ReleaseFromComments();

                return fileData;
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: originalFileName);
                return string.Empty;
            }
        }

        private async Task<IEnumerable<IKeyValueEntity>> GetKeyValuesAsync(Func<IKeyValueEntity, bool> filter = null, string reposiroyId = null)
        {
            try
            {
                List<IKeyValueEntity> keyValues = new List<IKeyValueEntity>();
                IEnumerable<IKeyValueEntity> regularKeyValues = await _keyValuesRepository.GetKeyValuesAsync(filter, reposiroyId);

                keyValues.AddRange(regularKeyValues);

                if (IS_PRODUCTION)
                {
                    if (!string.IsNullOrEmpty(_appSettings.SecretsConnString))
                    {
                        var secretskeyValues = await _secretKeyValuesRepository.GetKeyValuesAsync();
                        keyValues.AddRange(secretskeyValues);
                    }
                    if (!HttpContext.IsAdmin())
                    {
                        keyValues = keyValues.Where(k => k.Types == null || !HasSecretInTypes(k)).ToList();
                    }
                }

                foreach (var keyValue in keyValues)
                {
                    if (!keyValue.UseNotTaggedValue.HasValue || !keyValue.UseNotTaggedValue.Value)
                        continue;

                    var originalKeyValue = keyValues.FirstOrDefault(k => k.RowKey == keyValue.RowKey.SubstringFromString(keyValue.Tag + "-"));
                    if (originalKeyValue != null)
                        keyValue.Value = originalKeyValue.Value;
                }

                return keyValues;
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return new List<IKeyValueEntity>();
            }
        }

        private bool HasSecretInTypes(IKeyValueEntity keyValueEntity)
        {
            if (keyValueEntity.Types == null)
                return false;

            foreach (var type in keyValueEntity.Types)
            {
                if (type.ToLowCase().Contains("secret"))
                    return true;
            }
            return false;
        }

        #endregion


        #region SelfCheck functions

        private static bool _isSeflTestRan;

        protected override void AddSuccess(StringBuilder sb, string selfTestingSuccessfulCompleted)
        {
            sb.AppendLine($"Success: {selfTestingSuccessfulCompleted}");
        }

        protected override void AddError(StringBuilder sb, string selfTestingError)
        {
            sb.AppendLine($"Error: {selfTestingError}");
        }

        protected override void AddText(StringBuilder sb, string selfTestingText)
        {
            sb.AppendLine(selfTestingText);
        }

        protected override void AddHeader(StringBuilder sb, string checkSettingsHeader)
        {
            sb.AppendLine($"--==={checkSettingsHeader}===--");
        }

        protected override void AddCaption(StringBuilder sb, string checkSettingsCaption)
        {
            sb.AppendLine($"--{checkSettingsCaption}--");
        }

        #endregion
    }
}
