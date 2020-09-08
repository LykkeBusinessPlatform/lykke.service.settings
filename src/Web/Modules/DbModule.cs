using Autofac;
using AzureRepositories.ApplicationSettings;
using AzureRepositories.Blob;
using AzureRepositories.KeyValue;
using AzureRepositories.Lock;
using AzureRepositories.Networks;
using AzureRepositories.Repository;
using AzureRepositories.ServiceToken;
using AzureRepositories.Token;
using AzureRepositories.User;
using AzureStorage.Tables;
using Core.Repositories;
using Lykke.Common.Log;
using Lykke.SettingsReader;
using Lykke.SettingsReader.ReloadingManager;
using Web.Settings;

namespace Web.Modules
{
    public class DbModule : Module
    {
        private readonly IReloadingManager<AppSettings> _settingsManager;

        public DbModule(AppSettings settings)
        {
            _settingsManager = ConstantReloadingManager.From(settings);
        }

        protected override void Load(ContainerBuilder builder)
        {
            var connectionString = _settingsManager.ConnectionString(x => x.Db.ConnectionString);
            var userConnectionString = _settingsManager.ConnectionString(x => x.Db.UserConnectionString);
            var secretsConnString = _settingsManager.ConnectionString(x => x.Db.SecretsConnString);

            builder.Register(c=>
                new ServiceTokenRepository(
                    AzureTableStorage<ServiceTokenEntity>.Create(connectionString, "ServiceToken", c.Resolve<ILogFactory>())))
                .As<IServiceTokenRepository>()
                .SingleInstance();

            builder.Register(c=>
                new UserRepository(
                    AzureTableStorage<UserEntity>.Create(userConnectionString, "User", c.Resolve<ILogFactory>())))
                .As<IUserRepository>()
                .SingleInstance();

            builder.Register(c=>
                new RoleRepository(
                    AzureTableStorage<RoleEntity>.Create(userConnectionString, "Role", c.Resolve<ILogFactory>())))
                .As<IRoleRepository>()
                .SingleInstance();

            builder.Register(c=>
                new UserSignInHistoryRepository(
                    AzureTableStorage<UserSignInHistoryEntity>.Create(userConnectionString, "UserSignInHistory", c.Resolve<ILogFactory>())))
                .As<IUserSignInHistoryRepository>()
                .SingleInstance();

            builder.Register(c=>
                new TokensRepository(
                    AzureTableStorage<TokenEntity>.Create(connectionString, "Tokens", c.Resolve<ILogFactory>())))
                .As<ITokensRepository>()
                .SingleInstance();

            builder.Register(c =>
                new KeyValuesRepository(
                    AzureTableStorage<KeyValueEntity>.Create(connectionString, "KeyValues", c.Resolve<ILogFactory>()),
                    c.Resolve<IKeyValueHistoryRepository>()))
                .As<IKeyValuesRepository>()
                .SingleInstance();

            builder.Register(c=>
                new KeyValuesRepository(
                    AzureTableStorage<KeyValueEntity>.Create(secretsConnString, "SecretKeyValues", c.Resolve<ILogFactory>()),
                    c.Resolve<IKeyValueHistoryRepository>()))
                .As<ISecretKeyValuesRepository>()
                .SingleInstance();

            builder.Register(c=>
                    new LockRepository(AzureTableStorage<LockEntity>.Create(connectionString, "Lock", c.Resolve<ILogFactory>())))
                .As<ILockRepository>()
                .SingleInstance();

            builder.Register(c=>
                new AccountTokenHistoryRepository(
                    AzureTableStorage<AccountTokenHistoryEntity>.Create(connectionString, "AccessTokenHistory", c.Resolve<ILogFactory>())))
                .As<IAccountTokenHistoryRepository>()
                .SingleInstance();

            builder.Register(c=>
                new ServiceTokenHistoryRepository(
                    AzureTableStorage<ServiceTokenHistoryEntity>.Create(connectionString, "ServiceTokenHistory", c.Resolve<ILogFactory>())))
                .As<IServiceTokenHistoryRepository>()
                .SingleInstance();

            builder.Register(c=>
                new RepositoriesRepository(
                    AzureTableStorage<RepositoryEntity>.Create(connectionString, "Repositories", c.Resolve<ILogFactory>())))
                .As<IRepositoriesRepository>()
                .SingleInstance();

            builder.Register(c=>
                new ConnectionUrlHistoryRepository(
                    AzureTableStorage<ConnectionUrlHistory>.Create(connectionString, "ConnectionUrlHistory", c.Resolve<ILogFactory>())))
                .As<IConnectionUrlHistoryRepository>()
                .SingleInstance();

            builder.Register(c=>
                new RepositoriesUpdateHistoryRepository(
                    AzureTableStorage<RepositoryUpdateHistory>.Create(connectionString, "RepositoryUpdateHistory", c.Resolve<ILogFactory>())))
                .As<IRepositoriesUpdateHistoryRepository>()
                .SingleInstance();

            builder.Register(c=>
                new NetworkRepository(
                    AzureTableStorage<NetworkEntity>.Create(connectionString, "Networks", c.Resolve<ILogFactory>())))
                .As<INetworkRepository>()
                .SingleInstance();

            builder.Register(c=>
                new ApplicationSettingsRepository(
                    AzureTableStorage<ApplicationSettingsEntity>.Create(connectionString, "ApplicationSettings",  c.Resolve<ILogFactory>())))
                .As<IApplicationSettingsRepostiory>()
                .SingleInstance();

            builder.Register(c =>
                new UserActionHistoryRepository(
                    AzureTableStorage<UserActionHistoryEntity>.Create(userConnectionString, "UserActionHistory", c.Resolve<ILogFactory>()),
                    new AzureBlobStorage(userConnectionString.CurrentValue), "useractionhistoryparam"))
                .As<IUserActionHistoryRepository>()
                .SingleInstance();

            builder.Register(c =>
                new KeyValueHistoryRepository(
                    AzureTableStorage<KeyValueHistory>.Create(connectionString, "KeyValueHistory", c.Resolve<ILogFactory>()),
                    new AzureBlobStorage(connectionString.CurrentValue), "keyvaluehistory"))
                .As<IKeyValueHistoryRepository>()
                .SingleInstance();

            builder.RegisterInstance(
                new RepositoryDataRepository(new AzureBlobStorage(connectionString.CurrentValue), "settings", "history")
            ).As<IRepositoryDataRepository>().SingleInstance();

            builder.RegisterInstance(
                new JsonDataRepository(new AzureBlobStorage(connectionString.CurrentValue), "settings", "history", "generalsettings.json")
            ).As<IJsonDataRepository>().SingleInstance();

            builder.RegisterInstance(
                new AccessDataRepository(new AzureBlobStorage(connectionString.CurrentValue), "access", "accesshistory", "accessHistory.json")
            ).As<IAccessDataRepository>().SingleInstance();

            //var kvHistory = new KeyValueHistoryRepository(
            //    AzureTableStorage<KeyValueHistory>.Create(_connectionString, "KeyValueHistory", _log),
            //    new AzureBlobStorage(_connectionString.CurrentValue), "keyvaluehistory");
        }
    }
}
