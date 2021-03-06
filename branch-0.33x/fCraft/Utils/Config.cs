﻿// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;


namespace fCraft {
    public class Config {

        public int Salt;
        public string ServerURL;
        public const int HeartBeatDelay = 50000;

        public const int ProtocolVersion = 7;
        public const int ConfigVersion = 100;
        public const uint LevelFormatID = 0xFC000002;
        public const int MaxPlayersSupported = 256;
        const string ConfigRootName = "fCraftConfig";
        World world;
        Dictionary<string, string> settings = new Dictionary<string, string>();
        public ClassList classes;
        public Logger logger;
        internal ReaderWriterLockSlim locker = new ReaderWriterLockSlim();

        public string errors = ""; // for ConfigTool


        public Config( World _world, ClassList _classes, Logger _logger ) {
            world = _world;
            classes = _classes;
            logger = _logger;
        }

        void Log( string format, LogType type, params object[] args ) {
            Log( String.Format( format, args ), type );
        }

        void Log( string message, LogType type ) {
            if( world != null ) {
                world.log.Log( message, type );
            } else if( type != LogType.Debug ) {
                errors += message + Environment.NewLine;
            }
        }


        public void LoadDefaultsGeneral() {
            settings["ServerName"] = "Minecraft custom server (fCraft)";
            settings["MOTD"] = "Welcome to the server!";
            settings["MaxPlayers"] = "16";
            settings["DefaultClass"] = ""; // empty = lowest rank
            settings["IsPublic"] = "false";
            settings["Port"] = "25565";
            settings["UploadBandwidth"] = "100";
            settings["ReservedSlotBehavior"] = "IncreaseMaxPlayers"; // can be "KickIdle", "KickRandom", "IncreaseMaxPlayers"

            settings["ClassColorsInChat"] = "true";
            settings["ClassPrefixesInChat"] = "false";
            settings["ClassPrefixesInList"] = "false";
            settings["SystemMessageColor"] = "yellow";
            settings["HelpColor"] = "magenta";
            settings["SayColor"] = "yellow";
        }


        public void LoadDefaultsSecurity() {
            settings["VerifyNames"] = "Balanced"; // can be "Always," "Balanced," or "Never"
            settings["AnnounceUnverifiedNames"] = "True";

            settings["AntispamMessageCount"] = "4";
            settings["AntispamInterval"] = "5";
            settings["AntispamMuteDuration"] = "5";
            settings["AntispamMaxWarnings"] = "2";

            settings["AntigriefBlockCount"] = "35";
            settings["AntigriefInterval"] = "5";
            settings["AntigriefAction1"] = "Warn";
            settings["AntigriefAction2"] = "Kick";
            settings["AntigriefAction3"] = "BanIP";
        }


        public void LoadDefaultsSavingAndBackup() {
            settings["SaveOnShutdown"] = "true";
            settings["SaveInterval"] = "60"; // 0 = no auto save

            settings["BackupOnStartup"] = "false";
            settings["BackupOnJoin"] = "false";
            settings["BackupOnlyWhenChanged"] = "true";
            settings["BackupInterval"] = "20"; // 0 = no auto backup
            settings["MaxBackups"] = "100"; // 0 = no backup file count limit
            settings["MaxBackupSize"] = "0"; // 0 = no backup file size count limit
        }


        public void LoadDefaultsLogging() {
            settings["LogMode"] = "OneFile"; // can be: "None", "OneFile", "SplitBySession", "SplitByDay"
            settings["MaxLogs"] = "0";
            for( int i = 0; i < logger.consoleOptions.Length; i++ ) {
                logger.consoleOptions[i] = true;
            }
            logger.consoleOptions[(int)LogType.ConsoleInput] = false;
            logger.consoleOptions[(int)LogType.Debug] = false;
            for( int i = 0; i < logger.logFileOptions.Length; i++ ) {
                logger.logFileOptions[i] = true;
            }
        }


