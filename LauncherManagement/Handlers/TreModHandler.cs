using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class TreModHandler : DatabaseHandler
    {
        internal async Task EnableMod(string modName, List<DownloadableFile> files)
        {
            List<DatabaseProperties.TreMods>? fileList = await ExecuteTreModsAsync
                (
                    "SELECT FileList " +
                    "FROM TreMods " +
                    $"where ModName = '{modName}';"
                );

            if (fileList is not null && fileList.Count > 0)
            {
                return;
            }

            StringBuilder sb = new();

            int i = 0;
            foreach (DownloadableFile file in files)
            {
                if (i < files.Count - 1)
                {
                    sb.Append(file.Name + ",");
                }
                else
                {
                    sb.Append(file.Name);
                }

                i += 1;
            }

            await ExecuteTreModsAsync
                (
                    "INSERT INTO TreMods " +
                    "(ModName, FileList)" +
                    "VALUES" +
                    $"('{modName}', '{sb}');"
                );
        }

        public async Task DisableMod(string modName)
        {
            await ExecuteTreModsAsync
                (
                    $"DELETE FROM TreMods WHERE ModName = '{modName}';"
                );
        }

        internal async Task<List<string>> GetModFileList(string modName)
        {
            List<DatabaseProperties.TreMods>? fileList = await ExecuteTreModsAsync
                (
                    "SELECT FileList " +
                    "FROM TreMods " +
                    $"where ModName = '{modName}';"
                );

            return fileList![0].FileList!.Split(",").ToList();
        }

        internal async Task<Dictionary<string, List<string>>> GetMods()
        {
            List<DatabaseProperties.TreMods>? modList = await ExecuteTreModsAsync
                (
                    "SELECT ModName " +
                    "FROM TreMods;"
                );

            Dictionary<string, List<string>> mods = new();

            if (modList is not null)
            {
                foreach (var mod in modList)
                {
                    if (mod.ModName is not null)
                    {
                        mods.Add(mod.ModName, await GetModFileList(mod.ModName));
                    }
                }
            }

            return mods;
        }
    }
}
