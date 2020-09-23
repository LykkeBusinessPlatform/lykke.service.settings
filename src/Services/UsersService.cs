using System.Threading.Tasks;
using Core.Entities;
using Core.Repositories;
using Core.Services;

namespace Services
{
    public class UsersService : IUsersService
    {
        private readonly IUserRepository _userRepository;
        private readonly string _defaultUserEmail;
        private readonly string _defaultUserPasswordHash;

        public UsersService(
            IUserRepository userRepository,
            string defaultUserEmail,
            string defaultPasswordHash)
        {
            _userRepository = userRepository;
            _defaultUserEmail = defaultUserEmail;
            _defaultUserPasswordHash = defaultPasswordHash;
        }

        public async Task CheckInitialAdminAsync()
        {
            var topUser = await _userRepository.GetTopUserRecordAsync();
            if (topUser == null)
                await _userRepository.CreateInitialAdminAsync(_defaultUserEmail, _defaultUserPasswordHash);
        }

        public Task CreateUserAsync(IUserEntity user)
        {
            user.PasswordHash = _defaultUserPasswordHash;

            return _userRepository.CreateUserAsync(user);
        }
    }
}
