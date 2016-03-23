using System.Collections.Generic;

using Newtonsoft.Json;

namespace Database.CodeGen
{
    public sealed class Config
    {
        [JsonProperty("connection")]
        public Config_Connection Connection { get; set; }

        [JsonProperty("code")]
        public Config_Code Code { get; set; }

        [JsonProperty("output")]
        public Config_Output Output { get; set; }
    }

    public sealed class Config_Connection
    {
        [JsonProperty("connection-string")]
        public string ConnectionString { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public sealed class Config_Code
    {
        [JsonProperty("namespace")]
        public string Ns { get; set; }

        [JsonProperty("wrapper-class")]
        public string WrapperClass { get; set; }

        [JsonProperty("exclude-schemas")]
        public List<string> ExcludeSchemas { get; } = new List<string>();

        [JsonProperty("entity-suffix")]
        public string EntitySuffix { get; set; }
    }

    public sealed class Config_Output
    {
        [JsonProperty("constants")]
        public string Constants { get; set; }

        [JsonProperty("entities")]
        public string Entities { get; set; }
    }
}