using System;
using System.Threading.Tasks;
using DiscordRPC;

namespace LauncherApp.Models.Handlers;

public class DiscordHandler
{
    public DiscordRpcClient? client;

    readonly TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);

    public async Task Initialize()
    {
        await Task.Run(() =>
        {
            client = new("929878372975263784");

            //Subscribe to events
            client.OnReady += async (sender, e) => await LogHandler.Log(LogType.INFO, "| Discord Rich Presense | " + e.User.Username);
            client.OnPresenceUpdate += async (sender, e) => await LogHandler.Log(LogType.INFO, "| Discord Rich Presense | " + e.Presence);
            client.OnError += async (sender, e) => await LogHandler.Log(LogType.ERROR, "| Discord Rich Presense |" + e.Message);

            //Connect to the RPC
            client.Initialize();

            //Set the rich presence
            client.SetPresence(new RichPresence()
            {
                Details = "Building The Foundation",
                State = "Developing",
                Timestamps = new Timestamps()
                {
                    StartUnixMilliseconds = (ulong)t.TotalSeconds
                },
                Assets = new Assets()
                {
                    LargeImageKey = "legacy512",
                    LargeImageText = "SWG Legacy",
                }
            });

            // Get recurring updates
            var timer = new System.Timers.Timer(5000);
            timer.Elapsed += (sender, args) => { client.Invoke(); };
            timer.Start();
        });
    }
}
