//using System.Linq;
//using Autofac;
//using Common.Log;
//using Core.Command;
//using Lykke.SettingsReader;
//using Services;
//using Shared.RabbitPublishers;
//using Shared.RabbitSubscribers;
//using Shared.Settings;

//namespace Web.Modules
//{
//    public class RabbitModule : Module
//    {
//        private IReloadingManager<AppSettings> _settings;
//        private readonly ILog _log;
//        private readonly IConsole _console;
//        private readonly bool _canPublish;

//        public RabbitModule(IReloadingManager<AppSettings> settings, ILog log, IConsole console)
//        {
//            _settings = settings;
//            _log = log;
//            _console = console;
//            var connectToRabbit = _settings.CurrentValue.SettingsUpdaterSettings?.ConnectToRabbit?.ToLower();
//            var allowedVariables = new string[] { "true", "enabled", "yes" };
//            _canPublish = connectToRabbit != null && allowedVariables.Contains(connectToRabbit);
//        }

//        protected override void Load(ContainerBuilder builder)
//        {
//            //builder.RegisterInstance(_log)
//            //    .As<ILog>()
//            //    .SingleInstance();

//            //builder.RegisterInstance(_console)
//            //    .As<IConsole>()
//            //    .SingleInstance();

//            //builder.RegisterType<Commands>()
//            //    .As<ICommand>()
//            //    .SingleInstance();

//            if (_canPublish)
//                RegisterRabbitMqSubscribers(builder);

//            RegisterRabbitMqPublishers(builder);
//        }

//        private void RegisterRabbitMqSubscribers(ContainerBuilder builder)
//        {
//            builder.RegisterType<SettingsSubscriber>()
//                .As<IStartable>()
//                .AutoActivate()
//                .SingleInstance()
//                .WithParameter("connectionString", _settings.CurrentValue.SettingsUpdaterSettings.Rabbit.InputConnectionString)
//                .WithParameter("exchangeName", _settings.CurrentValue.SettingsUpdaterSettings.SettingsExchangeName)
//                .WithParameter("serviceName", _settings.CurrentValue.SettingsServiceName);
//        }

//        private void RegisterRabbitMqPublishers(ContainerBuilder builder)
//        {
//            builder.RegisterType<SettingsPublisher>()
//                .As<ISettingsPublisher>()
//                .As<IStartable>()
//                .AutoActivate()
//                .SingleInstance()
//                .WithParameter("connectionString", _settings.CurrentValue.SettingsUpdaterSettings.Rabbit.OutputConnectionString)
//                .WithParameter("exchangeName", _settings.CurrentValue.SettingsUpdaterSettings.SettingsExchangeName)
//                .WithParameter("serviceName", _settings.CurrentValue.SettingsServiceName)
//                .WithParameter("canPublish", _canPublish);
//        }
//    }
//}
