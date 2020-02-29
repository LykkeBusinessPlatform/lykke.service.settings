using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AzureRepositories.KeyValue;
using AzureRepositories.Repository;
using Common;
using Common.Log;
using Core.Blob;
using Core.Extensions;
using Core.KeyValue;
using Core.Repository;
using Lykke.Common.Log;
using Newtonsoft.Json;
using Services.GitServices;
using Shared.Settings;

namespace Services.RepositoryServices
{
    // TODO: this RepositoryService later will be used to replace SaveRepository action in HomeController and CreateOrUpdateRepository in ApiRepositoryController
    public class RepositoriesService : IRepositoriesService
    {
        private readonly IKeyValuesRepository _keyValuesRepository;
        private readonly IKeyValueHistoryRepository _keyValueHistoryRepository;
        private readonly IRepositoriesRepository _repositoriesRepository;
        private readonly IRepositoryDataRepository _repositoryDataRepository;
        private readonly IRepositoriesUpdateHistoryRepository _repositoriesUpdateHistoryRepository;
        private readonly ISecretKeyValuesRepository _secretKeyValuesRepository;
        private readonly AppSettings _appSettings;
        private readonly ILog _log;

        #region Constants
        private const string MANUAL_FILE_PREFIX = "manual-";
        private const string HISTORY_FILE_PREFIX = "history-";
        private const string FILENAME = "settings_";
        private const string FILE_FORMAT_ON_GIT = ".yaml";
        private const string FILE_FORMAT = ".txt";
        private const string GITHUB_URL = "github.com";
        private const string ACTION_CREATE = "create";
        private const string ACTION_UPDATE = "update";
        private const string _repositoryFileInfoControllerAction = "/Home/RepositoryFile/";
        private const int PAGE_SIZE = 10;
        #endregion

        public RepositoriesService(
            IKeyValuesRepository keyValuesRepository,
            IKeyValueHistoryRepository keyValueHistoryRepository,
            IRepositoriesRepository repositoriesRepository,
            IRepositoryDataRepository repositoryDataRepository,
            IRepositoriesUpdateHistoryRepository repositoriesUpdateHistoryRepository,
            ISecretKeyValuesRepository secretKeyValuesRepository,
            AppSettings appSettings,
            ILogFactory logFactory)
        {
            _keyValuesRepository = keyValuesRepository;
            _keyValueHistoryRepository = keyValueHistoryRepository;
            _repositoriesRepository = repositoriesRepository;
            _repositoryDataRepository = repositoryDataRepository;
            _repositoriesUpdateHistoryRepository = repositoriesUpdateHistoryRepository;
            _secretKeyValuesRepository = secretKeyValuesRepository;
            _appSettings = appSettings;
            _log = logFactory.CreateLog(this);
        }

        public async Task<string> GetFileData(string file)
        {
            try
            {
                var fileData = await _repositoryDataRepository.GetDataAsync(file);
                return fileData;
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: file);
                return string.Empty;
            }
        }

        public async Task AddToHistoryRepository(
            IRepository repository,
            string settingsJson,
            string lastCommit = "",
            bool isManual = false,
            string userName = "",
            string userIp = "")
        {
            var lastUpdate = await _repositoriesUpdateHistoryRepository.GetAsync(lastCommit);

            bool firtstCommit = lastUpdate == null;

            //cheking if last commit is the same. Skip manual edits. If it is do noting - to not duplicate           
            if (lastUpdate != null)
                await GetFileData(HISTORY_FILE_PREFIX + "settings_" + lastUpdate.RowKey + FILE_FORMAT);

            //save commit data
            var repositoryUpdateHistory = new RepositoryUpdateHistory()
            {
                RowKey = isManual ? Guid.NewGuid().ToString() : repository.RowKey,
                InitialCommit = firtstCommit ? repository.RowKey : lastUpdate.InitialCommit,
                User = userName,
                Branch = repository.Branch,
                IsManual = isManual,
                CreatedAt = DateTime.UtcNow,
            };
            await _repositoriesUpdateHistoryRepository.SaveRepositoryUpdateHistory(repositoryUpdateHistory);

            //save history-settings_ file
            var blobFileName = HISTORY_FILE_PREFIX + "settings_" + repositoryUpdateHistory.RowKey + FILE_FORMAT;
            await _repositoryDataRepository.UpdateBlobAsync(settingsJson, userName, userIp, blobFileName);
        }

