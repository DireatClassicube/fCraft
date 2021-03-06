﻿// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Contains fCraft path settings, and some filesystem-related utilities. </summary>
    public static class Paths {

        static readonly string[] ProtectedFiles;

        static Paths() {
            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            string assemblyDir = Path.GetDirectoryName( assemblyLocation );
            if( assemblyDir != null ) {
                WorkingPathDefault = Path.GetFullPath( assemblyDir );
            } else {
                WorkingPathDefault = Path.GetPathRoot( assemblyLocation );
            }

            WorkingPath = WorkingPathDefault;
            MapPath = MapPathDefault;
            LogPath = LogPathDefault;
            ConfigFileName = ConfigFileNameDefault;

            ProtectedFiles = new[]{
                "ConfigGUI.exe",
                "ConfigCLI.exe",
                "fCraft.Core.dll",
                "fCraft.GUI.dll",
                "ServerCLI.exe",
                "ServerGUI.exe",
                UpdaterFileName,
                ConfigFileNameDefault,
                PlayerDBFileName,
                IPBanListFileName,
                RulesFileName,
                AnnouncementsFileName,
                GreetingFileName,
                HeartbeatDataFileName,
                WorldListFileName,
                AutoRankFileName
            };
        }


        #region Paths & Properties

        public static bool IgnoreMapPathConfigKey { get; internal set; }

        public const string MapPathDefault = "maps",
                            LogPathDefault = "logs",
                            ConfigFileNameDefault = "config.json";

        public static readonly string WorkingPathDefault;

        public const string MySqlPlayerDBProviderModule = "fCraft.MySql.dll";
        public const string PythonPluginLoaderModule = "fCraft.Python.dll";

        /// <summary> Path to save maps to (default: .\maps)
        /// Can be overridden at startup via command-line argument "--mappath=",
        /// or via "MapPath" ConfigKey </summary>
        public static string MapPath { get; set; }

        /// <summary> Working path (default: whatever directory fCraft.dll is located in)
        /// Can be overridden at startup via command line argument "--path=" </summary>
        public static string WorkingPath { get; set; }

        /// <summary> Path to save logs to (default: .\logs)
        /// Can be overridden at startup via command-line argument "--logpath=" </summary>
        public static string LogPath { get; set; }

        /// <summary> Path to load/save config to/from (default: .\config.xml)
        /// Can be overridden at startup via command-line argument "--config=" </summary>
        public static string ConfigFileName { get; set; }


        public const string PlayerDBFileName = "PlayerDB.txt";

        public const string IPBanListFileName = "ipbans.txt";

        public const string GreetingFileName = "greeting.txt";

        public const string AnnouncementsFileName = "announcements.txt";

        public const string RulesFileName = "rules.txt";

        public const string RulesDirectory = "rules";

        public const string HeartbeatDataFileName = "heartbeatdata.txt";

        public const string UpdaterFileName = "UpdateInstaller.exe";

        public const string WorldListFileName = "worlds.xml";

        public const string AutoRankFileName = "autorank.xml";

        public const string BlockDBDirectory = "blockdb";

        public const string PluginDirectory = "plugins";

        /// <summary> Path where block database is stored </summary>
        public static string BlockDBPath {
            get { return Path.Combine( WorkingPath, BlockDBDirectory ); }
        }
        
        /// <summary> Path where server rules are stored </summary>
        public static string RulesPath {
            get { return Path.Combine( WorkingPath, RulesDirectory ); }
        }

        /// <summary> Path where map backups are stored </summary>
        public static string BackupPath {
            get {
                return Path.Combine( MapPath, "backups" );
            }
        }

        public const string DataBackupDirectory = "databackups";
        public const string DataBackupFileNameFormat = "fCraftData_{0:yyyyMMdd'_'HH'-'mm'-'ss}.zip";

        #endregion


        #region Utility Methods

        public static void MoveOrReplaceFile( [NotNull] string source, [NotNull] string destination ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( destination == null ) throw new ArgumentNullException( "destination" );
            if( File.Exists( destination ) ) {
                if( Path.GetPathRoot( Path.GetFullPath( source ) ) == Path.GetPathRoot( Path.GetFullPath( destination ) ) ) {
                    string backupFileName = destination + ".bak";
                    File.Replace( source, destination, backupFileName, true );
                    File.Delete( backupFileName );
                } else {
                    File.Copy( source, destination, true );
                }
            } else {
                File.Move( source, destination );
            }
        }


        /// <summary> If given fileName is contained in a directory, returns full directory name.
        /// If fileName is in the path root (e.g. C:\), returns normalized path root. </summary>
        /// <exception cref="ArgumentNullException"> fileName is null </exception>
        [NotNull]
        public static string GetDirNameOrPathRoot( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            string fullPath = Path.GetFullPath( fileName );
            return Path.GetDirectoryName( fullPath ) ?? Path.GetPathRoot( fullPath );
        }


        /// <summary> Makes sure that the path format is valid, that it exists, that it is accessible and writeable. </summary>
        /// <param name="pathLabel"> Name of the path that's being tested (e.g. "map path"). Used for logging. </param>
        /// <param name="path"> Full or partial path. </param>
        /// <param name="checkForWriteAccess"> If set, tries to write to the given directory. </param>
        /// <returns> Full path of the directory (on success) or null (on failure). </returns>
        public static bool TestDirectory( [NotNull] string pathLabel, [NotNull] string path, bool checkForWriteAccess ) {
            if( pathLabel == null ) throw new ArgumentNullException( "pathLabel" );
            if( path == null ) throw new ArgumentNullException( "path" );
            try {
                if( !Directory.Exists( path ) ) {
                    Directory.CreateDirectory( path );
                }
                DirectoryInfo info = new DirectoryInfo( path );
                if( checkForWriteAccess ) {
                    string randomFileName = Path.Combine( info.FullName, "fCraft_write_test_" + Guid.NewGuid() );
                    using( File.Create( randomFileName ) ) { }
                    File.Delete( randomFileName );
                }
                return true;

            } catch( Exception ex ) {
                if( ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException ) {
                    Logger.Log( LogType.Error,
                                "Paths.TestDirectory: Specified path for {0} is invalid or incorrectly formatted ({1}: {2}).",
                                pathLabel, ex.GetType().Name, ex.Message );
                } else if( ex is SecurityException || ex is UnauthorizedAccessException ) {
                    Logger.Log( LogType.Error,
                                "Paths.TestDirectory: Cannot create or write to file/path for {0}, please check permissions ({1}: {2}).",
                                pathLabel, ex.GetType().Name, ex.Message );
                } else if( ex is DirectoryNotFoundException ) {
                    Logger.Log( LogType.Error,
                                "Paths.TestDirectory: Drive/volume for {0} does not exist or is not mounted ({1}: {2}).",
                                pathLabel, ex.GetType().Name, ex.Message );
                } else if( ex is IOException ) {
                    Logger.Log( LogType.Error,
                                "Paths.TestDirectory: Specified directory for {0} is not readable/writable ({1}: {2}).",
                                pathLabel, ex.GetType().Name, ex.Message );
                } else {
                    throw;
                }
            }
            return false;
        }


        /// <summary> Makes sure that the path format is valid, and optionally whether it is readable/writeable. </summary>
        /// <param name="fileLabel"> Name of the path that's being tested (e.g. "map path"). Used for logging. </param>
        /// <param name="fileName"> Full or partial path of the file. </param>
        /// <param name="createIfDoesNotExist"> If target file is missing and this option is OFF, TestFile returns true.
        /// If target file is missing and this option is ON, TestFile tries to create
        /// a file and returns whether it succeeded. </param>
        /// <param name="neededAccess"> If file is present, type of access to test. </param>
        /// <returns> Whether target file passed all tests. </returns>
        public static bool TestFile( [NotNull] string fileLabel, [NotNull] string fileName,
                                     bool createIfDoesNotExist, FileAccess neededAccess ) {
            if( fileLabel == null ) throw new ArgumentNullException( "fileLabel" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            try {
                new FileInfo( fileName );
                if( File.Exists( fileName ) ) {
                    if( (neededAccess & FileAccess.Read) == FileAccess.Read ) {
                        using( File.OpenRead( fileName ) ) { }
                    }
                    if( (neededAccess & FileAccess.Write) == FileAccess.Write ) {
                        using( File.OpenWrite( fileName ) ) { }
                    }
                } else if( createIfDoesNotExist ) {
                    using( File.Create( fileName ) ) { }
                }
                return true;

            } catch( Exception ex ) {
                if( ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException ) {
                    Logger.Log( LogType.Error,
                                "Paths.TestFile: Specified path for {0} is invalid or incorrectly formatted ({1}: {2}).",
                                fileLabel, ex.GetType().Name, ex.Message );
                } else if( ex is SecurityException || ex is UnauthorizedAccessException ) {
                    Logger.Log( LogType.Error,
                                "Paths.TestFile: Cannot create or write to {0}, please check permissions ({1}: {2}).",
                                fileLabel, ex.GetType().Name, ex.Message );
                } else if( ex is DirectoryNotFoundException ) {
                    Logger.Log( LogType.Error,
                                "Paths.TestFile: Drive/volume for {0} does not exist or is not mounted ({1}: {2}).",
                                fileLabel, ex.GetType().Name, ex.Message );
                } else if( ex is IOException ) {
                    Logger.Log( LogType.Error,
                                "Paths.TestFile: Specified file for {0} is not readable/writable ({1}: {2}).",
                                fileLabel, ex.GetType().Name, ex.Message );
                } else {
                    throw;
                }
            }
            return false;
        }


        public static bool IsDefaultMapPath( [CanBeNull] string path ) {
            return String.IsNullOrEmpty( path ) || Compare( MapPathDefault, path );
        }


        /// <summary>Returns true if paths or fileNames reference the same location (accounts for all the filesystem quirks).</summary>
        public static bool Compare( [NotNull] string p1, [NotNull] string p2 ) {
            if( p1 == null ) throw new ArgumentNullException( "p1" );
            if( p2 == null ) throw new ArgumentNullException( "p2" );
            return Compare( p1, p2, MonoCompat.IsCaseSensitive );
        }


        /// <summary>Returns true if paths or fileNames reference the same location (accounts for all the filesystem quirks).</summary>
        public static bool Compare( [NotNull] string p1, [NotNull] string p2, bool caseSensitive ) {
            if( p1 == null ) throw new ArgumentNullException( "p1" );
            if( p2 == null ) throw new ArgumentNullException( "p2" );
            StringComparison sc = (caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
            return String.Equals( Path.GetFullPath( p1 ).TrimEnd( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar ),
                                  Path.GetFullPath( p2 ).TrimEnd( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar ),
                                  sc );
        }


        public static bool IsValidPath( string path ) {
            try {
                new FileInfo( path );
                return true;
            } catch( ArgumentException ) {
            } catch( PathTooLongException ) {
            } catch( NotSupportedException ) {
            }
            return false;
        }


        /// <summary> Checks whether childPath is inside parentPath </summary>
        /// <param name="parentPath">Path that is supposed to contain childPath</param>
        /// <param name="childPath">Path that is supposed to be contained within parentPath</param>
        /// <returns>true if childPath is contained within parentPath</returns>
        public static bool Contains( [NotNull] string parentPath, [NotNull] string childPath ) {
            if( parentPath == null ) throw new ArgumentNullException( "parentPath" );
            if( childPath == null ) throw new ArgumentNullException( "childPath" );
            return Contains( parentPath, childPath, MonoCompat.IsCaseSensitive );
        }


        /// <summary> Checks whether childPath is inside parentPath </summary>
        /// <param name="parentPath"> Path that is supposed to contain childPath </param>
        /// <param name="childPath"> Path that is supposed to be contained within parentPath </param>
        /// <param name="caseSensitive"> Whether check should be case-sensitive or case-insensitive. </param>
        /// <returns> true if childPath is contained within parentPath </returns>
        public static bool Contains( [NotNull] string parentPath, [NotNull] string childPath, bool caseSensitive ) {
            if( parentPath == null ) throw new ArgumentNullException( "parentPath" );
            if( childPath == null ) throw new ArgumentNullException( "childPath" );
            string fullParentPath = Path.GetFullPath( parentPath ).TrimEnd( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar );
            string fullChildPath = Path.GetFullPath( childPath ).TrimEnd( Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar );
            StringComparison sc = (caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
            return fullChildPath.StartsWith( fullParentPath, sc );
        }


        /// <summary> Checks whether the file exists in a specified way (case-sensitive or case-insensitive) </summary>
        /// <param name="fileName"> fileName in question </param>
        /// <param name="caseSensitive"> Whether check should be case-sensitive or case-insensitive. </param>
        /// <returns> true if file exists, otherwise false </returns>
        public static bool FileExists( [NotNull] string fileName, bool caseSensitive ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            if( caseSensitive == MonoCompat.IsCaseSensitive ) {
                return File.Exists( fileName );
            } else {
                return new FileInfo( fileName ).Exists( caseSensitive );
            }
        }


        /// <summary>Checks whether the file exists in a specified way (case-sensitive or case-insensitive)</summary>
        /// <param name="fileInfo">FileInfo object in question</param>
        /// <param name="caseSensitive">Whether check should be case-sensitive or case-insensitive.</param>
        /// <returns>true if file exists, otherwise false</returns>
        public static bool Exists( [NotNull] this FileInfo fileInfo, bool caseSensitive ) {
            if( fileInfo == null ) throw new ArgumentNullException( "fileInfo" );
            if( caseSensitive == MonoCompat.IsCaseSensitive ) {
                return fileInfo.Exists;
            } else {
                DirectoryInfo parentDir = fileInfo.Directory;
                StringComparison sc = (caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
                return parentDir.EnumerateFiles()
                                .Any( file => file.Name.Equals( fileInfo.Name, sc ) );
            }
        }


        /// <summary> Allows making changes to fileName capitalization on case-insensitive filesystems. </summary>
        /// <param name="originalFullFileName"> Full path to the original fileName </param>
        /// <param name="newFileName"> New filename (do not include the full path) </param>
        public static void ForceRename( [NotNull] string originalFullFileName, [NotNull] string newFileName ) {
            if( originalFullFileName == null ) throw new ArgumentNullException( "originalFullFileName" );
            if( newFileName == null ) throw new ArgumentNullException( "newFileName" );
            FileInfo originalFile = new FileInfo( originalFullFileName );
            if( originalFile.Name == newFileName ) return;
            FileInfo newFile = new FileInfo( Path.Combine( originalFile.DirectoryName, newFileName ) );
            string tempFileName = originalFile.FullName + Guid.NewGuid();
            MoveOrReplaceFile( originalFile.FullName, tempFileName );
            MoveOrReplaceFile( tempFileName, newFile.FullName );
        }


        /// <summary> Find files that match the name in a case-insensitive way. </summary>
        /// <param name="fullFileName"> Case-insensitive fileName to look for. </param>
        /// <returns> Array of matches. Empty array if no files matches. </returns>
        public static FileInfo[] FindFiles( [NotNull] string fullFileName ) {
            if( fullFileName == null ) throw new ArgumentNullException( "fullFileName" );
            FileInfo fi = new FileInfo( fullFileName );
            DirectoryInfo parentDir = fi.Directory;
            return parentDir.EnumerateFiles()
                            .Where( file => file.Name.Equals( fi.Name, StringComparison.OrdinalIgnoreCase ) )
                            .ToArray();
        }


        /// <summary> Checks whether given fileName is a protected fCraft file. </summary>
        /// <exception cref="ArgumentNullException"> If fileName is null. </exception>
        public static bool IsProtectedFileName( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            return ProtectedFiles.Any( t => Compare( t, fileName ) );
        }

        #endregion
    }
}