        public void LoadDefaultsAdvanced() {
            settings["PolicyColorCodesInChat"] = "ConsoleOnly"; // can be: "Allow", "ConsoleOnly", "Disallow"
            settings["PolicyIllegalCharacters"] = "Disallow"; // can be: "Allow", "ConsoleOnly", "Disallow"
            settings["SendRedundantBlockUpdates"] = "false";
            settings["PingInterval"] = "0"; // 0 = ping disabled
            settings["AutomaticUpdates"] = "Prompt"; // can be "Disabled", "Notify", "Prompt", and "Auto"
            settings["NoPartialPositionUpdates"] = "false";
            settings["ProcessPriority"] = "";
            settings["RunOnStartup"] = "Never"; // can be "Always", "OnUnexpectedShutdown", or "Never"
            settings["BlockUpdateThrottling"] = "2500";
            settings["TickInterval"] = "100";
            settings["LowLatencyMode"] = "false";
        }


        public void LoadDefaults() {
            //locker.EnterWriteLock();
            settings.Clear();
            LoadDefaultsGeneral();
            LoadDefaultsSecurity();
            LoadDefaultsSavingAndBackup();
            LoadDefaultsLogging();
            LoadDefaultsAdvanced();
            //locker.ExitWriteLock();
        }


        public bool Load( string configFileName ) {
            // generate random salt
            Salt = new Random().Next();

            LoadDefaults();
            bool fromFile = false;

            // try to load config file (XML)
            XDocument file;
            if( File.Exists( configFileName ) ) {
                try {
                    file = XDocument.Load( configFileName );
                    if( file.Root == null || file.Root.Name != ConfigRootName ) {
                        Log( "Config.Load: Malformed or incompatible config file {0}. Loading defaults.", LogType.Warning, configFileName );
                        file = new XDocument();
                        file.Add( new XElement( ConfigRootName ) );
                    } else {
                        Log( "Config.Load: Config file {0} loaded succesfully.", LogType.Debug, configFileName );
                        fromFile = true;
                    }
                } catch( Exception ex ) {
                    Log( "Config.Load: Fatal error while loading config file {0}: {1}", LogType.FatalError,
                                        configFileName, ex.Message );
                    return false;
                }
            } else {
                // create a new one (with defaults) if no file exists
                file = new XDocument();
                file.Add( new XElement( ConfigRootName ) );
            }

            XElement config = file.Root;

            XAttribute attr = config.Attribute( "version" );
            int version;
            if( fromFile && (attr == null || !Int32.TryParse( attr.Value, out version ) || version < ConfigVersion) ) {
                Log( "Config.Load: Your config.xml was made for an older version of fCraft. " +
                    "Some obsolete settings might be ignored, and some recently-added settings will be set to their default values. " +
                    "It is recommended that you run ConfigTool to make sure everything is in order.", LogType.Warning );
            }


            XElement classList = config.Element( "Classes" );
            if( classList != null ) {
                foreach( XElement playerClass in classList.Elements( "PlayerClass" ) ) {
                    if( !DefineClass( playerClass ) ) {
                        Log( "Config.Load: Could not parse one of the class definitions.", LogType.Warning );
                    }
                }
                if( classes.classes.Count == 0 ) {
                    Log( "Config.Load: No classes were defined, or none were defined correctly. Using default player classes.", LogType.Warning );
                    config.Add( DefineDefaultClasses() );
                }
            } else {
                if( fromFile ) Log( "Config.Load: using default player classes.", LogType.Warning );
                config.Add( DefineDefaultClasses() );
            }

            // parse rank-limit permissions
            foreach( PlayerClass pc in classes.classesByIndex ) {
                if( !classes.ParseClassLimits( pc ) ) {
                    Log( "Could not parse one of the rank-limits for kick, ban, promote, and/or demote permissions for {0}. "+
                         "Any unrecognized limits were reset to default (own class).", LogType.Warning, pc.name );
                }
            }

            XElement consoleOptions = config.Element( "ConsoleOptions" );
            if( consoleOptions != null ) {
                ParseLogOptions( consoleOptions, ref logger.consoleOptions );
            } else {
                if( fromFile ) Log( "Config.Load: using default console options.", LogType.Warning );
                for( int i = 0; i < logger.consoleOptions.Length; i++ ) {
                    logger.consoleOptions[i] = true;
                }
                logger.consoleOptions[(int)LogType.ConsoleInput] = false;
                logger.consoleOptions[(int)LogType.Debug] = false;
            }

            XElement logFileOptions = config.Element( "LogFileOptions" );
            if( logFileOptions != null ) {
                ParseLogOptions( logFileOptions, ref logger.logFileOptions );
            } else {
                if( fromFile ) Log( "Config.Load: using default log file options.", LogType.Warning );
                for( int i = 0; i < logger.logFileOptions.Length; i++ ) {
                    logger.logFileOptions[i] = true;
                }
            }

            // Load config
            foreach( XElement element in config.Elements() ) {
                if( settings.ContainsKey( element.Name.ToString() ) ) {
                    // known key
                    SetValue( element.Name.ToString(), element.Value );
                } else if( element.Name.ToString() != "ConsoleOptions" &&
                    element.Name.ToString() != "LogFileOptions" &&
                    element.Name.ToString() != "Classes" ) {
                    // unknown key
                    Log( "Unrecognized entry ignored: {0} = {1}", LogType.Debug, element.Name, element.Value );
                    //TODO: custom settings store
                    //settings.Add( element.Name.ToString(), element.Value );
                }
            }
            return true;
        }


