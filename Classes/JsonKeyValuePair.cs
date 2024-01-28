using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SRXDFolderSwitcher.Classes
{
    public class JsonKeyValuePair<TKey, TValue>
    {
        [JsonProperty("Folder label")]
        public TKey Key { get; set; }

        [JsonProperty("Folder path")]
        public TValue Value { get; set; }

        public JsonKeyValuePair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }
}
