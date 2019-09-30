using System;
using System.IO;
using System.Net;
using System.Text;
using Common.Log;
using Lykke.Common.Log;

namespace Services.GitServices
{
    public static class GitServices
    {
        private const string FILE_FORMAT_ON_GIT = ".yaml";
        private const string FILENAME = "settings";

        public static string GenerateRepositorySettingsGitUrl(string gitUrl, SourceControlTypes type, string branch = "")
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
            if (type == SourceControlTypes.Github)
            {
                repositoryUrl = gitUrl.Replace("github.com/", "api.github.com/repos/");

                if (repositoryUrl.Contains("blob/"))
                    repositoryUrl = repositoryUrl.Remove(repositoryUrl.IndexOf("blob/"));

                if (repositoryUrl.Contains("tree/" + branch))
                    repositoryUrl = repositoryUrl.Remove(repositoryUrl.IndexOf("tree/" + branch));

                repositoryUrl += "contents/" + settingsFileName;

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

        public static ServiceResult DownloadSettingsFileFromGit(
            ILog log,
            string url,
            SourceControlTypes type,
            string bitbutcketEmail,
            string bitbucketPassword)
        {
            // checking source control type
            if (type == SourceControlTypes.Github)
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.UserAgent = "Test";
                request.Accept = "application/vnd.github.v3.raw";
                try
                {
                    using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                    {
                        StreamReader reader = new StreamReader(response.GetResponseStream());
                        var json = reader.ReadToEnd();
                        return new ServiceResult { Success = true, Data = json };
                    }
                }
                catch(Exception ex)
                {
                    log.Error(ex, context: url);
                    return new ServiceResult { Success = false, Message = ex.Message };
                }
            }
            else
            {
                // we need to be authenticated on bitbucket to get access on repository. so, appending encoded username and password to request headers
                var uri = new Uri(url);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                String encoded = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(bitbutcketEmail + ":" + bitbucketPassword));
                request.Headers.Add("Authorization", "Basic " + encoded);

                try
                {
                    string result;
                    using (var response = request.GetResponse())
                    using (var stream = response.GetResponseStream())
                    using (var reader = new StreamReader(stream))
                    {
                        result = reader.ReadToEnd();
                    }

                    return new ServiceResult { Success = true, Data = result };
                }
                catch(Exception ex)
                {
                    log.Error(ex, context: url);
                    return new ServiceResult { Success = false, Message = ex.Message };
                }
            }
        }

        public static string GetGitRepositoryName(string gitUrl, SourceControlTypes type)
        {
            var gitType = type == SourceControlTypes.Github ? "github.com/" : "bitbucket.org/";
            var startIndex = gitUrl.IndexOf(gitType) + gitType.Length;

            // get git organization or user name. repository name is always after this account name
            var gitAccountEndIndex = gitUrl.IndexOf("/", startIndex);

            // length of forward slash "/"
            var separatorLength = 1;

            // get git repository name's endIndex
            var endIndex = gitUrl.IndexOf("/", gitAccountEndIndex + separatorLength);

            // substring repositoryName
            var length = (endIndex > 0 ? endIndex - gitAccountEndIndex : gitUrl.Length - gitAccountEndIndex) - separatorLength;
            return gitUrl.Substring(gitAccountEndIndex + separatorLength, length).Replace(".git", "");
        }
    }
}
