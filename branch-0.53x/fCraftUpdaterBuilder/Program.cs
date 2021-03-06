﻿using System.IO;
using System.IO.Compression;

namespace fCraftUpdaterBuilder {
    static class Program {

        static readonly string[] FileList = {
            "ConfigTool.exe",
            "fCraft.dll",
            "fCraftConsole.exe",
            "fCraftUI.exe",
            "fCraftWinService.exe",
            "../../CHANGELOG.txt",
            "../../README.txt"
        };

        const string BinariesFileName = "../../fCraftUpdater/Resources/Payload.zip";


        static void Main() {
            FileInfo binaries = new FileInfo( BinariesFileName );
            if( binaries.Exists ) {
                binaries.Delete();
            }

            using( ZipStorer zs = ZipStorer.Create( binaries.FullName, "" ) ) {
                foreach( string file in FileList ) {
                    FileInfo fi = new FileInfo( file );
                    if( !fi.Exists ) {
                        return; // abort if any of the files do not exist
                    }
                    zs.AddFile( ZipStorer.Compression.Deflate, fi.FullName, fi.Name, "" );
                }
            }
        }
    }
}