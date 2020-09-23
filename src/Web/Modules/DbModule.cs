using Autofac;
using AzureRepositories;
using Web.Settings;

namespace Web.Modules
{
    public class DbModule : Module
    {
        private readonly AppSettings _settings;

        public DbModule(AppSettings settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterDbModule(
                _settings.Db.ConnectionString,
                _settings.Db.UserConnectionString,
                _settings.Db.SecretsConnString);
        }
    }
}
