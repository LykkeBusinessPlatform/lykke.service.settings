using System;
using System.Collections.Generic;
using System.Text;
using Core.Entities;
using Core.Enums;

namespace Web.Models
{
    public class ApiOverrideModel
    {
        public UpdateSettingsStatus Status { get; set; }
        public IEnumerable<IKeyValueEntity> KeyValues { get; set; }
        public IEnumerable<string> DuplicatedKeys { get; set; }
    }
}
