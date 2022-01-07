using System.Text.Json.Serialization;

namespace LauncherManagement
{
    public struct DownloadableFile
    {
        [JsonPropertyName("Name")]
        public string Name { get; set; }

        [JsonPropertyName("Size")]
        public long Size { get; set; }

        [JsonPropertyName("Md5")]
        public string Md5 { get; set; }
    }
}
