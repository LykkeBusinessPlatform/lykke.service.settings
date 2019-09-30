namespace Core.ServiceToken
{
    public interface IServiceTokenEntity : IEntity
    {
        string SecurityKeyOne { get; set; }
        string SecurityKeyTwo { get; set; }
    }
}
