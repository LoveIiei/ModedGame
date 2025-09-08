using System.Text.Json;
using System.Text.Json.Serialization;
using static ModedGame.Models.MCVersionInfo;

namespace ModedGame.Models
{
    public class MCVersionInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
        public string MainClass { get; set; }
        public MCLibrary[] Libraries { get; set; }
        public MCAssetIndex AssetIndex { get; set; }

        public class MCLibrary
        {
            public string Name { get; set; }
            public Dictionary<string, string> Natives { get; set; }
        }

        public class MCAssetIndex
        {
            public string Id { get; set; }
        }
    }
}
