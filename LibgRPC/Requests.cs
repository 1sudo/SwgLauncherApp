using System.Diagnostics;
using System.Net.Security;
using Grpc.Core;
using Grpc.Net.Client;

namespace LibgRPC;

public static class Requests
{
    private static GrpcChannel? _channel = null;
    public static Action<List<string>, string>? LoggedIn { get; set; }
    public static Action<string>? LoginFailed { get; set; }

    private static void GrpcInit()
    { 
        // Creates shared gRPC channel, ignoring self-signed errors
        _channel = GrpcChannel.ForAddress("https://localhost:7198/", new GrpcChannelOptions
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

        var client = new LoginManager.LoginManagerClient(_channel);

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

            if (response.Status == "ok") LoggedIn?.Invoke(response.Characters, response.Username!);
            else LoginFailed?.Invoke(response.Status!);
        }
        catch (Exception e)
        {
            Trace.WriteLine(e.StackTrace);
            LoginFailed?.Invoke("Unable to reach login server.");
        }
    }
}
