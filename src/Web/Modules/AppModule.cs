using Autofac;
using Common.Log;
using Core.Command;
using Core.Services;
using Lykke.Job.LykkeJob.Services;
using Lykke.SettingsReader;
using Services;
using System.Linq;
using Shared.Settings;
using Shared.Commands;
using Shared.RabbitSubscribers;
using Shared.RabbitPublishers;
using Core.Repository;
using Services.RepositoryServices;

namespace Web.Modules
{
    public class AppModule : Module
    {
        private readonly IReloadingManager<AppSettings> _settings;
        private readonly bool _canPublish;

        public AppModule(IReloadingManager<AppSettings> settings)
        {
            _settings = settings;
            var connectToRabbit = _settings.CurrentValue.SettingsUpdaterSettings?.ConnectToRabbit?.ToLower();
            var allowedVariables = new string[] { "true", "enabled", "yes" };
            _canPublish = connectToRabbit != null && allowedVariables.Contains(connectToRabbit);
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<Commands>()
                .As<ICommand>()
                .SingleInstance();

            builder.RegisterInstance(_settings.CurrentValue)
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<RepositoriesService>()
                .As<IRepositoriesService>()
                .SingleInstance();

            if (_canPublish)
                RegisterRabbitMqSubscribers(builder);

            RegisterRabbitMqPublishers(builder);
            // await Task.CompletedTask;

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>()
                .WithParameter("canPublish", _canPublish);
        }

        private void RegisterRabbitMqSubscribers(ContainerBuilder builder)
        {
            builder.RegisterType<SettingsSubscriber>()
                .As<ISettingsSubscriber>()
                .AutoActivate()
                .SingleInstance()
                .WithParameter("connectionString", _settings.CurrentValue.SettingsUpdaterSettings.Rabbit.InputConnectionString)
                .WithParameter("exchangeName", _settings.CurrentValue.SettingsUpdaterSettings.SettingsExchangeName)
                .WithParameter("serviceName", _settings.CurrentValue.SettingsServiceName);
        }

        private void RegisterRabbitMqPublishers(ContainerBuilder builder)
        {
            builder.RegisterType<SettingsPublisher>()
                .As<ISettingsPublisher>()
                .AutoActivate()
                .SingleInstance()
                .WithParameter("connectionString", _settings.CurrentValue.SettingsUpdaterSettings.Rabbit.OutputConnectionString)
                .WithParameter("exchangeName", _settings.CurrentValue.SettingsUpdaterSettings.SettingsExchangeName)
                .WithParameter("serviceName", _settings.CurrentValue.SettingsServiceName)
                .WithParameter("canPublish", _canPublish);
        }
    }
}
