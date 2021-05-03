using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LauncherManagement
{
    class JsonHandler
    {
        internal static List<DownloadableFile> GetFileList(string listData)
        {
            List<DownloadableFile> fileList = new List<DownloadableFile>();

            // Parses a JSON array and iterates through items in the array
            foreach (var item in JArray.Parse(listData))
            {
                // Deserialize the JSON string, add it to a new 'DownloadableFile' object and add it to the file list
                DownloadableFile downloadableFile = JsonConvert.DeserializeObject<DownloadableFile>(item.ToString());

                fileList.Add(downloadableFile);
            }

            return fileList;
        }
    }
}
