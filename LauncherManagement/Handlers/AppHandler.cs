using System.Diagnostics;
using System.IO;

namespace LauncherManagement
{
    public class AppHandler
    {
        public void StartGame(string serverPath, string password = "", string username = "", string charactername = "", bool autoLogin = false)
        {
            try
            {
                var startInfo = new ProcessStartInfo();

                if (autoLogin)
                {
                    startInfo.Arguments = $"-- -s ClientGame loginClientPassword={password} autoConnectToLoginServer=1 loginClientID={username} avatarName={charactername} autoConnectToGameServer=1 -s Station -s SwgClient allowMultipleInstances=true";
                }

                startInfo.EnvironmentVariables["SWGCLIENT_MEMORY_SIZE_MB"] = "4096";
                startInfo.UseShellExecute = false;
                startInfo.WorkingDirectory = serverPath;
                startInfo.FileName = Path.Join(serverPath, "SWGEmu.exe");

                Process.Start(startInfo);
            }
            catch
            { }
        }

        public void StartGameConfig(string serverPath)
        {
            try
            {
                var startInfo = new ProcessStartInfo();

                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = serverPath;
                startInfo.FileName = Path.Join(serverPath, "SWGEmu_Setup.exe");

                Process.Start(startInfo);
            }
            catch
            { }
        }
    }
}
