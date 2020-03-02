using System;
using System.IO;
using System.Net;
using System.Text;
using Common.Log;
using Core;
using Core.Services;
using Lykke.Common.Log;
using Shared.Settings;

namespace Services.GitServices
{
    public class GitService : IGitService
    {
        private const string GITHUB_URL = "github.com";
        private const string GITHUB_FILES_STORAGE_PATH_PART = "blob";
        private const string BITBUCKET_URL = "bitbucket.org";
        private const string FILE_FORMAT_ON_GIT = ".yaml";
        private const string FILENAME = "settings";

        private readonly AppSettings _appSettings;
        private readonly Encoding _encoding = Encoding.GetEncoding("ISO-8859-1");

        public GitService(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        public SourceControlTypes ResolveSourceControlTypeFromUrl(string url)
        {
            if (url.Contains(GITHUB_URL))
                return SourceControlTypes.GithubPublic;
            if (url.Contains(BITBUCKET_URL))
                return SourceControlTypes.Bitbucket;
            if (url.Contains(GITHUB_FILES_STORAGE_PATH_PART))
                return SourceControlTypes.GithubPrivate;

            return SourceControlTypes.Bitbucket;
        }

        public string GenerateRepositorySettingsGitUrl(string gitUrl, SourceControlTypes type, string branch = "")
        {
            if (branch == "null")
                branch = "";

            //check for url that ends on ".git" and remove it
            if (gitUrl.EndsWith(".git"))
                gitUrl = gitUrl.Remove(gitUrl.Length - 4, 4);

            // setting json file name
            string settingsFileName; 
            if (gitUrl.EndsWith(FILE_FORMAT_ON_GIT))
            {
                settingsFileName = gitUrl.Remove(0, gitUrl.LastIndexOf('/') + 1);
                gitUrl = gitUrl.Substring(0, gitUrl.IndexOf(settingsFileName));
            }
            else
            {
                settingsFileName = FILENAME + FILE_FORMAT_ON_GIT;
            }

            if (!gitUrl.EndsWith("/"))
                gitUrl += "/";

            // to build url
            var repositoryUrl = string.Empty;

            //checking if file is uploaded on github or bitbucket
            if (type == SourceControlTypes.GithubPublic || type == SourceControlTypes.GithubPrivate)
            {
                var gitAccountStartIndex = gitUrl.IndexOf("/", 8);
                if (type == SourceControlTypes.GithubPublic)
                {
                    repositoryUrl = gitUrl.Insert(gitAccountStartIndex + 1, "repos/");
                    var domainStartIndex = gitUrl.LastIndexOf("/", 8);
                    repositoryUrl = repositoryUrl.Insert(domainStartIndex + 1, "api.");
                }
                else
                {
                    repositoryUrl = gitUrl.Insert(gitAccountStartIndex + 1, "api/v3/repos/");
                }

                var filePath = settingsFileName;

                var blobIndex = gitUrl.IndexOf("blob/", gitAccountStartIndex);
                if (blobIndex != -1)
                {
                    var searchStartIndex = blobIndex + 5;
                    if (searchStartIndex < gitUrl.Length)
                    {
                        var branchIndex = gitUrl.IndexOf("/", searchStartIndex);
                        if (branchIndex != -1)
                        {
                            var path = gitUrl.Substring(branchIndex + 1);
                            if (path != string.Empty)
                            {
                                if (!path.EndsWith(settingsFileName))
                                {
                                    if (path.EndsWith("/"))
                                        path += settingsFileName;
                                    else
                                        path += $"/{settingsFileName}";
                                }
                                filePath = path;
                            }
                        }
                    }
                    repositoryUrl = repositoryUrl.Remove(repositoryUrl.IndexOf("blob/"));
                }

                if (repositoryUrl.Contains("tree/" + branch))
                    repositoryUrl = repositoryUrl.Remove(repositoryUrl.IndexOf("tree/" + branch));

                repositoryUrl += "contents/" + filePath;

                if (!string.IsNullOrWhiteSpace(branch))
                    repositoryUrl += "?ref=" + branch;
            }
            else
            {
                //to delete "1230@" from links of type "1230@bitbucket.org/infrastructuredevelopers/settingsservicev2.git"
                if (gitUrl.Contains("@"))
                    gitUrl = gitUrl.Remove(gitUrl.IndexOf("://") + 3, gitUrl.IndexOf("@") - gitUrl.IndexOf("://") - 2);

                if (!gitUrl.EndsWith((branch + "/")))
                {
                    if (!gitUrl.EndsWith("src/"))
                        gitUrl += "src/";
                    gitUrl += branch + "/";
                }

                // to get json in raw format, we need to change bitbucket url from "src" to "raw"
                var bitbucketRawUrl = gitUrl.Replace("src", "raw");

                repositoryUrl += bitbucketRawUrl + settingsFileName;
            }

            return repositoryUrl;
        }

        public ServiceResult DownloadSettingsFileFromGit(
            ILog log,
            string url,
            SourceControlTypes type)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            switch (type)
            {
                case SourceControlTypes.GithubPublic:
                    request.UserAgent = "Test";
                    request.Accept = "application/vnd.github.v3.raw";
                    break;
                case SourceControlTypes.GithubPrivate:
                    request.Accept = "application/vnd.github.v3.raw";
                    request.Headers.Add("Authorization", "token " + _appSettings.GitHubToken);
                    break;
                case SourceControlTypes.Bitbucket:
                    // we need to be authenticated on bitbucket to get access on repository. so, appending encoded username and password to request headers
                    var credentials = _appSettings.BitBucketSettings.BitbucketEmail + ":" + _appSettings.BitBucketSettings.BitbucketPassword;
                    string encoded = Convert.ToBase64String(_encoding.GetBytes(credentials));
                    request.Headers.Add("Authorization", "Basic " + encoded);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            try
            {
                using (var response = request.GetResponse())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    return new ServiceResult { Success = true, Data = result };
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, context: url);
                return new ServiceResult { Success = false, Message = ex.Message };
            }
        }

        public string GetGitRepositoryName(string gitUrl, SourceControlTypes type)
        {
            // length of forward slash "/"
            var separatorLength = 1;
            // get git organization or user name. repository name is always after this account name
            var accountStartIndex = gitUrl.IndexOf("/", 8);
            var repoStartIndex = gitUrl.IndexOf("/", accountStartIndex + separatorLength);
            // get git repository name's endIndex
            var endIndex = gitUrl.IndexOf("/", repoStartIndex + separatorLength);

            // substring repositoryName
            var length = (endIndex > 0 ? endIndex - repoStartIndex : gitUrl.Length - repoStartIndex) - separatorLength;
            return gitUrl.Substring(repoStartIndex + separatorLength, length).Replace(".git", "");
        }
    }
}
