namespace Core.Networks
{
    public interface INetwork
    {
        string Id { get; }
        string Name { get; }
        string Ip { get; }
    }
}
