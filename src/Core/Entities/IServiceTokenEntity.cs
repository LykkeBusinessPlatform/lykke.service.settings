namespace Core.Entities
{
    public interface IServiceTokenEntity : IEntity
    {
        string SecurityKeyOne { get; set; }
        string SecurityKeyTwo { get; set; }
    }
}
