using System.Diagnostics;
using System.Net.Security;
using Grpc.Core;
using Grpc.Net.Client;
using LauncherWebService.Services;

namespace LibgRPC;

public static class Requests
{
    private static GrpcChannel? _channel = null;
    public static Action<List<string>, string>? LoggedIn { get; set; }
    public static Action<string>? LoginFailed { get; set; }
    public static Action<string>? AccountCreated { get; set; }
    public static Action<string>? AccountCreationFailed { get; set; }
    public static string? GrpcUrl { get; set; }

    private static void GrpcInit()
    {
        if (GrpcUrl is null) return;

        // Creates shared gRPC channel, ignoring self-signed errors
        _channel = GrpcChannel.ForAddress(GrpcUrl, new GrpcChannelOptions
        {
            HttpHandler = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                EnableMultipleHttp2Connections = true,
                SslOptions = new SslClientAuthenticationOptions
                {
                    RemoteCertificateValidationCallback = delegate { return true; }
                }
            }
        });
    }

    public static async Task RequestLogin(string username, string password)
    {
        if (_channel is null) GrpcInit();

        var client = new AccountLoginManager.AccountLoginManagerClient(_channel);

        var reply = client.RequestLogin(new LoginRequest
        {
            Username = username,
            Password = password
        });

        LoginResponse response = new();
        List<string> characters = new();

        try
        {
            // Read response, populate LoginResponse properties
            await foreach (var data in reply.ResponseStream.ReadAllAsync())
            {
                response.Status = data.Status;
                response.Username = data.Username;
                if (data.Characters.Count > 0) characters.Add(data.Characters[0]);
            }

            response.Characters = characters;

            if (response.Status == "ok")
            {
                LoggedIn?.Invoke(response.Characters, response.Username!);
            }
            else
            {
                LoginFailed?.Invoke(response.Status!);
            }
        }
        catch (Exception e)
        {
            Trace.WriteLine(e.StackTrace);
            LoginFailed?.Invoke("Unable to reach login server.");
        }
    }

    public static async Task RequestAccount(Models.AccountModel account)
    {
        if (_channel is null) GrpcInit();

        var client = new AccountCreationManager.AccountCreationManagerClient(_channel);

        var reply = await client.RequestCreateAsync(new CreateRequest()
        {
            Username = account.username,
            Email = account.email,
            Password = account.password,
            SubscribeToNewsletter = account.subscribed,
            SecretQuestionAnswer = 0,
            DiscordId = account.discord
        });

        if (reply.Status == "ok")
        {
            AccountCreated?.Invoke("Account Created Successfully.");
        }
        else
        {
            AccountCreationFailed?.Invoke(reply.Status);
        }
    }
}
