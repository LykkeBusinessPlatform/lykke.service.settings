using Lykke.SettingsReader.Attributes;

namespace Shared.Settings
{
    public class AppSettings
    {
        public string SettingsServiceName { get; set; }
        public string UserConnectionString { get; set; }
        public string ConnectionString { get; set; }
        [Optional]
        public string SecretsConnString { get; set; }
        public int LockTimeInMinutes { get; set; }
        public int UserLoginTime { get; set; }
        public string DefaultPassword { get; set; }
        public string DefaultUserEmail { get; set; }
        public string DefaultUserFirstName { get; set; }
        public string DefaultUserLasttName { get; set; }
        public bool UseUnsafeTokens { get; set; }
        public string SecTokenPassword { get; set; }
        public string ApiClientId { get; set; }
        public string AvailableEmailsRegex { get; set; }
        public int KeyValuesShowsInterval { get; set; }
        public string SlackNotificationsConnString { get; set; }
        public string SlackNotificationsQueueName { get; set; }
        public string ApiSecurityKey { get; set; }
        // public bool IsProduction { get; set; }
        public SettingsUpdaterSettings SettingsUpdaterSettings { get; set; }
        public BitbucketSettings BitBucketSettings { get; set; }
    }

    public class RabbitMqSettings
    {
        [AmqpCheck]
        public string InputConnectionString { get; set; }

        [AmqpCheck]
        public string OutputConnectionString { get; set; }
    }

    public class SettingsUpdaterSettings
    {
        public string ConnectToRabbit { get; set; }
        public RabbitMqSettings Rabbit { get; set; }
        public string SettingsExchangeName { get; set; }
    }

    public class BitbucketSettings
    {
        public string BitbucketEmail { get; set; }
        public string BitbucketPassword { get; set; }
    }
}
