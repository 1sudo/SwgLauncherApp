using System.Collections.Generic;

namespace LibLauncherApp.gRPC;

public class LoginResponse
{
    public string? Status { get; set; }
    public string? Username { get; set; }
    public List<string>? Characters { get; set; }
}
