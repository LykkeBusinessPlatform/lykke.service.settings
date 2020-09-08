using System;

namespace Core.Entities
{
    public interface IRepositoryUpdateHistory : IEntity
    {
        string InitialCommit { get; }
        string User { get; }
        string Branch { get; }
        bool IsManual { get; }
        DateTimeOffset? CreatedAt { get; }
    }
}
