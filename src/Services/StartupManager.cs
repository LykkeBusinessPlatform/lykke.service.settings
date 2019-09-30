using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Core.Services;
using Autofac;
using Shared.RabbitSubscribers;
using Shared.RabbitPublishers;
using Shared.Settings;
using Lykke.SettingsReader;
using System;
using System.Diagnostics;

namespace Lykke.Job.LykkeJob.Services
{
    // NOTE: Sometimes, startup process which is expressed explicitly is not just better, 
    // but the only way. If this is your case, use this class to manage startup.
    // For example, sometimes some state should be restored before any periodical handler will be started, 
    // or any incoming message will be processed and so on.
    // Do not forget to remove As<IStartable>() and AutoActivate() from DI registartions of services, 
    // which you want to startup explicitly.

    public class StartupManager : IStartupManager
    {
        private readonly ISettingsSubscriber _settingsSubscriber;
        private readonly ISettingsPublisher _settingsPublisher;
        private readonly bool _canPublish;

        public StartupManager(ISettingsPublisher settingsPublisher, bool canPublish)
        {
            Console.WriteLine("StartupManager without settingsSubscriber started");
            _settingsPublisher = settingsPublisher;
            _canPublish = canPublish;
        }

        public StartupManager(ISettingsSubscriber settingsSubscriber, ISettingsPublisher settingsPublisher, bool canPublish)
        {
            Console.WriteLine("StartupManager with settingsSubscriber started");
            _settingsSubscriber = settingsSubscriber;
            _settingsPublisher = settingsPublisher;
            _canPublish = canPublish;
        }

        public async Task StartAsync()
        {
            if (_canPublish)
                _settingsSubscriber.Start();

            _settingsPublisher.Start();
            //if (_canPublish)
            //    RegisterRabbitMqSubscribers(_builder);

            //RegisterRabbitMqPublishers(_builder);
            // await Task.CompletedTask;
        }

        //private void RegisterRabbitMqSubscribers(ContainerBuilder builder)
        //{
        //    builder.RegisterType<SettingsSubscriber>()
        //        .AutoActivate()
        //        .SingleInstance()
        //        .WithParameter("connectionString", _settings.CurrentValue.SettingsUpdaterSettings.Rabbit.InputConnectionString)
        //        .WithParameter("exchangeName", _settings.CurrentValue.SettingsUpdaterSettings.SettingsExchangeName)
        //        .WithParameter("serviceName", _settings.CurrentValue.SettingsServiceName);
        //}

        //private void RegisterRabbitMqPublishers(ContainerBuilder builder)
        //{
        //    builder.RegisterType<SettingsPublisher>()
        //        .AutoActivate()
        //        .SingleInstance()
        //        .WithParameter("connectionString", _settings.CurrentValue.SettingsUpdaterSettings.Rabbit.OutputConnectionString)
        //        .WithParameter("exchangeName", _settings.CurrentValue.SettingsUpdaterSettings.SettingsExchangeName)
        //        .WithParameter("serviceName", _settings.CurrentValue.SettingsServiceName)
        //        .WithParameter("canPublish", _canPublish);
        //}
    }
}