        public bool Save( string configFileName ) {
            XDocument file = new XDocument();

            XElement config = new XElement( ConfigRootName );
            config.Add( new XAttribute( "version", ConfigVersion ) );

            foreach( KeyValuePair<string, string> pair in settings ) {
                config.Add( new XElement( pair.Key, pair.Value ) );
            }

            XElement consoleOptions = new XElement( "ConsoleOptions" );
            for( int i = 0; i < logger.consoleOptions.Length; i++ ) {
                if( logger.consoleOptions[i] ) {
                    consoleOptions.Add( new XElement( ((LogType)i).ToString() ) );
                }
            }
            config.Add( consoleOptions );

            XElement logFileOptions = new XElement( "LogFileOptions" );
            for( int i = 0; i < logger.logFileOptions.Length; i++ ) {
                if( logger.logFileOptions[i] ) {
                    logFileOptions.Add( new XElement( ((LogType)i).ToString() ) );
                }
            }
            config.Add( logFileOptions );

            XElement classesTag = new XElement( "Classes" );
            foreach( PlayerClass playerClass in classes.classes.Values ) {
                XElement classTag = new XElement( "PlayerClass" );
                classTag.Add( new XAttribute( "name", playerClass.name ) );
                classTag.Add( new XAttribute( "rank", playerClass.rank ) );
                classTag.Add( new XAttribute( "color", Color.GetName( playerClass.color ) ) );
                if( playerClass.prefix.Length > 0 ) classTag.Add( new XAttribute( "prefix", playerClass.prefix ) );
                if( playerClass.spamKickThreshold > 0 ) classTag.Add( new XAttribute( "spamKickAt", playerClass.spamKickThreshold ) );
                if( playerClass.spamBanThreshold > 0 ) classTag.Add( new XAttribute( "spamBanAt", playerClass.spamBanThreshold ) );
                if( playerClass.idleKickTimer > 0 ) classTag.Add( new XAttribute( "idleKickAfter", playerClass.idleKickTimer ) );
                if( playerClass.reservedSlot ) classTag.Add( new XAttribute( "reserveSlot", playerClass.reservedSlot ) );
                XElement temp;
                for( int i = 0; i < Enum.GetValues(typeof(Permissions)).Length; i++ ) {
                    if( playerClass.permissions[i] ) {
                        temp = new XElement( ((Permissions)i).ToString() );
                        if( i == (int)Permissions.Ban && playerClass.maxBan!=null ) {
                            temp.Add( new XAttribute( "max", playerClass.maxBan.name ) );
                        } else if( i == (int)Permissions.Kick && playerClass.maxKick != null ) {
                            temp.Add( new XAttribute( "max", playerClass.maxKick.name ) );
                        } else if( i == (int)Permissions.Promote && playerClass.maxPromote != null ) {
                            temp.Add( new XAttribute( "max", playerClass.maxPromote.name ) );
                        } else if( i == (int)Permissions.Demote && playerClass.maxDemote != null ) {
                            temp.Add( new XAttribute( "max", playerClass.maxDemote.name ) );
                        }
                        classTag.Add( temp );
                    }
                }
                classesTag.Add( classTag );
            }
            config.Add( classesTag );


            file.Add( config );
            // save the settings
            try {
                file.Save( configFileName );
                return true;
            } catch( Exception ex ) {
                Log( "Config.Load: Fatal error while saving config file {0}: {1}", LogType.FatalError, configFileName, ex.Message );
                return false;
            }
        }


