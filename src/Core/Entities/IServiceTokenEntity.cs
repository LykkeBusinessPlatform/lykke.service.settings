using System;

namespace Core.Entities
{
    public interface IServiceTokenEntity
    {
        public string Token { get; set; }
        public string SecurityKeyOne { get; set; }
        public string SecurityKeyTwo { get; set; }
    }
}
