using System.Threading.Tasks;

namespace Core.Lock
{
    public interface ILockRepository
    {
        Task<ILockEntity> GetJsonPageLockAsync();
        Task SetJsonPageLockAsync(string userEmail, string userName, string ipAddress);
        Task ResetJsonPageLockAsync();
    }
}
