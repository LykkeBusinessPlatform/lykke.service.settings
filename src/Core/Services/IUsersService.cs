using System.Threading.Tasks;
using Core.Entities;

namespace Core.Services
{
    public interface IUsersService
    {
        Task CheckInitialAdminAsync();

        Task CreateUserAsync(IUserEntity user);
    }
}