        void ParseLogOptions( XElement el, ref bool[] list ) {
            for( int i = 0; i < 13; i++ ) {
                if( el.Element( ((LogType)i).ToString() ) != null ) {
                    list[i] = true;
                } else {
                    list[i] = false;
                }
            }
        }


        internal void ApplyConfig() {
            // TODO: logging settings
            //Logger.Threshold = (LogLevel)Enum.Parse( typeof( LogLevel ), settings["LogThreshold"] );

            // chat colors
            Color.Sys = Color.Parse( settings["SystemMessageColor"] );
            Color.Say = Color.Parse( settings["SayColor"] );
            Color.Help = Color.Parse( settings["HelpColor"] );

            // default class
            if( classes.ParseClass( settings["DefaultClass"] ) != null ) {
                classes.defaultClass = classes.ParseClass( settings["DefaultClass"] );
            } else {
                classes.defaultClass = classes.lowestClass;
                Log( "Config.ParseConfig: No default player class defined; assuming that the lowest rank ({0}) is the default.",
                            LogType.Warning, classes.defaultClass.name );
            }

            Player.spamChatCount = GetInt( "AntispamMessageCount" );
            Player.spamChatTimer = GetInt( "AntispamInterval" );
            Player.spamBlockCount = GetInt( "AntigriefBlockCount" );
            Player.spamBlockTimer = GetInt( "AntigriefInterval" );
            Player.muteDuration = TimeSpan.FromSeconds( GetInt( "AntispamMuteDuration" ) );

            Server.maxUploadSpeed = GetInt("UploadBandwidth");
            Server.maxSessionPacketsPerTick = GetInt("BlockUpdateThrottling" );
            world.ticksPerSecond = 1000 / GetInt( "TickInterval" );
        }


        public bool SetValue( string key, string value ) {
            switch( key ) {
                case "ServerName":
                    return ValidateString( key, value, 1, 64 );
                case "MOTD":
                    return ValidateString( key, value, 0, 64 );
                case "MaxPlayers":
                    return ValidateInt( key, value, 1, MaxPlayersSupported );
                case "DefaultClass":
                    if( value != "" ) {
                        if( classes.ParseClass( value ) != null ) {
                            settings[key] = classes.ParseClass( value ).name;
                            return true;
                        } else {
                            Log( "DefaultClass could not be parsed. It should be either blank (indicating \"use lowest class\") or a valid class name", LogType.Warning );
                            return false;
                        }
                    } else {
                        settings[key] = "";
                        return true;
                    }
                case "Port":
                    return ValidateInt( key, value, 1, 65535 );
                case "UploadBandwidth":
                    return ValidateInt( key, value, 1, 10000 );
                case "ReservedSlotBehavior":
                    return ValidateEnum( key, value, "KickIdle", "KickRandom", "IncreaseMaxPlayers" );

                case "IsPublic":
                case "ClassColorsInChat":// TODO: colors in player names
                case "ClassPrefixesInChat":
                case "ClassPrefixesInList":
                case "SaveOnShutdown":
                case "BackupOnStartup":
                case "BackupOnJoin":
                case "BackupOnlyWhenChanged":
                case "SendRedundantBlockUpdates":
                case "NoPartialPositionUpdates":
                    return ValidateBool( key, value );

                case "SystemMessageColor":
                case "HelpColor":
                case "SayColor":
                    return ValidateColor( key, value );


                case "VerifyNames":
                    return ValidateEnum( key, value, "Always", "Balanced", "Never" );
                case "SpamChatCount":
                    return ValidateInt( key, value, 2, 50 );
                case "SpamChatTimer":
                    return ValidateInt( key, value, 1, 50 );
                case "SpamBlockCount":
                    return ValidateInt( key, value, 2, 500 );
                case "SpamBlockTimer":
                    return ValidateInt( key, value, 1, 50 );
                case "SpamChatAction1":
                case "SpamChatAction2":
                case "SpamChatAction3":
                    return ValidateEnum( key, value, "Warn", "Mute", "Kick", "Demote", "Ban", "BanIP", "BanAll" );
                case "SpamBlockAction1":
                case "SpamBlockAction2":
                case "SpamBlockAction3":
                    return ValidateEnum( key, value, "Warn", "Kick", "Demote", "Ban", "BanIP", "BanAll" );


                case "SaveInterval":
                    return ValidateInt( key, value, 0, 1000000 );
                case "BackupInterval":
                    return ValidateInt( key, value, 0, 100000 );
                case "MaxBackups":
                    return ValidateInt( key, value, 0, 100000 );
                case "MaxBackupSize":
                    return ValidateInt( key, value, 0, 1000000 );

                case "LogMode":
                    return ValidateEnum( key, value, "None", "OneFile", "SplitBySession", "SplitByDay" );
                case "MaxLogs":
                    return ValidateInt( key, value, 0, 100000 );

                case "PolicyColorCodesInChat":
                case "PolicyIllegalCharacters":
                    return ValidateEnum( key, value, "Allow", "ConsoleOnly", "Disallow" );
                case "ProcessPriority":
                    return ValidateEnum( key, value, "", "High", "AboveNormal", "Normal", "BelowNormal", "Low" );
                case "RunOnStartup":
                    return ValidateEnum( key, value, "Always", "OnUnexpectedShutdown", "Never" );
                case "AutomaticUpdates":
                    return ValidateEnum( key, value, "Disabled", "Notify", "Prompt", "Auto" );
                case "BlockUpdateThrottling":
                    return ValidateInt( key, value, 1, 100000 );
                default:
                    settings[key] = value;
                    return true;
            }
        }


