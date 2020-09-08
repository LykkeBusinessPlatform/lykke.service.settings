namespace Core.Entities
{
    public interface IKeyValueHistory : IEntity
    {
        string KeyValueId { get; set; }
        string NewValue { get; set; }
        string NewOverride { get; set; }
        string KeyValuesSnapshot { get; set; }
        string UserName { get; set; }
        string UserIpAddress { get; set; }
    }
}
