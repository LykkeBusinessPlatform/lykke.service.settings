using System;

namespace Core.Entities
{
    public interface IUserActionHistoryEntity
    {
        string UserEmail { get; set; }
        DateTime ActionDate { get; set; }
        string IpAddress { get; set; }
        string ControllerName { get; set; }
        string ActionName { get; set; }
        string Params { get; set; }
    }
}
