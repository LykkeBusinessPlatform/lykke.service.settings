using System;
using Core.Entities;

namespace Core.Models
{
    public class UserActionHistory : IUserActionHistoryEntity
    {
        public string UserEmail { get; set; }
        public DateTime ActionDate { get; set; }
        public string IpAddress { get; set; }
        public string ControllerName { get; set; }
        public string ActionName { get; set; }
        public string Params { get; set; }
    }
}
