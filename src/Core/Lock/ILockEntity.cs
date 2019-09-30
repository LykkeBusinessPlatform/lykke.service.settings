using System;

namespace Core.Lock
{
    public interface ILockEntity: IEntity
    {
        DateTime DateTime { get; set; }
        string UserName { get; set; }
        string UserEmail { get; set; }
        string IpAddress { get; set; }
    }
}