        bool ValidateInt( string key, string value, int minRange, int maxRange ) {
            int temp;
            if( Int32.TryParse( value, out temp ) ) {
                if( temp >= minRange && temp <= maxRange ) {
                    settings[key] = temp.ToString();
                } else {
                    Log( "Config.SetValue: Specified value for {0} is not within valid range ({1}...{2}). Using default ({3}).", LogType.Warning,
                                        key, minRange, maxRange, settings[key] );
                }
                return true;
            } else {
                Log( "Config.SetValue: Specified value for {0} could not be parsed. Using default ({1}).", LogType.Warning,
                                    key, settings[key] );
                return false;
            }
        }

        bool ValidateBool( string key, string value ) {
            bool temp;
            if( Boolean.TryParse( value, out temp ) ) {
                settings[key] = temp.ToString();
                return true;
            } else {
                Log( "Config.SetValue: Specified value for {0} could not be parsed. Expected 'true' or 'false'. Using default ({1}).", LogType.Warning,
                                    key, settings[key] );
                return false;
            }
        }

        bool ValidateColor( string key, string value ) {
            if( Color.Parse( value ) != null ) {
                settings[key] = value;
                return true;
            } else {
                Log( "Config.SetValue: Specified value for {0} could not be parsed. Using default ({1}).", LogType.Warning,
                                    key, settings[key] );
                return false;
            }
        }

        bool ValidateString( string key, string value, int minLength, int maxLength ) {
            if( key.Length < minLength ) {
                Log( "Config.SetValue: Specified value for {0} is too short (expected length: {1}...{2}). Using default ({3}).", LogType.Warning,
                    key, minLength, maxLength, settings[key] );
                return false;
            } else if( key.Length > maxLength ) {
                settings[key] = value.Substring( 0, maxLength );
                Log( "Config.SetValue: Specified value for {0} is too long (expected length: {1}...{2}). The value has been truncated to \"{3}\".", LogType.Warning,
                    key, minLength, maxLength, settings[key] );
                return true;
            } else {
                settings[key] = value;
                return true;
            }
        }

        bool ValidateEnum( string key, string value, params string[] options ) {
            for( int i = 0; i < options.Length; i++ ) {
                if( value.ToLowerInvariant() == options[i].ToLowerInvariant() ) {
                    settings[key] = options[i];
                    return true;
                }
            }
            Log( "Config.SetValue: Invalid option specified for {0}. " +
                    "See documentation for the list of permitted options. Using default: {1}", LogType.Warning,
                    key, settings[key] );
            return false;
        }


        public string GetString( string key ) {
            return settings[key];
        }

        public int GetInt( string key ) {
            return Int32.Parse( settings[key] );
        }

        public bool GetBool( string key ) {
            return Boolean.Parse( settings[key] );
        }

