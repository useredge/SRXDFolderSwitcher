using System.Collections.Generic;

namespace SRXDFolderSwitcher.Classes
{
    internal struct ConfigSchema
    {

        public List<PathEntry<string, string>> Paths { get; set; }

        public string DefaultPreloadPath { get; set; }

    }
}
