using Autofac;
using Lykke.SettingsReader;
using Shared.Settings;
using Core.Repository;
using Services.RepositoryServices;

namespace Web.Modules
{
    public class AppModule : Module
    {
        private readonly IReloadingManager<AppSettings> _settings;

        public AppModule(IReloadingManager<AppSettings> settings)
        {
            _settings = settings;
            var allowedVariables = new string[] { "true", "enabled", "yes" };
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_settings.CurrentValue)
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<RepositoriesService>()
                .As<IRepositoriesService>()
                .SingleInstance();
        }
    }
}
