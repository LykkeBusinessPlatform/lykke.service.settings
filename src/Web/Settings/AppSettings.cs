using Lykke.SettingsReader.Attributes;

namespace Web.Settings
{
    public class AppSettings
    {
        public DbSettings Db { get; set; }
        public int LockTimeInMinutes { get; set; }
        public int UserLoginTime { get; set; }
        public string DefaultPassword { get; set; }
        public string DefaultUserEmail { get; set; }
        public string GoogleApiClientId { get; set; }
        public string AvailableEmailsRegex { get; set; }
        [Optional]
        public string SlackNotificationsConnString { get; set; }
        [Optional]
        public string SlackNotificationsQueueName { get; set; }
        public GitSettings GitSettings { get; set; }
    }

    public enum DbType
    {
        AzureStorageTables,
        Postgres,
    }

    public class DbSettings
    {
        public DbType DbType { get; set; }
        public string UserConnectionString { get; set; }
        public string ConnectionString { get; set; }
        [Optional]
        public string SecretsConnString { get; set; }
    }

    public class GitSettings
    {
        [Optional]
        public string GitHubToken { get; set; }
        [Optional]
        public string BitbucketEmail { get; set; }
        [Optional]
        public string BitbucketPassword { get; set; }
    }
}
