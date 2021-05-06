using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class AppHandler
    {
        public async Task StartGameAsync(GameOptionsProperties gameOptions, string serverPath, string password = "", 
            string username = "", string charactername = "", bool autoLogin = false)
        {
            // Run Async to prevent launcher from locking up when starting game and writing bytes
            await Task.Run(() =>
            {
                try
                {
                    // Read SWGEmu.exe and seek to 0x1153
                    string exePath = Path.Join(serverPath, "SWGEmu.exe");
                    FileStream rs = File.OpenRead(exePath);
                    rs.Seek(0x1153, SeekOrigin.Begin);

                    // Convert FPS integer (casted to float) to hex
                    string hexSelection = BitConverter.ToString(
                        BitConverter.GetBytes((float)gameOptions.Fps)).Replace("-", "");

                    // Read the next 3 bytes to ensure we're at the right position and 
                    // the binary hasn't been altered
                    if (rs.ReadByte() == 199 && rs.ReadByte() == 69 && rs.ReadByte() == 148)
                    {
                        string hex = "";
                        
                        // Get hex of next 4 bytes at 0x1153 (float)
                        for (int i = 0; i <= 3; i++)
                        {
                            byte[] b = { (byte)rs.ReadByte() };
                            hex += BitConverter.ToString(b).Replace("-", "");
                        }

                        // Close Read to prepare for write
                        rs.Dispose();

                        // If the selected FPS already matches hex value in the 
                        // binary, don't write to it again (faster loading)
                        if (hex != hexSelection)
                        {
                            using FileStream ws = File.OpenWrite(exePath);
                            ws.Seek(0x1156, SeekOrigin.Begin);

                            // Create byte array of FPS value
                            byte[] bytes = BitConverter.GetBytes((float)gameOptions.Fps);

                            // Write FPS float at 0x1156
                            foreach (byte b in bytes)
                            {
                                ws.WriteByte(b);
                            }
                        }
                    }

                    var startInfo = new ProcessStartInfo();

                    if (autoLogin)
                    {
                        startInfo.Arguments = $"-- -s ClientGame loginClientPassword={password} autoConnectToLoginServer=1 loginClientID={username} avatarName={charactername} autoConnectToGameServer=1 -s Station -s SwgClient allowMultipleInstances=true";
                    }

                    startInfo.EnvironmentVariables["SWGCLIENT_MEMORY_SIZE_MB"] = gameOptions.Ram.ToString();
                    startInfo.UseShellExecute = false;
                    startInfo.WorkingDirectory = serverPath;
                    startInfo.FileName = Path.Join(serverPath, "SWGEmu.exe");

                    Process.Start(startInfo);
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e.Message.ToString());
                }
            });
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
