using System;

namespace Core.User
{
    public interface IUserSignInHistoryEntity
    {
        string UserEmail { get; set; }
        DateTime SignInDate { get; set; }
        string IpAddress { get; set; }
    }
}
