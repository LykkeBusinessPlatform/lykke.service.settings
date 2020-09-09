namespace Core.Entities
{
    public interface IKeyValueHistory
    {
        string DateTime { get; set; }
        string KeyValueId { get; set; }
        string NewValue { get; set; }
        string NewOverride { get; set; }
        string KeyValuesSnapshot { get; set; }
        string UserName { get; set; }
        string UserIpAddress { get; set; }
    }
}
