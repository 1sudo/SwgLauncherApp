using System.Text.Json.Serialization;

namespace LauncherApp.Models.Properties
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
