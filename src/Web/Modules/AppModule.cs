﻿using Autofac;
using Core.Repository;
using Core.Services;
using Services.GitServices;
using Services.RepositoryServices;
using Web.Code;
using Web.Settings;

namespace Web.Modules
{
    public class AppModule : Module
    {
        private readonly AppSettings _settings;

        public AppModule(AppSettings settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_settings)
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<RepositoriesService>()
                .As<IRepositoriesService>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(!string.IsNullOrEmpty(_settings.Db.SecretsConnString)));

            builder.RegisterType<GitService>()
                .As<IGitService>()
                .SingleInstance()
                .WithParameter("gitHubToken", _settings.GitSettings.GitHubToken)
                .WithParameter("bitbucketEmail", _settings.GitSettings.BitbucketEmail)
                .WithParameter("bitbucketPassword", _settings.GitSettings.BitbucketPassword);

            builder.RegisterType<SelfTestService>()
                .AsSelf()
                .SingleInstance();
        }
    }
}
