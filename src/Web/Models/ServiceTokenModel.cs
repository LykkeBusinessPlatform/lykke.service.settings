using System.Collections.Generic;
using Core.ServiceToken;

namespace Web.Models
{
    public class ServiceTokenModel
    {
        public List<IServiceTokenEntity> Tokens { get; set; }
    }
}
