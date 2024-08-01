using System.Collections.Generic;
using Newtonsoft.Json;

namespace SRXDFolderSwitcher.Classes
{
    internal struct ConfigSchema
    {
        [JsonProperty("Custom paths")]
        public List<PathEntry<string, string>> Paths { get; set; }

        [JsonProperty("Preloaded path")]
        public string DefaultPreloadPath { get; set; }

    }
}
