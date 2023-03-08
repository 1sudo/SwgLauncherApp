using System.Net.Security;
using Grpc.Core;
using Grpc.Net.Client;
using LauncherWebService.Services;

namespace LibLauncherApp.gRPC;

public class Requests
{
    private static GrpcChannel? _channel = null;
    public static event EventHandler<OnLoggedInEventArgs>? OnLoggedIn;
    public static event EventHandler<OnLoginFailedEventArgs>? OnLoginFailed;
    public static event EventHandler<OnAccountCreatedEventArgs>? OnAccountCreated;
    public static event EventHandler<OnAccountCreateFailedEventArgs>? OnAccountCreateFailed;
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

    public async Task RequestLogin(string username, string password, bool autoLogin = false)
    {
        if (_channel is null) GrpcInit();

        var client = new AccountLoginManager.AccountLoginManagerClient(_channel);

        var reply = client.RequestLogin(new LoginRequest
        {
            Username = username,
            Password = password
        });

        LoginResponse? response = new();
        List<string> characters = new();

        try
        {
            // Read response, populate LoginResponse properties
            await foreach (var data in reply.ResponseStream.ReadAllAsync())
            {
                response.Status = data.Status;
                response.Username = data.Username;

                if (data.Characters?.Count > 0)
                {
                    characters?.Add(data.Characters[0]);
                }
            }

            response.Characters = characters;

            if (response.Status == "ok")
            {
                if (autoLogin)
                {
                    OnLoggedIn?.Invoke(this, new OnLoggedInEventArgs(response.Characters, response.Username!, true));
                }
                else
                {
                    OnLoggedIn?.Invoke(this, new OnLoggedInEventArgs(response.Characters, response.Username!, false));
                }
            }
            else
            {
                OnLoginFailed?.Invoke(this, new OnLoginFailedEventArgs(response.Status!));
            }
        }
        catch (Exception e)
        {
            Logger.Instance.Log(e, ERROR);
            OnLoginFailed?.Invoke(this, new OnLoginFailedEventArgs("Unable to reach login server."));
        }
    }

    public async Task RequestAccount(Models.AccountModel account)
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
            OnAccountCreated?.Invoke(this, new OnAccountCreatedEventArgs("Account Created Successfully."));
        }
        else
        {
            OnAccountCreateFailed?.Invoke(this, new OnAccountCreateFailedEventArgs(reply.Status));
        }
    }
}
