using Autofac;
using Core.Repository;
using Core.Services;
using Lykke.SettingsReader;
using Services.GitServices;
using Services.RepositoryServices;
using Shared.Settings;

namespace Web.Modules
{
    public class AppModule : Module
    {
        private readonly IReloadingManager<AppSettings> _settings;

        public AppModule(IReloadingManager<AppSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_settings.CurrentValue)
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<RepositoriesService>()
                .As<IRepositoriesService>()
                .SingleInstance();

            builder.RegisterType<GitService>()
                .As<IGitService>()
                .SingleInstance();
        }
    }
}
