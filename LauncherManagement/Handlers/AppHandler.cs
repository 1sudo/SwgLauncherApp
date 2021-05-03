using System.Diagnostics;
using System.IO;

namespace LauncherManagement
{
    public class AppHandler
    {
        public void StartGame(string serverPath)
        {
            try
            {
                var startInfo = new ProcessStartInfo();

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
