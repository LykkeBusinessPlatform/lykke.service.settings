using System;
using System.Collections.Generic;
using System.Text;

namespace Web.Models
{
    public class KeyValueToUpdate
    {
        public string RowKey { get; set; }
        public string Value { get; set; }
        public bool Forced { get; set; }
    }
}
