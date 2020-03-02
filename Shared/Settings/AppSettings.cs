using Lykke.SettingsReader.Attributes;

namespace Shared.Settings
{
    public class AppSettings
    {
        public string UserConnectionString { get; set; }
        public string ConnectionString { get; set; }
        [Optional]
        public string SecretsConnString { get; set; }
        public int LockTimeInMinutes { get; set; }
        public string DefaultPassword { get; set; }
        public string DefaultUserEmail { get; set; }
        public string DefaultUserFirstName { get; set; }
        public string DefaultUserLastName { get; set; }
        public string ApiClientId { get; set; }
        public string AvailableEmailsRegex { get; set; }
        public string SlackNotificationsConnString { get; set; }
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
