using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRXDFolderSwitcher.Classes
{
    public struct FileCollection
    {
        public string SrtbName { get; set; }

        public string AlbumArtFileName { get; set; }

        public List<string> AudioFileNames { get; set; }

        public void Dump()
        {

            Console.WriteLine("-- Chart info --");
            Console.WriteLine($"SRTB: {SrtbName}");
            Console.WriteLine($"Art file: {AlbumArtFileName}");
            Console.WriteLine($"Audio files: ");

            foreach (string item in AudioFileNames) 
            {
                Console.WriteLine($"{item}");
            }

        }

    }
}
