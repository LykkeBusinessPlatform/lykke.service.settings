using System;
using System.Collections.Generic;
using System.Text;

namespace Services
{
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }
}
