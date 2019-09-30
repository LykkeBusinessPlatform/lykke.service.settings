using System;

namespace Core.Repository
{
    public interface IRepository : IEntity
    {
        string LastModified { get; set; }
        string Name { get; set; }
        string GitUrl { get; set; }
        string Branch { get; set; }
        string FileName { get; set; }
        string UserName { get; set; }
        string ConnectionUrl { get; set; }
        string OriginalName { get; set; }
        bool UseManualSettings { get; set; }
        string Tag { get; set; }
        DateTimeOffset Timestamp { get; set; }
    }
}
