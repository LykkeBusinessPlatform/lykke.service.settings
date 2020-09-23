using Autofac;
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
using Lykke.AzureStorage.Tables.Entity.Metamodel;
using Lykke.AzureStorage.Tables.Entity.Metamodel.Providers;
using Lykke.Common.Log;
using Lykke.SettingsReader.ReloadingManager;

namespace AzureRepositories
{
    public static class AutofacExtensions
    {
        public static void RegisterDbModule(
            this ContainerBuilder builder,
            string connectionString,
            string userConnectionString,
            string secretsConnString)
        {
            var provider = new AnnotationsBasedMetamodelProvider();
            EntityMetamodel.Configure(provider);

            var conString = ConstantReloadingManager.From(connectionString);
            var userConString = ConstantReloadingManager.From(userConnectionString);
            var secretsConString = ConstantReloadingManager.From(secretsConnString);

            builder.Register(c =>
                new ServiceTokenRepository(
                    AzureTableStorage<ServiceTokenEntity>.Create(conString, "ServiceToken", c.Resolve<ILogFactory>())))
                .As<IServiceTokenRepository>()
                .SingleInstance();

            builder.Register(c =>
                new UserRepository(
                    AzureTableStorage<UserEntity>.Create(userConString, "User", c.Resolve<ILogFactory>())))
                .As<IUserRepository>()
                .SingleInstance();

            builder.Register(c =>
                new RoleRepository(
                    AzureTableStorage<RoleEntity>.Create(userConString, "Role", c.Resolve<ILogFactory>())))
                .As<IRoleRepository>()
                .SingleInstance();

            builder.Register(c =>
                new UserSignInHistoryRepository(
                    AzureTableStorage<UserSignInHistoryEntity>.Create(userConString, "UserSignInHistory", c.Resolve<ILogFactory>())))
                .As<IUserSignInHistoryRepository>()
                .SingleInstance();

            builder.Register(c =>
                new TokensRepository(
                    AzureTableStorage<TokenEntity>.Create(conString, "Tokens", c.Resolve<ILogFactory>())))
                .As<ITokensRepository>()
                .SingleInstance();

            builder.Register(c =>
                new KeyValuesRepository(
                    AzureTableStorage<KeyValueEntity>.Create(conString, "KeyValues", c.Resolve<ILogFactory>()),
                    c.Resolve<IKeyValueHistoryRepository>()))
                .As<IKeyValuesRepository>()
                .SingleInstance();

            builder.Register(c =>
                new KeyValuesRepository(
                    AzureTableStorage<KeyValueEntity>.Create(secretsConString, "SecretKeyValues", c.Resolve<ILogFactory>()),
                    c.Resolve<IKeyValueHistoryRepository>()))
                .As<ISecretKeyValuesRepository>()
                .SingleInstance();

            builder.Register(c =>
                    new LockRepository(AzureTableStorage<LockEntity>.Create(conString, "Lock", c.Resolve<ILogFactory>())))
                .As<ILockRepository>()
                .SingleInstance();

            builder.Register(c =>
                new AccountTokenHistoryRepository(
                    AzureTableStorage<AccountTokenHistoryEntity>.Create(conString, "AccessTokenHistory", c.Resolve<ILogFactory>())))
                .As<IAccountTokenHistoryRepository>()
                .SingleInstance();

            builder.Register(c =>
                new ServiceTokenHistoryRepository(
                    AzureTableStorage<ServiceTokenHistoryEntity>.Create(conString, "ServiceTokenHistory", c.Resolve<ILogFactory>())))
                .As<IServiceTokenHistoryRepository>()
                .SingleInstance();

            builder.Register(c =>
                new RepositoriesRepository(
                    AzureTableStorage<RepositoryEntity>.Create(conString, "Repositories", c.Resolve<ILogFactory>())))
                .As<IRepositoriesRepository>()
                .SingleInstance();

            builder.Register(c =>
                new ConnectionUrlHistoryRepository(
                    AzureTableStorage<ConnectionUrlHistory>.Create(conString, "ConnectionUrlHistory", c.Resolve<ILogFactory>())))
                .As<IConnectionUrlHistoryRepository>()
                .SingleInstance();

            builder.Register(c =>
                new RepositoriesUpdateHistoryRepository(
                    AzureTableStorage<RepositoryUpdateHistoryEntity>.Create(conString, "RepositoryUpdateHistory", c.Resolve<ILogFactory>())))
                .As<IRepositoriesUpdateHistoryRepository>()
                .SingleInstance();

            builder.Register(c =>
                new NetworkRepository(
                    AzureTableStorage<NetworkEntity>.Create(conString, "Networks", c.Resolve<ILogFactory>())))
                .As<INetworkRepository>()
                .SingleInstance();

            builder.Register(c =>
                new UserActionHistoryRepository(
                    AzureTableStorage<UserActionHistoryEntity>.Create(userConString, "UserActionHistory", c.Resolve<ILogFactory>()),
                    new AzureBlobStorage(userConString.CurrentValue), "useractionhistoryparam"))
                .As<IUserActionHistoryRepository>()
                .SingleInstance();

            builder.Register(c =>
                new KeyValueHistoryRepository(
                    AzureTableStorage<KeyValueHistory>.Create(conString, "KeyValueHistory", c.Resolve<ILogFactory>()),
                    new AzureBlobStorage(conString.CurrentValue), "keyvaluehistory"))
                .As<IKeyValueHistoryRepository>()
                .SingleInstance();

            builder.RegisterInstance(
                new RepositoryDataRepository(new AzureBlobStorage(conString.CurrentValue), "settings", "history")
            ).As<IRepositoryDataRepository>().SingleInstance();

            builder.RegisterInstance(
                new JsonDataRepository(new AzureBlobStorage(conString.CurrentValue), "settings", "history", "generalsettings.json")
            ).As<IJsonDataRepository>().SingleInstance();

            builder.RegisterInstance(
                new AccessDataRepository(new AzureBlobStorage(conString.CurrentValue), "access", "accesshistory", "accessHistory.json")
            ).As<IAccessDataRepository>().SingleInstance();
        }
    }
}
