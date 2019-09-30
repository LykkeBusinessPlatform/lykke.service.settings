using System;
using System.Collections.Generic;
using System.Text;
using AzureRepositories.User;
using Core.User;

namespace Web.Models
{
    public class RoleModel
    {
        public string RowKey { get; set; }
        public string Name { get; set; }
        public RoleKeyValue[] KeyValues { get; set; }
    }
}
