using System.Threading.Tasks;

namespace Core.Services
{
    public interface IStartupManager
    {
        Task StartAsync();
    }
}