        public async Task<List<IRepository>> GetAllRepositories()
        {
            try
            {
                var repositoriesData = await GetAllRepos();

                var repositories = (from r in repositoriesData
                                    orderby r.RowKey
                                    select new RepositoryEntity
                                    {
                                        RowKey = r.RowKey,
                                        Name = r.Name,
                                        GitUrl = r.GitUrl,
                                        Branch = r.Branch,
                                        FileName = r.FileName,
                                        UserName = r.UserName,
                                        ConnectionUrl = r.ConnectionUrl,
                                        UseManualSettings = r.UseManualSettings,
                                        Tag = r.Tag,
                                        OriginalName = r.OriginalName,
                                        Timestamp = r.Timestamp,
                                        LastModified = r.Timestamp.ToString("MM/dd/yy")
                                    }).OrderByDescending(x => x.Timestamp)
                                    .ToList<IRepository>();

                return repositories;
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return new List<IRepository>();
            }
        }

        public async Task<RepositoriesServiceModel> GetPaginatedRepositories(string search = "", int? page = 1)
        {
            try
            {
                var repositoriesData = await GetAllRepos();
                var repositoryNames = repositoriesData.Select(x => x.Name).Distinct().ToList();

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    repositoriesData = repositoriesData.Where(r => r.Name.ToLower().Contains(search)); 
                }

                var repositories = (from r in repositoriesData 
                                    orderby r.RowKey 
                                    select new RepositoryEntity
                                    {
                                        RowKey = r.RowKey,
                                        Name = r.Name,
                                        GitUrl = r.GitUrl,
                                        Branch = r.Branch,
                                        FileName = r.FileName,
                                        UserName = r.UserName,
                                        ConnectionUrl = r.ConnectionUrl,
                                        UseManualSettings = r.UseManualSettings,
                                        Tag = r.Tag,
                                        OriginalName = r.OriginalName,
                                        Timestamp = r.Timestamp,
                                        LastModified = r.Timestamp.ToString("MM/dd/yy")
                                    }).OrderByDescending(x=>x.Timestamp)
                                    .ToList<IRepository>();

                var repositoryModel = new RepositoriesServiceModel
                {
                    Result = UpdateSettingsStatus.Ok,
                    Data = PaginatedList<IRepository>.CreateAsync(repositories, page ?? 1, PAGE_SIZE),
                    CollectionData = repositoryNames
                };

                return repositoryModel;
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return new RepositoriesServiceModel { Result = UpdateSettingsStatus.InternalError, Message = ex.Message };
            }
        }

        public async Task<bool> SaveKeyValuesAsync(
            IEnumerable<IKeyValueEntity> keyValues,
            string userEmail,
            string userIp,
            bool isProduction)
        {
            var uniqueDict = new Dictionary<string, IKeyValueEntity>();
            foreach (var keyValue in keyValues)
            {
                uniqueDict[keyValue.RowKey] = keyValue;
            }

            var tasks = new List<Task>(3)
            {
                _keyValueHistoryRepository.SaveKeyValuesHistoryAsync(
                    uniqueDict.Values,
                    userEmail,
                    userIp),
            };
            var resultTasks = new List<Task<bool>>(2);
            var regularKeyValues = new List<IKeyValueEntity>();
            var secretKeyValues = new List<IKeyValueEntity>();
            foreach (var item in uniqueDict.Values)
            {
                if (isProduction && !string.IsNullOrEmpty(_appSettings.SecretsConnString))
                {
                    if (item.Types == null || item.Types != null && !item.Types.Contains("Secret"))
                        regularKeyValues.Add(item);
                    else if (item.Types != null && item.Types.Contains("Secret"))
                        secretKeyValues.Add(item);
                }
                else
                {
                    regularKeyValues.Add(item);
                }
            }
            if (regularKeyValues.Count > 0)
            {
                var regularKeysTask = _keyValuesRepository.ReplaceKeyValueAsync(regularKeyValues);
                tasks.Add(regularKeysTask);
                resultTasks.Add(regularKeysTask);
            }
            if (secretKeyValues.Count > 0)
            {
                var secretKeysTask = _secretKeyValuesRepository.ReplaceKeyValueAsync(secretKeyValues);
                tasks.Add(secretKeysTask);
                resultTasks.Add(secretKeysTask);
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
            return resultTasks.All(t => t.Result);
        }

        private async Task<IEnumerable<IRepository>> GetAllRepos()
        {
            var repositories = await _repositoriesRepository.GetAllAsync();
            return repositories;
        }

        private async Task<List<KeyValueEntity>> InitKeyValuesAsync(
            IRepository repositoryEntity,
            IEnumerable<KeyValue> placeholders,
            string repositoryTag,
            string keyRepoName,
            bool isCreate)
        {
            var keyValues = new List<KeyValueEntity>();

            // save key values history
            foreach (var keyValue in placeholders)
            {
                if (!string.IsNullOrEmpty(repositoryTag))
                {
                    if (isCreate)
                    {
                        var baseKeyValueEntity = await _keyValuesRepository.GetKeyValueAsync(keyValue.RowKey);
                        if (baseKeyValueEntity?.RepositoryNames != null && baseKeyValueEntity.RepositoryNames.Contains(repositoryEntity?.OriginalName))
                            keyValue.UseNotTaggedValue = true;
                    }
                    else
                    {
                        keyValue.UseNotTaggedValue = false;
                    }

                    keyValue.RowKey = repositoryTag + "-" + keyValue.RowKey;
                    keyValue.Tag = repositoryTag;
                }
                var keyValueEntity = await _keyValuesRepository.GetKeyValueAsync(keyValue.RowKey);
                if (keyValueEntity == null)
                {
                    keyValueEntity = new KeyValueEntity
                    {
                        RowKey = keyValue.RowKey,
                        RepositoryNames = new[] { keyRepoName },
                        Value = keyValue.Value,
                    };
                }
                else if (keyValueEntity.RepositoryNames == null)
                {
                    keyValueEntity.RepositoryNames = new[] { keyRepoName };
                }
                else if (keyValueEntity.RepositoryNames != null && !keyValueEntity.RepositoryNames.Contains(keyRepoName))
                {
                    var repositoryIds = keyValueEntity.RepositoryNames.ToList();
                    repositoryIds.Add(keyRepoName);
                    keyValueEntity.RepositoryNames = repositoryIds.ToArray();
                }

                keyValueEntity.Types = keyValue.Types;
                keyValueEntity.RepositoryId = repositoryEntity.RowKey;
                keyValueEntity.Tag = keyValue.Tag;
                if (!keyValueEntity.UseNotTaggedValue.HasValue)
                    keyValueEntity.UseNotTaggedValue = keyValue.UseNotTaggedValue;
                keyValues.Add(keyValueEntity as KeyValueEntity);
            }

            return keyValues;
        }

        public async Task<RepositoriesServiceModel> CreateRepositoryAsync(
            IRepository repository,
            string userName,
            string userIp,
            string userEmail,
            bool isProduction)
        {
            if (string.IsNullOrWhiteSpace(repository.GitUrl))
                return new RepositoriesServiceModel
                {
                    Result = UpdateSettingsStatus.InvalidInput,
                    Message = "Url can't be empty",
                };

            repository.GitUrl = repository.GitUrl?.Trim();

            //check if type if github or bitbucket. since we have only github and bitbucket I am checking url for github.com
            var type = repository.GitUrl.Contains(GITHUB_URL) ? SourceControlTypes.Github : SourceControlTypes.Bitbucket;

            //get gitUrl for raw json format
            var settingsGitUrl = GitServices.GitServices.GenerateRepositorySettingsGitUrl(repository.GitUrl, type, repository.Branch);

            var repositoryExistedItems = await _repositoriesRepository.GetAllAsync();
            IRepository noTagRepo = null;
            foreach (var item in repositoryExistedItems)
            {
                if (GitServices.GitServices.GenerateRepositorySettingsGitUrl(item.GitUrl, type, item.Branch) != settingsGitUrl)
                    continue;

                if (string.IsNullOrWhiteSpace(item.Tag))
                    noTagRepo = item;
                if (item.Tag == repository.Tag)
                    return new RepositoriesServiceModel { Result = UpdateSettingsStatus.AlreadyExists };
            }

            if (!string.IsNullOrWhiteSpace(repository.Tag))
            {
                if (noTagRepo == null)
                    return new RepositoriesServiceModel
                    {
                        Result = UpdateSettingsStatus.InvalidInput,
                        Message = "Please, first create main repo without tag."
                    };
                var whiteSpaceRegex = new Regex(@"\s");
                var tagMaximumLength = 20;
                if (whiteSpaceRegex.IsMatch(repository.Tag) || repository.Tag.Length > tagMaximumLength)
                    return new RepositoriesServiceModel
                    {
                        Result = UpdateSettingsStatus.InvalidInput,
                        Message = "Tag includes whitespace characters or it is too long (max length is 20 characters)"
                    };
            }

            var name = GitServices.GitServices.GetGitRepositoryName(repository.GitUrl, type);

            string fileFullName = FILENAME;

            fileFullName += (type == SourceControlTypes.Github) ? "git_" : "bb_";
            fileFullName += name + "_" + repository.Branch;
            fileFullName += (repository.Tag == null) ? FILE_FORMAT : "_" + repository.Tag + FILE_FORMAT;

            IRepository repositoryEntity = new RepositoryEntity
            {
                RowKey = Guid.NewGuid().ToString(),
                GitUrl = repository.GitUrl,
                Branch = repository.Branch,
                FileName = fileFullName,
                UserName = userName,
                Name = name,
                OriginalName = name,
                Tag = repository.Tag
            };

            repositoryEntity.ConnectionUrl = _repositoryFileInfoControllerAction + repositoryEntity.RowKey + "/"
                + (!string.IsNullOrWhiteSpace(repositoryEntity.Tag) ? repositoryEntity.Tag + "/" : string.Empty) + name;

            //get json from generated gitUrl
            var settingsResult = GitServices.GitServices.DownloadSettingsFileFromGit(
                _log,
                settingsGitUrl,
                type,
                _appSettings.BitBucketSettings?.BitbucketEmail,
                _appSettings.BitBucketSettings?.BitbucketPassword);

            if (!settingsResult.Success)
            {
                _log.Warning($"Couldn't download settings file from git: {settingsResult.Message}");
                return new RepositoriesServiceModel
                {
                    Result = UpdateSettingsStatus.GitDownloadError,
                    Message = "Couldn't download settings file from git",
                };
            }

            string settingsYaml = settingsResult.Data.ToString();
            var settings = settingsYaml.GetSettingsDataFromYaml();
            if (!settings.Success)
            {
                _log.Warning($"Couldn't get settings data from yaml: {settings.Message}");
                return new RepositoriesServiceModel
                {
                    Result = UpdateSettingsStatus.YamlProcessingError,
                    Message = "Couldn't get settings data from yaml",
                };
            }

            var settingsJson = (settings.Data as DataFromYaml)?.Json;

            //Adding data to history repository
            await AddToHistoryRepository(repositoryEntity, settingsJson, userName: userName, userIp: userIp);
            // if updating file, we must not create new name for it
            await _repositoryDataRepository.UpdateBlobAsync(settingsJson, userName, userIp, fileFullName);
            await _repositoriesRepository.SaveRepositoryAsync(repositoryEntity);

            // update key values
            var placeholders = (settings.Data as DataFromYaml)?.Placeholders;
            var keyRepoName = !string.IsNullOrEmpty(repositoryEntity.Tag) ? repositoryEntity.Tag + "-" + repositoryEntity.OriginalName : repositoryEntity.OriginalName;
            var keyValues = await InitKeyValuesAsync(
                repositoryEntity,
                placeholders,
                repository.Tag,
                keyRepoName,
                true);
            await SaveKeyValuesAsync(keyValues, userEmail, userIp, isProduction);

            var repositoriesModel = await GetPaginatedRepositories();
            var repositories = repositoriesModel.Data as PaginatedList<IRepository>;

            return new RepositoriesServiceModel
            {
                Result = UpdateSettingsStatus.Ok,
                Json = JsonConvert.SerializeObject(repositories),
                Data = new
                {
                    repositories.PageIndex,
                    repositories.TotalPages
                }
            };
        }

        public async Task<RepositoriesServiceModel> UpdateRepositoryAsync(
            IRepository repository,
            string userName,
            string userIp,
            string userEmail,
            bool isProduction,
            string search = null)
        {
            var repositoryEntity = await _repositoriesRepository.GetAsync(repository.RowKey);
            if (repositoryEntity == null)
                return new RepositoriesServiceModel { Result = UpdateSettingsStatus.NotFound };

            repository.GitUrl = repositoryEntity.GitUrl;
            repository.Tag = repositoryEntity.Tag;

            //check if type if github or bitbucket. since we have only github and bitbucket I am checking url for github.com
            var type = repository.GitUrl.Contains(GITHUB_URL) ? SourceControlTypes.Github : SourceControlTypes.Bitbucket;

            //get gitUrl for raw json format
            var settingsGitUrl = GitServices.GitServices.GenerateRepositorySettingsGitUrl(repository.GitUrl, type, repository.Branch);

            var name = GitServices.GitServices.GetGitRepositoryName(repository.GitUrl, type);

            string fileFullName = FILENAME;

            fileFullName += type == SourceControlTypes.Github ? "git_" : "bb_";
            fileFullName += name + "_" + repository.Branch;
            fileFullName += repository.Tag == null ? FILE_FORMAT : $"_{repository.Tag}{FILE_FORMAT}";

            repositoryEntity = new RepositoryEntity
            {
                RowKey = Guid.NewGuid().ToString(),
                GitUrl = repository.GitUrl,
                Branch = repository.Branch,
                FileName = fileFullName,
                UserName = userName,
                Name = name,
                OriginalName = name,
                Tag = repository.Tag
            };

            var last = await _repositoriesUpdateHistoryRepository.GetAsync(repository.RowKey);
            repositoryEntity.ConnectionUrl = _repositoryFileInfoControllerAction + last.InitialCommit + "/"
                + (!string.IsNullOrWhiteSpace(repositoryEntity.Tag) ? repositoryEntity.Tag + "/" : string.Empty) + name;

            //get json from generated gitUrl
            var settingsResult = GitServices.GitServices.DownloadSettingsFileFromGit(
                _log,
                settingsGitUrl,
                type,
                _appSettings.BitBucketSettings?.BitbucketEmail,
                _appSettings.BitBucketSettings?.BitbucketPassword);

            if (!settingsResult.Success)
                return new RepositoriesServiceModel
                {
                    Result = UpdateSettingsStatus.GitDownloadError,
                    Message = "Couldn't download settings file from git",
                    Data = settingsResult.Message,
                };

            string settingsYaml = settingsResult.Data.ToString();
            var settings = settingsYaml.GetSettingsDataFromYaml();
            if (!settings.Success)
                return new RepositoriesServiceModel
                {
                    Result = UpdateSettingsStatus.YamlProcessingError,
                    Message = "Couldn't get settings data from yaml",
                    Data = settings.Message,
                };

            var settingsJson = (settings.Data as DataFromYaml)?.Json;

            // update key values
            var placeholders = (settings.Data as DataFromYaml)?.Placeholders;
            var keyRepoName = !string.IsNullOrEmpty(repositoryEntity?.Tag) ? repositoryEntity?.Tag + "-" + repositoryEntity?.OriginalName : repositoryEntity?.OriginalName;
            var keyValues = await InitKeyValuesAsync(
                repositoryEntity,
                placeholders,
                repository.Tag,
                keyRepoName,
                false);

            //no need to update if files are the same
            var existingBlob = await GetFileData(fileFullName);
            bool isDuplicated;
            if (existingBlob == settingsJson)
            {
                var oldKeyValues = await _keyValuesRepository.GetKeyValuesAsync(x => x.RepositoryNames != null && x.RepositoryNames.Contains(keyRepoName));
                if (oldKeyValues.Count() != keyValues.Count)
                {
                    isDuplicated = false;
                }
                else
                {
                    var changesCount = 0;
                    foreach (var item in keyValues)
                    {
                        var oldItem = oldKeyValues.FirstOrDefault(x => x.RowKey == item.RowKey);
                        var areTypesEqual = oldItem != null && oldItem.Types == null && item.Types == null
                            || (oldItem.Types != null && item.Types != null && !oldItem.Types.Except(item.Types).Any() && !item.Types.Except(oldItem.Types).Any());
                        if (oldItem == null || !areTypesEqual)
                            changesCount++;
                    }
                    isDuplicated = changesCount == 0;
                }
            }
            else
            {
                isDuplicated = false;
            }

            if (isDuplicated)
                return new RepositoriesServiceModel
                {
                    Result = UpdateSettingsStatus.HasDuplicated
                };

            //Adding data to history repository
            await AddToHistoryRepository(repositoryEntity, settingsJson, repository.RowKey, userName: userName, userIp: userIp);

            //delete repository to add updated one
            await _repositoriesRepository.RemoveRepositoryAsync(repository.RowKey);

            // if updating file, we must not create new name for it
            await _repositoryDataRepository.UpdateBlobAsync(settingsJson, userName, userIp, fileFullName);

            await _repositoriesRepository.SaveRepositoryAsync(repositoryEntity);
            var repositoriesModel = await GetPaginatedRepositories(search);
            var repositories = repositoriesModel.Data as PaginatedList<IRepository>;

            //await _keyValuesRepository.UpdateKeyValueAsync(keyValues);
            await SaveKeyValuesAsync(keyValues, userEmail, userIp, isProduction);

            existingBlob = existingBlob.ReleaseFromComments();
            settingsJson = settingsJson.ReleaseFromComments();

            //If Update remove checking for placeholders change if any deleted fix RepositoryNames for that
            var lastPlaceholders = existingBlob.PlaceholderList();

            for (int i = 0; i < lastPlaceholders.Count; i++)
            {
                foreach (var item in keyValues)
                {
                    if (lastPlaceholders[i].RowKey != null
                        && lastPlaceholders[i].RowKey == (string.IsNullOrEmpty(repository.Tag) ? item.RowKey : item.RowKey.SubstringFromString(item.Tag + "-")))
                    {
                        lastPlaceholders.Remove(lastPlaceholders[i]);
                        --i;
                        break;
                    }
                }
            }

            if (lastPlaceholders.Count > 0)
            {
                var keyValuesToUpdate = new List<IKeyValueEntity>();
                foreach (var lastItem in lastPlaceholders)
                {
                    var keyValueToUpdate = await _keyValuesRepository.GetKeyValueAsync(string.IsNullOrEmpty(repository.Tag)
                        ? lastItem.RowKey
                        : repository.Tag + "-" + lastItem.RowKey);
                    if (keyValueToUpdate != null)
                    {
                        var tempRepoNames = keyValueToUpdate.RepositoryNames?.ToList();
                        tempRepoNames?.Remove(keyRepoName);
                        keyValueToUpdate.RepositoryNames = tempRepoNames != null && tempRepoNames.Count > 0 ? tempRepoNames.ToArray() : null;
                        keyValuesToUpdate.Add(keyValueToUpdate);
                    }
                }
                await _keyValuesRepository.UpdateKeyValueAsync(keyValuesToUpdate);
            }

            return new RepositoriesServiceModel
            {
                Result = UpdateSettingsStatus.Ok,
                Json = JsonConvert.SerializeObject(repositories),
                Data = new
                {
                    Last = existingBlob,
                    Current = settingsJson,
                    Oldid = repository.RowKey,
                    Newid = repositoryEntity.RowKey,
                    repositories.PageIndex,
                    repositories.TotalPages
                }
            };
        }
    }
}
