using Core.Entities;

namespace Web.Models
{
    public class ServiceTokenModel : IServiceTokenEntity
    {
        public string Token { get; set; }
        public string SecurityKeyOne { get; set; }
        public string SecurityKeyTwo { get; set; }
    }
}
