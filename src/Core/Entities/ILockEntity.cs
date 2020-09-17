using System;

namespace Core.Entities
{
    public interface ILockEntity
    {
        DateTime DateTime { get; set; }
        string UserName { get; set; }
        string UserEmail { get; set; }
        string IpAddress { get; set; }
    }
}
