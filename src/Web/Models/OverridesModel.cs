using Core.Entities;
using Core.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Web.Models
{
    public class OverridesModel
    {
        public Network[] Networks { get; set; }
        public SelectListItem[] AvailableNetworks { get; set; }
        public IKeyValueEntity KeyValue { get; set; }
    }
}
