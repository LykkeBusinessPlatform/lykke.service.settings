using Core.Entities;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IApplicationSettingsRepostiory
    {
        Task<IApplicationSettingsEntity> GetAsync();
        Task SaveApplicationSettings(IApplicationSettingsEntity entity);
    }
}
