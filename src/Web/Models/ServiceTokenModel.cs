using System.Collections.Generic;
using Core.Entities;

namespace Web.Models
{
    public class ServiceTokenModel
    {
        public List<IServiceTokenEntity> Tokens { get; set; }
    }
}
