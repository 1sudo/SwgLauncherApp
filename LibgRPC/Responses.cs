using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibgRPC
{
    public class LoginResponse
    {
        public string? Status { get; set; }
        public string? Username { get; set; }
        public List<string>? Characters { get; set; }
    }
}
