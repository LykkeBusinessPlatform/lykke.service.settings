using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.ApplicationSettings
{
    public interface IApplicationSettingsRepostiory
    {
        Task<IApplicationSettingsEntity> GetAsync();
        Task SaveApplicationSettings(IApplicationSettingsEntity entity);
    }
}
