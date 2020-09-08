using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Entities;
using Core.Repositories;

namespace PostgresRepositories.ApplicationSettings
{
    public class ApplicationSettingsRepository : IApplicationSettingsRepostiory
    {
        public Task<IApplicationSettingsEntity> GetAsync()
        {
            throw new NotImplementedException();
        }

        public Task SaveApplicationSettings(IApplicationSettingsEntity entity)
        {
            throw new NotImplementedException();
        }
    }
}
