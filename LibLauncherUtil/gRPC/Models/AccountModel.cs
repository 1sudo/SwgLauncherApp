namespace LibLauncherUtil.gRPC.Models;

public class AccountModel
{
    public string? username { get; set; }
    public string? password { get; set; }
    public string? email { get; set; }
    public string? discord { get; set; }
    public int subscribed { get; set; }
}