        public void ResetClasses() {
            classes = new ClassList( world );
            XElement classList = DefineDefaultClasses();
            foreach( XElement pc in classList.Elements() ) {
                DefineClass( pc );
            }
            // parse rank-limit permissions
            foreach( PlayerClass pc in classes.classesByIndex ) {
                classes.ParseClassLimits( pc );
            }
        }


        XElement DefineDefaultClasses() {
            XElement temp;
            XElement permissions = new XElement( "Classes" );

            XElement guest = new XElement( "PlayerClass" );
            guest.Add( new XAttribute( "name", "guest" ) );
            guest.Add( new XAttribute( "rank", 0 ) );
            guest.Add( new XAttribute( "color", "silver" ) );
            guest.Add( new XAttribute( "prefix", "" ) );
            guest.Add( new XAttribute( "spamKickAt", 0 ) );
            guest.Add( new XAttribute( "spamBanAt", 0 ) );
            guest.Add( new XAttribute( "idleKickAfter", 20 ) );
            guest.Add( new XElement( "Chat" ) );
            guest.Add( new XElement( "Build" ) );
            guest.Add( new XElement( "Delete" ) );
            permissions.Add( guest );
            DefineClass( guest );

            XElement regular = new XElement( "PlayerClass" );
            regular.Add( new XAttribute( "name", "regular" ) );
            regular.Add( new XAttribute( "rank", 30 ) );
            regular.Add( new XAttribute( "color", "white" ) );
            regular.Add( new XAttribute( "prefix", "" ) );
            regular.Add( new XAttribute( "spamKickAt", 0 ) );
            regular.Add( new XAttribute( "spamBanAt", 0) );
            regular.Add( new XAttribute( "idleKickAfter", 20 ) );
            regular.Add( new XElement( "Chat" ) );
            regular.Add( new XElement( "Build" ) );
            regular.Add( new XElement( "Delete" ) );
            temp = new XElement( "Kick" );
            temp.Add( new XAttribute( "max", "regular" ) );
            regular.Add( temp );
            regular.Add( new XElement( "Teleport" ) );
            regular.Add( new XElement( "ViewOthersInfo" ) );
            regular.Add( new XElement( "PlaceAdmincrete" ) );
            regular.Add( new XElement( "DeleteAdmincrete" ) );
            regular.Add( new XElement( "PlaceGrass" ) );
            regular.Add( new XElement( "PlaceGlitchedSand" ) );
            permissions.Add( regular );
            DefineClass( regular );

            XElement op = new XElement( "PlayerClass" );
            op.Add( new XAttribute( "name", "op" ) );
            op.Add( new XAttribute( "rank", 80 ) );
            op.Add( new XAttribute( "color", "aqua" ) );
            op.Add( new XAttribute( "prefix", "-" ) );
            op.Add( new XAttribute( "spamKickAt", 0 ) );
            op.Add( new XAttribute( "spamBanAt", 0 ) );
            op.Add( new XAttribute( "idleKickAfter", 0 ) );
            op.Add( new XElement( "Chat" ) );
            op.Add( new XElement( "Build" ) );
            op.Add( new XElement( "Delete" ) );
            op.Add( new XElement( "Say" ) );
            temp = new XElement( "Kick" );
            temp.Add( new XAttribute( "max", "op" ) );
            op.Add( temp );
            temp = new XElement( "Ban" );
            temp.Add( new XAttribute( "max", "regular" ) );
            op.Add( temp );
            op.Add( new XElement( "BanIP" ) );
            temp = new XElement( "Promote" );
            temp.Add( new XAttribute( "max", "regular" ) );
            op.Add( temp );
            temp = new XElement( "Demote" );
            temp.Add( new XAttribute( "max", "regular" ) );
            op.Add( temp );
            op.Add( new XElement( "Hide" ) );
            op.Add( new XElement( "ChangeName" ) );
            op.Add( new XElement( "Teleport" ) );
            op.Add( new XElement( "Bring" ) );
            op.Add( new XElement( "Freeze" ) );
            op.Add( new XElement( "SetSpawn" ) );
            op.Add( new XElement( "ViewOthersInfo" ) );
            op.Add( new XElement( "PlaceAdmincrete" ) );
            op.Add( new XElement( "DeleteAdmincrete" ) );
            op.Add( new XElement( "PlaceGrass" ) );
            permissions.Add( op );
            DefineClass( op );

            XElement owner = new XElement( "PlayerClass" );
            owner.Add( new XAttribute( "name", "owner" ) );
            owner.Add( new XAttribute( "rank", 100 ) );
            owner.Add( new XAttribute( "color", "red" ) );
            owner.Add( new XAttribute( "prefix", "+" ) );
            owner.Add( new XAttribute( "spamKickAt", 0 ) );
            owner.Add( new XAttribute( "spamBanAt", 0 ) );
            owner.Add( new XAttribute( "idleKickAfter", 0 ) );
            owner.Add( new XElement( "Chat" ) );
            owner.Add( new XElement( "Build" ) );
            owner.Add( new XElement( "Delete" ) );
            owner.Add( new XElement( "Say" ) );
            temp = new XElement( "Kick" );
            temp.Add( new XAttribute( "max", "owner" ) );
            owner.Add( temp );
            temp = new XElement( "Ban" );
            temp.Add( new XAttribute( "max", "owner" ) );
            owner.Add( temp );
            owner.Add( new XElement( "BanIP" ) );
            owner.Add( new XElement( "BanAll" ) );
            owner.Add( new XElement( "BanOfflinePlayers" ) );
            temp = new XElement( "Promote" );
            temp.Add( new XAttribute( "max", "owner" ) );
            owner.Add( temp );
            temp = new XElement( "Demote" );
            temp.Add( new XAttribute( "max", "owner" ) );
            owner.Add( temp );
            owner.Add( new XElement( "Hide" ) );
            owner.Add( new XElement( "ChangeName" ) );
            owner.Add( new XElement( "Teleport" ) );
            owner.Add( new XElement( "Bring" ) );
            owner.Add( new XElement( "Freeze" ) );
            owner.Add( new XElement( "SetSpawn" ) );
            owner.Add( new XElement( "ViewOthersInfo" ) );
            owner.Add( new XElement( "PlaceAdmincrete" ) );
            owner.Add( new XElement( "DeleteAdmincrete" ) );
            owner.Add( new XElement( "PlaceGrass" ) );
            owner.Add( new XElement( "PlaceWater" ) );
            owner.Add( new XElement( "PlaceLava" ) );
            owner.Add( new XElement( "SaveAndLoad" ) );
            owner.Add( new XElement( "Lock" ) );
            owner.Add( new XElement( "ControlPhysics" ) );
            owner.Add( new XElement( "Draw" ) );
            permissions.Add( owner );
            DefineClass( owner );

            return permissions;
        }


