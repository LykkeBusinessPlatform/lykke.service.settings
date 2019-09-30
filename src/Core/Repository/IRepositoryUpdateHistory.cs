using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Repository
{
    public interface IRepositoryUpdateHistory : IEntity
    {
        string InitialCommit { get; set; }
        string User { get; set; }
        string Branch { get; set; }
        bool IsManual { get; set; }
    }
}
