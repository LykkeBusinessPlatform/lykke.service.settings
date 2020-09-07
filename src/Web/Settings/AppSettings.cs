using Lykke.SettingsReader.Attributes;

namespace Web.Settings
{
    public class AppSettings
    {
        public string UserConnectionString { get; set; }
        public string ConnectionString { get; set; }
        [Optional]
        public string SecretsConnString { get; set; }
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
        public BitbucketSettings BitBucketSettings { get; set; }
        public string GitHubToken { get; set; }
    }

    public class BitbucketSettings
    {
        public string BitbucketEmail { get; set; }
        public string BitbucketPassword { get; set; }
    }
}