        bool DefineClass( XElement el ) {
            PlayerClass playerClass = new PlayerClass();

            // read required attributes
            XAttribute attr = el.Attribute( "name" );
            if( attr == null ) {
                Log( "Config.DefineClass: Class definition with no name was ignored.", LogType.Warning );
                return false;
            }
            if( !PlayerClass.IsValidClassName( attr.Value.Trim() ) ) {
                Log( "Config.DefineClass: Invalid name specified for class \"{0}\". Class name can only contain letters, digits, and underscores.",
                     LogType.Warning, playerClass.name );
                return false;
            }
            playerClass.name = attr.Value.Trim();

            if( classes.classes.ContainsKey( playerClass.name ) ) {
                Log( "Config.DefineClass: Duplicate class definition for \"{0}\" was ignored.", LogType.Warning, playerClass.name );
                return true;
            }

            
            if( (attr = el.Attribute( "rank" )) == null ) {
                Log( "Config.DefineClass: No rank specified for {0}. Class definition was ignored.", LogType.Warning, playerClass.name );
                return false;
            }
            if( !Byte.TryParse( attr.Value, out playerClass.rank ) ) {
                Log( "Config.DefineClass: Cannot parse rank for {0}. Class definition was ignored.", LogType.Warning, playerClass.name );
                return false;
            }

            attr = el.Attribute( "color" );
            if( attr == null || Color.Parse( attr.Value ) == null ) {
                playerClass.color = "";
            } else {
                playerClass.color = Color.Parse( attr.Value );
            }

            // read optional attributes
            if( (attr = el.Attribute( "prefix" )) != null ) {
                if( PlayerClass.IsValidPrefix( attr.Value ) ) {
                    playerClass.prefix = attr.Value;
                } else {
                    Log( "Config.DefineClass: Invalid prefix specified for {0}.", LogType.Warning, playerClass.name );
                    playerClass.prefix = "";
                }
            }

            if( (attr = el.Attribute( "spamKickAt" )) != null ) {
                if( !Int32.TryParse( attr.Value, out playerClass.spamKickThreshold ) ) {
                    Log( "Config.DefineClass: Could not parse the value for spamKickAt for {0}. Assuming 0 (never).", LogType.Warning, playerClass.name );
                    playerClass.spamKickThreshold = 0;
                }
            } else {
                playerClass.spamKickThreshold = 0;
            }

            if( (attr = el.Attribute( "spamBanAt" )) != null ) {
                if( !Int32.TryParse( attr.Value, out playerClass.spamBanThreshold ) ) {
                    Log( "Config.DefineClass: Could not parse the value for spamBanAt for {0}. Assuming 0 (never).", LogType.Warning, playerClass.name );
                    playerClass.spamBanThreshold = 0;
                }
            } else {
                playerClass.spamBanThreshold = 0;
            }

            if( (attr = el.Attribute( "idleKickAfter" )) != null ) {
                if( !Int32.TryParse( attr.Value, out playerClass.idleKickTimer ) ) {
                    Log( "Config.DefineClass: Could not parse the value for idleKickAfter for {0}. Assuming 0 (never).", LogType.Warning, playerClass.name );
                    playerClass.idleKickTimer = 0;
                }
            } else {
                playerClass.idleKickTimer = 0;
            }

            if( (attr = el.Attribute( "reserveSlot" )) != null ) {
                if( !Boolean.TryParse( attr.Value, out playerClass.reservedSlot ) ) {
                    Log( "Config.DefineClass: Could not parse the value for reserveSlot for {0}. Assuming \"false\".", LogType.Warning, playerClass.name );
                    playerClass.reservedSlot = false;
                }
            } else {
                playerClass.reservedSlot = false;
            }

            // read permissions
            XElement temp;
            for( int i = 0; i < Enum.GetValues(typeof(Permissions)).Length; i++ ) {
                string permission = ((Permissions)i).ToString();
                if( (temp=el.Element( permission )) != null ) {
                    playerClass.permissions[i] = true;
                    if( i == (int)Permissions.Promote ) {
                        if( (attr = temp.Attribute( "max" )) != null ) {
                            playerClass.maxPromoteVal = attr.Value;
                        } else {
                            playerClass.maxPromoteVal = "";
                        }
                    } else if( i == (int)Permissions.Demote ) {
                        if( (attr = temp.Attribute( "max" )) != null ) {
                            playerClass.maxDemoteVal = attr.Value;
                        } else {
                            playerClass.maxDemoteVal = "";
                        }
                    } else if( i == (int)Permissions.Kick ) {
                        if( (attr = temp.Attribute( "max" )) != null ) {
                            playerClass.maxKickVal = attr.Value;
                        } else {
                            playerClass.maxKickVal = "";
                        }
                    } else if( i == (int)Permissions.Ban ) {
                        if( (attr = temp.Attribute( "max" )) != null ) {
                            playerClass.maxBanVal = attr.Value;
                        } else {
                            playerClass.maxBanVal = "";
                        }
                    }
                }
            }

            // check for consistency in ban permissions
            if( !playerClass.Can( Permissions.Ban ) &&
                (playerClass.Can( Permissions.BanAll ) || playerClass.Can( Permissions.BanIP )) ) {
                Log( "Class \"{0}\" is allowed to BanIP and/or BanAll but not allowed to Ban.\n" +
                    "Assuming that all ban permissions were ment to be off.", LogType.Warning, playerClass.name );
                playerClass.permissions[(int)Permissions.BanIP] = false;
                playerClass.permissions[(int)Permissions.BanAll] = false;
            }

            classes.AddClass( playerClass );
            return true;
        }


        public System.Diagnostics.ProcessPriorityClass GetBasePriority() {
            switch( GetString( "ProcessPriority" ) ) {
                case "High": return System.Diagnostics.ProcessPriorityClass.High;
                case "AboveNormal": return System.Diagnostics.ProcessPriorityClass.AboveNormal;
                case "BelowNormal": return System.Diagnostics.ProcessPriorityClass.BelowNormal;
                case "Low": return System.Diagnostics.ProcessPriorityClass.Idle;
                default: return System.Diagnostics.ProcessPriorityClass.Normal;
            }
        }
    }
}