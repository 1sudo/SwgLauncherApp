using System;
using System.Threading.Tasks;
using DiscordRPC;

namespace LauncherApp.Models;

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
            client.OnReady += (sender, e) => Logger.Instance.Log("Discord Rich Presense Ready -> " + e.User.Username, INFO);
            client.OnPresenceUpdate += (sender, e) => Logger.Instance.Log("Discord Rich Presense Updated -> " + e.Presence, INFO);
            client.OnError += (sender, e) => Logger.Instance.Log("Discord Rich Presense Error ->" + e.Message, ERROR);

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
