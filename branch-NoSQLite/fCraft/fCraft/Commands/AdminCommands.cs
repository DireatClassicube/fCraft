﻿using System;
using System.Net;
using System.Collections.Generic;
using System.IO;

namespace fCraft {
    class AdminCommands {

        internal static void Init() {
            string banCommonHelp = "Ban information can be viewed with &H/baninfo";

            cdBan.help += banCommonHelp;
            cdBanIP.help += banCommonHelp;
            cdBanAll.help += banCommonHelp;
            cdUnban.help += banCommonHelp;
            cdUnbanIP.help += banCommonHelp;
            cdUnbanAll.help += banCommonHelp;

            CommandList.RegisterCommand( cdBan );
            CommandList.RegisterCommand( cdBanIP );
            CommandList.RegisterCommand( cdBanAll );
            CommandList.RegisterCommand( cdUnban );
            CommandList.RegisterCommand( cdUnbanIP );
            CommandList.RegisterCommand( cdUnbanAll );

            CommandList.RegisterCommand( cdKick );

            CommandList.RegisterCommand( cdChangeClass );

            CommandList.RegisterCommand( cdImportBans );
            CommandList.RegisterCommand( cdImportRanks );

            CommandList.RegisterCommand( cdHide );
            CommandList.RegisterCommand( cdUnhide );

            CommandList.RegisterCommand( cdSetSpawn );

            CommandList.RegisterCommand( cdReloadConfig );
            CommandList.RegisterCommand( cdShutdown );

            CommandList.RegisterCommand( cdFreeze );
            CommandList.RegisterCommand( cdUnfreeze );

            CommandList.RegisterCommand( cdSay );

            CommandList.RegisterCommand( cdTP );
            CommandList.RegisterCommand( cdBring );
            CommandList.RegisterCommand( cdPatrol );

            CommandList.RegisterCommand( cdMute );
        }


        #region Ban

        static CommandDescriptor cdBan = new CommandDescriptor {
            name = "ban",
            consoleSafe = true,
            permissions = new Permission[] { Permission.Ban },
            usage = "/ban PlayerName [Reason]",
            help = "Bans a specified player by name. Note: Does NOT ban IP. " +
                   "Any text after the player name will be saved as a memo. ",
            handler = Ban
        };

        internal static void Ban( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), false, false, false );
        }



        static CommandDescriptor cdBanIP = new CommandDescriptor {
            name = "banip",
            consoleSafe = true,
            permissions = new Permission[] { Permission.Ban, Permission.BanIP },
            usage = "/banip PlayerName|IPAddress [Reason]",
            help = "Bans the player's name and IP. If player is not online, last known IP associated with the name is used. " +
                   "You can also type in the IP address directly. " +
                   "Any text after PlayerName/IP will be saved as a memo. ",
            handler = BanIP
        };

        internal static void BanIP( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), true, false, false );
        }



        static CommandDescriptor cdBanAll = new CommandDescriptor {
            name = "banall",
            consoleSafe = true,
            permissions = new Permission[] { Permission.Ban, Permission.BanIP, Permission.BanAll },
            usage = "/banall PlayerName|IPAddress [Reason]",
            help = "Bans the player's name, IP, and all other names associated with the IP. " +
                   "If player is not online, last known IP associated with the name is used. " +
                   "You can also type in the IP address directly. " +
                   "Any text after PlayerName/IP will be saved as a memo. ",
            handler = BanAll
        };

        internal static void BanAll( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), true, true, false );
        }



        static CommandDescriptor cdUnban = new CommandDescriptor {
            name = "unban",
            consoleSafe = true,
            permissions = new Permission[] { Permission.Ban },
            usage = "/unban PlayerName [Reason]",
            help = "Removes ban for a specified player. Does NOT remove associated IP bans. " +
                   "Any text after the player name will be saved as a memo. ",
            handler = Unban
        };

        internal static void Unban( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), false, false, true );
        }



        static CommandDescriptor cdUnbanIP = new CommandDescriptor {
            name = "unbanip",
            consoleSafe = true,
            permissions = new Permission[] { Permission.Ban, Permission.BanIP },
            usage = "/unbanip PlayerName|IPaddress [Reason]",
            help = "Removes ban for a specified player's name and last known IP. " +
                   "You can also type in the IP address directly. " +
                   "Any text after the player name will be saved as a memo. ",
            handler = UnbanIP
        };

        internal static void UnbanIP( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), true, false, true );
        }



        static CommandDescriptor cdUnbanAll = new CommandDescriptor {
            name = "unbanall",
            consoleSafe = true,
            permissions = new Permission[] { Permission.Ban, Permission.BanIP, Permission.BanAll },
            usage = "/unbanall PlayerName|IPaddress [Reason]",
            help = "Removes ban for a specified player's name, last known IP, and all other names associated with the IP. " +
                   "You can also type in the IP address directly. " +
                   "Any text after the player name will be saved as a memo. ",
            handler = UnbanAll
        };

        internal static void UnbanAll( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), true, true, true );
        }


        internal static void DoBan( Player player, string nameOrIP, string reason, bool banIP, bool banAll, bool unban ) {
            if( nameOrIP == null ) {
                player.Message( "Please specify player name or IP to ban." );
                return;
            }

            IPAddress address;
            Player offender = Server.FindPlayerExact( nameOrIP );
            PlayerInfo info = PlayerDB.FindPlayerInfoExact( nameOrIP );

            if( Config.GetBool( ConfigKey.RequireBanReason ) && (reason == null || reason.Length == 0) ) {
                player.Message( Color.Red + "Please specify a ban/unban reason." );
                // freeze the target player to prevent further damage
                if( !unban && offender != null && player.Can( Permission.Freeze ) && player.info.playerClass.CanBan( offender.info.playerClass ) ) {
                    player.Message( offender.GetClassyName() + Color.Red + " has been frozen while you retry." );
                    Freeze( player, new Command( "/freeze " + offender.name ) );
                }

                return;
            }

            // ban by IP address
            if( banIP && IPAddress.TryParse( nameOrIP, out address ) ) {
                DoIPBan( player, address, reason, null, banAll, unban );

                // ban online players
            } else if( !unban && offender != null ) {

                // check permissions
                if( player.info.playerClass.CanBan( offender.info.playerClass ) ) {
                    address = offender.info.lastIP;
                    if( banIP ) DoIPBan( player, address, reason, offender.name, banAll, unban );
                    if( !banAll ) {
                        if( offender.info.ProcessBan( player, reason ) ) {
                            Logger.Log( "{0} was banned by {1}.", LogType.UserActivity,
                                        offender.info.name, player.name );
                            Server.SendToAll( offender.GetClassyName() + Color.Red + " was banned by " + player.GetClassyName(), offender );
                            if( reason != null && reason.Length > 0 ) {
                                if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) ) {
                                    Server.SendToAll( Color.Red + "Ban reason: " + reason );
                                }
                                offender.session.Kick( "Banned by " + player.GetClassyName() + Color.White + ": " + reason );
                            } else {
                                offender.session.Kick( "Banned by " + player.GetClassyName() );
                            }
                        } else {
                            player.Message( offender.GetClassyName() + "&S is already banned." );
                        }
                    }
                } else {
                    player.Message( "You can only ban players ranked {0}&S or lower.",
                                    player.info.playerClass.maxBan.GetClassyName() );
                    player.Message( "{0}&S is ranked {1}",
                                    offender.GetClassyName(),
                                    offender.info.playerClass.GetClassyName() );
                }

                // ban or unban offline players
            } else if( info != null ) {
                if( player.info.playerClass.CanBan( info.playerClass ) || unban ) {
                    address = info.lastIP;
                    if( banIP ) DoIPBan( player, address, reason, info.name, banAll, unban );
                    if( !banAll ) {
                        if( unban ) {
                            if( info.ProcessUnban( player.name, reason ) ) {
                                Logger.Log( "{0} (offline) was unbanned by {1}", LogType.UserActivity,
                                            info.name, player.name );
                                Server.SendToAll( info.GetClassyName() + Color.Red + " (offline) was unbanned by " + player.GetClassyName() );
                                if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) && reason != null && reason.Length > 0 ) {
                                    Server.SendToAll( Color.Red + "Unban reason: " + reason );
                                }
                            } else {
                                player.Message( info.name + " (offline) is not currenty banned." );
                            }
                        } else {
                            if( info.ProcessBan( player, reason ) ) {
                                Logger.Log( "{0} (offline) was banned by {1}.", LogType.UserActivity,
                                            info.name, player.name );
                                Server.SendToAll( info.GetClassyName() + Color.Red + " (offline) was banned by " + player.GetClassyName() );
                                if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) && reason != null && reason.Length > 0 ) {
                                    Server.SendToAll( Color.Red + "Ban reason: " + reason );
                                }
                            } else {
                                player.Message( info.GetClassyName() + "&S (offline) is already banned." );
                            }
                        }
                    }
                } else {
                    PlayerClass maxRank = player.info.playerClass.maxBan;
                    if( maxRank == null ) {
                        player.Message( "You can only ban players ranked {0}&S or lower.",
                                        player.info.playerClass.GetClassyName() );
                    } else {
                        player.Message( "You can only ban players ranked {0}&S or lower.",
                                        maxRank.GetClassyName() );
                    }
                    player.Message( "{0} is ranked {1}",
                                    info.name,
                                    info.playerClass.name );
                }

                // ban players who are not in the database yet
            } else if( Player.IsValidName( nameOrIP ) ) {
                if( unban ) {
                    player.Message( nameOrIP + " (unrecognized) is not banned." );
                } else {
                    info = PlayerDB.AddFakeEntry( nameOrIP );
                    info.ProcessBan( player, reason );
                    player.Message( "Player \"" + nameOrIP + "\" (unrecognized) was banned." );
                    Logger.Log( "{0} (unrecognized) was banned by {1}", LogType.UserActivity,
                                info.name,
                                player.GetClassyName() );
                    Server.SendToAll( Color.Red + info.name + " (unrecognized) was banned by " + player.GetClassyName() );

                    if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) && reason != null && reason.Length > 0 ) {
                        Server.SendToAll( Color.Red + "Ban reason: " + reason );
                    }
                }
            } else {
                player.Message( "Please specify valid player name or IP." );
            }
        }


        internal static void DoIPBan( Player player, IPAddress address, string reason, string playerName, bool banAll, bool unban ) {

            if( address == IPAddress.None || address == IPAddress.Any ) {
                player.Message( "Invalid IP: " + address );
                return;
            }

            if( unban ) {
                if( IPBanList.Remove( address ) ) {
                    player.Message( address.ToString() + " has been removed from the IP ban list." );
                    Server.SendToAll( Color.Red + address.ToString() + " was unbanned by " + player.GetClassyName() );
                    if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) && reason != null && reason.Length > 0 ) {
                        Server.SendToAll( Color.Red + "Unban reason: " + reason );
                    }
                } else {
                    player.Message( address.ToString() + " is not currently banned." );
                }
                if( banAll ) {
                    foreach( PlayerInfo otherInfo in PlayerDB.FindPlayersByIP( address ) ) {
                        if( otherInfo.ProcessUnban( player.name, reason + "~UnBanAll" ) ) {
                            Server.SendToAll( Color.Red + otherInfo.name + " was unbanned (UnbanAll) by " + player.GetClassyName() );
                            player.Message( otherInfo.name + " matched the IP and was also unbanned." );
                        }
                    }
                }

            } else {
                if( IPBanList.Add( new IPBanInfo( address, playerName, player.name, reason ) ) ) {
                    player.Message( address.ToString() + " has been added to the IP ban list." );
                    Server.SendToAll( Color.Red + address.ToString() + " was banned by " + player.GetClassyName() );
                    if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) && reason != null && reason.Length > 0 ) {
                        Server.SendToAll( Color.Red + "Ban reason: " + reason );
                    }

                } else {
                    player.Message( address.ToString() + " is already banned." );
                }
                if( banAll ) {
                    foreach( PlayerInfo otherInfo in PlayerDB.FindPlayersByIP( address ) ) {
                        if( banAll && otherInfo.ProcessBan( player, reason + "~BanAll" ) ) {
                            player.Message( otherInfo.name + " matched the IP and was also banned." );
                        }
                        Server.SendToAll( String.Format( "{0}{1} was banned (BanAll) by {2}",
                                                         otherInfo.GetClassyName(),
                                                         Color.Red,
                                                         player.GetClassyName() ) );
                        foreach( Player other in Server.FindPlayers( address ) ) {
                            if( reason != null && reason.Length > 0 ) {
                                other.session.Kick( "IP-banned by " + player.GetClassyName() + Color.White + ": " + reason );
                            } else {
                                other.session.Kick( "IP-banned by " + player.GetClassyName() );
                            }
                        }
                    }
                }
            }
        }

        #endregion


        #region Kick

        static CommandDescriptor cdKick = new CommandDescriptor {
            name = "kick",
            aliases = new string[] { "k" },
            consoleSafe = true,
            permissions = new Permission[] { Permission.Kick },
            usage = "/kick PlayerName [Reason]",
            help = "Kicks the specified player from the server. " +
                   "Optional kick reason/message is shown to the kicked player and logged.",
            handler = Kick
        };

        internal static void Kick( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name != null ) {
                string msg = cmd.NextAll();
                List<Player> targets = Server.FindPlayers( player, name );
                if( targets.Count == 1 ) {
                    DoKick( player, targets[0], msg, false );
                } else if( targets.Count > 1 ) {
                    player.ManyPlayersMessage( targets );
                } else {
                    player.NoPlayerMessage( name );
                }
            } else {
                player.Message( "Usage: " + Color.Help + "/kick PlayerName [Message]" +
                                   Color.Sys + " or " + Color.Help + "/k PlayerName [Message]" );
            }
        }

        internal static bool DoKick( Player player, Player target, string reason, bool silent ) {
            if( !player.info.playerClass.CanKick( target.info.playerClass ) ) {
                player.Message( "You can only kick players ranked {0}&S or lower.",
                                player.info.playerClass.maxKick.GetClassyName() );
                player.Message( target.GetClassyName() + "&S is ranked " + target.info.playerClass.GetClassyName() );
                return false;
            } else {
                if( !silent ) {
                    Server.SendToAll( target.GetClassyName() + Color.Red + " was kicked by " + player.GetClassyName() );
                    target.info.ProcessKick( player );
                }
                if( reason != null && reason.Length > 0 ) {
                    if( !silent && Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) ) {
                        Server.SendToAll( Color.Red + "Kick reason: " + reason );
                    }
                    Logger.Log( "{0} was kicked by {1}. Reason: {2}", LogType.UserActivity,
                                target.name, player.name, reason );
                    target.session.Kick( "Kicked by " + player.GetClassyName() + Color.White + ": " + reason );
                } else {
                    Logger.Log( "{0} was kicked by {1}", LogType.UserActivity,
                                target.name, player.name );
                    target.session.Kick( "You have been kicked by " + player.GetClassyName() );
                }
                return true;
            }
        }

        #endregion


        #region Changing Class (Promotion / Demotion)

        static CommandDescriptor cdChangeClass = new CommandDescriptor {
            name = "user",
            aliases = new string[] { "rank", "promote", "demote" },
            consoleSafe = true,
            usage = "/user PlayerName ClassName [Reason]",
            help = "Changes the class/rank of a player to a specified class. " +
                   "Any text specified after the ClassName will be saved as a memo.",
            handler = ChangeClass
        };

        internal static void ChangeClass( Player player, Command cmd ) {
            string name = cmd.Next();
            string newClassName = cmd.Next();

            // Check arguments
            if( newClassName == null ) {
                cdChangeClass.PrintUsage( player );
                player.Message( "See &H/classes&S for list of player classes." );
                return;
            }

            // Parse class name
            PlayerClass newClass = ClassList.FindClass( newClassName );
            if( newClass == null ) {
                player.Message( "Unrecognized player class: {0}",
                                newClassName );
                return;
            }

            // Parse player name
            PlayerInfo info;
            Player target = Server.FindPlayerExact( name );
            if( target == null ) {
                info = PlayerDB.FindPlayerInfoExact( name );
            } else {
                info = target.info;
            }

            if( info == null ) {
                info = PlayerDB.AddFakeEntry( name );
                player.Message( "Warning: player \"{0}\" is in the database (possible typo)",
                                name );
            }



            DoChangeClass( player, info, target, newClass, cmd.NextAll() );
        }

        internal static void DoChangeClass( Player player, PlayerInfo targetInfo, Player target, PlayerClass newClass, string reason ) {

            bool promote = (targetInfo.playerClass.rank < newClass.rank);

            // Make sure it's not same rank
            if( targetInfo.playerClass == newClass ) {
                player.Message( "{0} is already ranked {1}",
                                targetInfo.name,
                                newClass.GetClassyName() );
                return;
            }

            // Make sure player has the general permissions
            if( (promote && !player.Can( Permission.Promote )) ) {
                player.NoAccessMessage( Permission.Promote );
                return;
            } else if( !promote && !player.Can( Permission.Demote ) ) {
                player.NoAccessMessage( Permission.Demote );
                return;
            }

            // Make sure player has the specific permissions (including limits)
            if( promote && !player.info.playerClass.CanPromote( newClass ) ) {
                player.Message( "You can only promote players up to {0}",
                                player.info.playerClass.maxPromote.GetClassyName() );
                player.Message( "{0} is ranked {1}",
                                targetInfo.name,
                                targetInfo.playerClass.GetClassyName() );
                return;
            } else if( !promote && !player.info.playerClass.CanDemote( targetInfo.playerClass ) ) {
                player.Message( "You can only demote players that are {0}&S or lower",
                                player.info.playerClass.maxDemote.GetClassyName() );
                player.Message( "{0} is ranked {1}",
                                targetInfo.name,
                                targetInfo.playerClass.GetClassyName() );
                return;
            }

            if( Config.GetBool( ConfigKey.RequireClassChangeReason ) && (reason == null || reason.Length == 0) ) {
                if( promote ) {
                    player.Message( Color.Red + "Please specify a promotion reason." );
                } else {
                    player.Message( Color.Red + "Please specify a demotion reason." );
                }
                cdChangeClass.PrintUsage( player );
                return;
            }


            string verb = (promote ? "promoted" : "demoted");

            // Do the class change
            if( (promote && targetInfo.playerClass.rank < newClass.rank) ||
                (!promote && targetInfo.playerClass.rank > newClass.rank) ) {
                PlayerClass oldClass = targetInfo.playerClass;

                if( !Server.FirePlayerClassChange( targetInfo, player, oldClass, newClass ) ) return;

                Logger.Log( "{0} {1} {2} from {3} to {4}.", LogType.UserActivity,
                            player.name, verb, targetInfo.name, targetInfo.playerClass.name, newClass.name );

                // if player is online, toggle visible/invisible players
                if( target != null && target.world != null ) {

                    HashSet<Player> invisiblePlayers = new HashSet<Player>();

                    Player[] worldPlayerList = target.world.playerList;
                    for( int i = 0; i < worldPlayerList.Length; i++ ) {
                        if( !target.CanSee( worldPlayerList[i] ) ) {
                            invisiblePlayers.Add( worldPlayerList[i] );
                        }
                    }


                    // ==== Actual class change happens here ====
                    targetInfo.ProcessClassChange( newClass, player, reason );
                    // ==== Actual class change happens here ====


                    // change admincrete deletion permission
                    target.Send( PacketWriter.MakeSetPermission( target ) );

                    // inform the player of the class change
                    target.Message( "You have been {0} to {1}&S by {2}",
                                    verb,
                                    newClass.GetClassyName(),
                                    player.GetClassyName() );

                    // Handle hiding/revealing hidden players (in case relative permissions change)
                    for( int i = 0; i < worldPlayerList.Length; i++ ) {
                        if( target.CanSee( worldPlayerList[i] ) && invisiblePlayers.Contains( worldPlayerList[i] ) ) {
                            target.Send( PacketWriter.MakeAddEntity( worldPlayerList[i], worldPlayerList[i].pos ) );
                        } else if( !target.CanSee( worldPlayerList[i] ) && !invisiblePlayers.Contains( worldPlayerList[i] ) ) {
                            target.Send( PacketWriter.MakeRemoveEntity( worldPlayerList[i].id ) );
                        }
                    }

                    // remove/readd player to change the name color
                    target.world.SendToAll( PacketWriter.MakeRemoveEntity( target.id ), target );
                    target.world.SendToSeeing( PacketWriter.MakeAddEntity( target, target.pos ), target );

                    // check if player is still patrollable by others
                    target.world.CheckIfPlayerIsStillPatrollable( target );

                } else {
                    // ==== Actual class change happens here (offline) ====
                    targetInfo.ProcessClassChange( newClass, player, reason );
                    // ==== Actual class change happens here (offline) ====
                }

                Server.FirePlayerListChangedEvent();


                if( Config.GetBool( ConfigKey.AnnounceClassChanges ) ) {
                    Server.SendToAll( String.Format( "&S{0} was {1} from {2}&S to {3}",
                                                    targetInfo.name,
                                                    verb,
                                                    oldClass.GetClassyName(),
                                                    newClass.GetClassyName() ) );
                } else {
                    player.Message( "You {0} {1} from {2}&S to {3}",
                                    verb,
                                    targetInfo.name,
                                    oldClass.GetClassyName(),
                                    newClass.GetClassyName() );
                }

            } else {
                if( promote ) {
                    player.Message( "{0}&S is already same or lower rank than {1}",
                                    targetInfo.GetClassyName(),
                                    newClass.GetClassyName() );
                } else {
                    player.Message( "{0}&S is already same or higher rank than {1}",
                                    targetInfo.GetClassyName(),
                                    newClass.GetClassyName() );
                }
            }
        }

        #endregion


        #region Importing

        static CommandDescriptor cdImportBans = new CommandDescriptor {
            name = "importbans",
            permissions = new Permission[] { Permission.Import, Permission.Ban },
            usage = "/importbans SoftwareName File",
            help = "Imports ban list from formats used by other servers. " +
                   "Currently only MCSharp/MCZall files are supported.",
            handler = ImportBans
        };

        static void ImportBans( Player player, Command cmd ) {
            string serverName = cmd.Next();
            string file = cmd.Next();

            // Make sure all parameters are specified
            if( file == null ) {
                cdImportBans.PrintUsage( player );
                return;
            }

            // Check if file exists
            if( !File.Exists( file ) ) {
                player.Message( "File not found: {0}", file );
                return;
            }

            string[] names;

            switch( serverName.ToLower() ) {
                case "mcsharp":
                case "mczall":
                    try {
                        names = File.ReadAllLines( file );
                    } catch( Exception ex ) {
                        Logger.Log( "Could not open \"{0}\" to import bans: {1}", LogType.Error,
                                    file,
                                    ex );
                        return;
                    }
                    break;
                default:
                    player.Message( "fCraft does not support importing from {0}", serverName );
                    return;
            }

            string reason = "(import from " + serverName + ")";
            IPAddress ip;
            foreach( string name in names ) {
                if( Player.IsValidName( name ) ) {
                    DoBan( player, name, reason, false, false, false );
                } else if( IPAddress.TryParse( name, out ip ) ) {
                    DoIPBan( player, ip, reason, "", false, false );
                } else {
                    player.Message( "Could not parse \"{0}\" as either name or IP. Skipping.", name );
                }
            }

            PlayerDB.Save();
            IPBanList.Save();
        }



        static CommandDescriptor cdImportRanks = new CommandDescriptor {
            name = "importranks",
            permissions = new Permission[] { Permission.Import, Permission.Promote, Permission.Demote },
            usage = "/importranks SoftwareName File ClassToAssign",
            help = "Imports player list from formats used by other servers. " +
                   "All players listed in the specified file are added to PlayerDB with the specified rank. " +
                   "Currently only MCSharp/MCZall files are supported.",
            handler = ImportRanks
        };

        static void ImportRanks( Player player, Command cmd ) {
            string serverName = cmd.Next();
            string fileName = cmd.Next();
            string targetName = cmd.Next();


            // Make sure all parameters are specified
            if( targetName == null ) {
                cdImportRanks.PrintUsage( player );
                return;
            }

            // Check if file exists
            if( !File.Exists( fileName ) ) {
                player.Message( "File not found: {0}", fileName );
                return;
            }

            PlayerClass targetClass = ClassList.ParseClass( targetName );
            if( targetClass == null ) {
                player.Message( "Unrecognized player class: \"{0}\"", targetName );
                return;
            }

            string[] names;

            switch( serverName.ToLower() ) {
                case "mcsharp":
                case "mczall":
                    try {
                        names = File.ReadAllLines( fileName );
                    } catch( Exception ex ) {
                        Logger.Log( "Could not open \"{0}\" to import ranks: {1}", LogType.Error,
                                    fileName,
                                    ex );
                        return;
                    }
                    break;
                default:
                    player.Message( "fCraft does not support importing from {0}", serverName );
                    return;
            }

            string reason = "(import from " + serverName + ")";
            foreach( string name in names ) {
                PlayerInfo info = PlayerDB.FindPlayerInfoExact( name );
                if( info == null ) {
                    info = PlayerDB.AddFakeEntry( name );
                }
                DoChangeClass( player, info, null, targetClass, reason );
            }

            PlayerDB.Save();
        }

        #endregion


        #region Hide

        static CommandDescriptor cdHide = new CommandDescriptor {
            name = "hide",
            permissions = new Permission[] { Permission.Hide },
            help = "Enables invisible mode. It looks to other players like you left the server, " +
                   "but you can still do anything - chat, build, delete, type commands - as usual. " +
                   "Great way to spy on griefers and scare newbies. " +
                   "Call &H/unhide&S to reveal yourself.",
            handler = Hide
        };

        internal static void Hide( Player player, Command cmd ) {
            if( !player.isHidden ) {
                player.isHidden = true;

                Server.SendToBlind( PacketWriter.MakeRemoveEntity( player.id ), player );

                string message = String.Format( "{0}&S left the server.", player.GetClassyName() );
                foreach( Packet packet in PacketWriter.MakeWrappedMessage( ">", message, false ) ) {
                    Server.SendToBlind( packet, player );
                }

                message = String.Format( "{0}&S is now hidden.", player.GetClassyName() );
                foreach( Packet packet in PacketWriter.MakeWrappedMessage( ">", message, false ) ) {
                    Server.SendToSeeing( packet, player );
                }

                player.Message( Color.Gray + "You are now hidden." );
            } else {
                player.Message( "You are already hidden." );
            }
        }



        static CommandDescriptor cdUnhide = new CommandDescriptor {
            name = "unhide",
            permissions = new Permission[] { Permission.Hide },
            usage = "/unhide PlayerName",
            help = "Disables the &H/hide&S invisible mode. " +
                   "It looks to other players like you just joined the server.",
            handler = Unhide
        };

        internal static void Unhide( Player player, Command cmd ) {
            if( player.Can( Permission.Hide ) ) {
                if( player.isHidden ) {
                    player.isHidden = false;

                    player.Message( Color.Gray + "You are no longer hidden." );
                    player.world.SendToBlind( PacketWriter.MakeAddEntity( player, player.pos ), player );

                    string message = String.Format( "{0}&S is no longer hidden.", player.GetClassyName() );
                    foreach( Packet packet in PacketWriter.MakeWrappedMessage( ">", message, false ) ) {
                        Server.SendToSeeing( packet, player );
                    }

                    Server.ShowPlayerConnectedMessage( player, false, player.world );
                } else {
                    player.Message( "You are not currently hidden." );
                }
            } else {
                player.NoAccessMessage( Permission.Hide );
            }
        }

        #endregion


        #region Set Spawn

        static CommandDescriptor cdSetSpawn = new CommandDescriptor {
            name = "setspawn",
            permissions = new Permission[] { Permission.SetSpawn },
            help = "Assigns your current location to be the spawn point of the map/world.",
            handler = SetSpawn
        };

        internal static void SetSpawn( Player player, Command cmd ) {
            if( player.Can( Permission.SetSpawn ) ) {
                player.world.map.spawn = player.pos;
                player.world.map.changesSinceSave++;
                player.Send( PacketWriter.MakeSelfTeleport( player.world.map.spawn ), true );
                player.Message( "New spawn point saved." );
                Logger.Log( "{0} changed the spawned point.", LogType.UserActivity,
                            player.name );
            } else {
                player.NoAccessMessage( Permission.SetSpawn );
            }
        }

        #endregion


        #region ReloadConfig / Shutdown

        static CommandDescriptor cdReloadConfig = new CommandDescriptor {
            name = "reloadconfig",
            permissions = new Permission[] { Permission.ReloadConfig },
            consoleSafe = true,
            help = "Reloads most of server's configuration file. " +
                   "NOTE: THIS COMMAND IS EXPERIMENTAL! Excludes class changes and IRC bot settings. " +
                   "Server has to be restarted to change those.",
            handler = ReloadConfig
        };

        static void ReloadConfig( Player player, Command cmd ) {
            player.Message( "Attempting to reload config..." );
            if( Config.Load( true ) ) {
                player.Message( "Config reloaded." );
            } else {
                player.Message( "An error occured while trying to reload the config. See server log for details." );
            }
        }

        static CommandDescriptor cdShutdown = new CommandDescriptor {
            name = "shutdown",
            permissions = new Permission[] { Permission.ShutdownServer },
            consoleSafe = true,
            help = "Shuts down the server immediately.",
            handler = Shutdown
        };

        static void Shutdown( Player player, Command cmd ) {
            string reason = cmd.Next();

            Server.SendToAll( Color.Red + "Server shutting down in 5 seconds." );

            if( reason == null ) {
                Logger.Log( "{0} shut down the server.", LogType.UserActivity, player.name );
                Server.InitiateShutdown( player.GetClassyName() );
            } else {
                Logger.Log( "{0} shut down the server. Reason: {1}", LogType.UserActivity, player.name, reason );
                Server.InitiateShutdown( reason );
            }
        }


        #endregion


        #region Freeze

        static CommandDescriptor cdFreeze = new CommandDescriptor {
            name = "freeze",
            consoleSafe = true,
            permissions = new Permission[] { Permission.Freeze },
            usage = "/freeze PlayerName",
            help = "Freezes the specified player in place. " +
                   "This is usually effective, but not hacking-proof. " +
                   "To release the player, use &H/unfreeze PlayerName",
            handler = Freeze
        };

        internal static void Freeze( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                cdFreeze.PrintUsage( player );
                return;
            }
            List<Player> targets = Server.FindPlayers( player, name );
            if( targets.Count == 1 ) {
                if( !targets[0].isFrozen ) {
                    Server.SendToAll( targets[0].GetClassyName() + "&S has been frozen by " + player.GetClassyName() );
                    targets[0].isFrozen = true;
                } else {
                    player.Message( targets[0].GetClassyName() + "&S is already frozen." );
                }
            } else if( targets.Count > 1 ) {
                player.ManyPlayersMessage( targets );
            } else {
                player.NoPlayerMessage( name );
            }
        }



        static CommandDescriptor cdUnfreeze = new CommandDescriptor {
            name = "unfreeze",
            consoleSafe = true,
            permissions = new Permission[] { Permission.Freeze },
            usage = "/unfreeze PlayerName",
            help = "Releases the player from a frozen state. See &H/freeze&S for more information.",
            handler = Unfreeze
        };

        internal static void Unfreeze( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                cdFreeze.PrintUsage( player );
                return;
            }
            List<Player> targets = Server.FindPlayers( player, name );
            if( targets.Count == 1 ) {
                if( targets[0].isFrozen ) {
                    Server.SendToAll( targets[0].GetClassyName() + "&S is no longer frozen." );
                    targets[0].isFrozen = false;
                } else {
                    player.Message( targets[0].GetClassyName() + "&S is currently not frozen." );
                }
            } else if( targets.Count > 1 ) {
                player.ManyPlayersMessage( targets );
            } else {
                player.NoPlayerMessage( name );
            }
        }

        #endregion


        #region Say

        static CommandDescriptor cdSay = new CommandDescriptor {
            name = "say",
            consoleSafe = true,
            permissions = new Permission[] { Permission.Say },
            usage = "/say Message",
            help = "Shows a message in special color, without the player name prefix. " +
                   "Can be used for making announcements.",
            handler = Say
        };

        internal static void Say( Player player, Command cmd ) {
            if( player.Can( Permission.Say ) ) {
                string msg = cmd.NextAll();
                if( msg != null && msg.Trim().Length > 0 ) {
                    Server.SendToAll( Color.Say + msg.Trim() );
                } else {
                    cdSay.PrintUsage( player );
                }
            } else {
                player.NoAccessMessage( Permission.Say );
            }
        }

        #endregion


        #region Teleport / Bring / Patrol

        static CommandDescriptor cdTP = new CommandDescriptor {
            name = "tp",
            aliases = new string[] { "spawn" },
            usage = "/tp [PlayerName]&S or &H/tp X Y Z",
            help = "Teleports you to a specified player's location. " +
                   "If no name is given, teleports you to map spawn. " +
                   "If coordinates are given, teleports to that location.",
            handler = TP
        };

        internal static void TP( Player player, Command cmd ) {
            string name = cmd.Next();

            if( name == null ) {
                player.Send( PacketWriter.MakeSelfTeleport( player.world.map.spawn ) );
                return;
            }

            if( !player.Can( Permission.Teleport ) ) {
                player.NoAccessMessage( Permission.Teleport );
                return;
            }

            List<Player> matches = Server.FindPlayers( player, name );
            if( matches.Count == 1 ) {
                Player target = matches[0];

                if( target.world == player.world ) {
                    player.Send( PacketWriter.MakeSelfTeleport( target.pos ) );

                } else if( player.CanJoin( target.world ) ) {
                    player.session.JoinWorld( target.world, target.pos );

                } else {
                    player.Message( "Cannot teleport to {0}&S because this world requires {0}+&S to join.",
                                    target.GetClassyName(),
                                    player.world.classAccess.GetClassyName() );
                }

            } else if( matches.Count > 1 ) {
                player.ManyPlayersMessage( matches );

            } else if( cmd.Next() != null ) {
                cmd.Rewind();
                int x, y, h;
                if( cmd.NextInt( out x ) && cmd.NextInt( out y ) && cmd.NextInt( out h ) ) {

                    if( x <= -1024 || x >= 1024 || y <= -1024 || y >= 1024 || h <= -1024 || h >= 1024 ) {
                        player.Message( "Coordinates are outside the valid range!" );

                    } else {
                        player.Send( PacketWriter.MakeTeleport( 255, new Position {
                            x = (short)(x * 32 + 16),
                            y = (short)(y * 32 + 16),
                            h = (short)(h * 32 + 16),
                            r = player.pos.r,
                            l = player.pos.l
                        } ) );
                    }

                } else {
                    cdTP.PrintUsage( player );
                }

            } else {
                World w = Server.FindWorld( name );
                if( w != null ) {
                    player.ParseMessage( "/join " + name, false );
                } else {
                    player.NoPlayerMessage( name );
                }
            }
        }




        static CommandDescriptor cdBring = new CommandDescriptor {
            name = "bring",
            permissions = new Permission[] { Permission.Bring },
            usage = "/bring PlayerName",
            help = "Teleports you to a specified player's location. If no name is given, teleports you to map spawn.",
            handler = Bring
        };

        internal static void Bring( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                cdBring.PrintUsage( player );
                return;
            }

            List<Player> matches = Server.FindPlayers( player, name );
            if( matches.Count == 1 ) {
                Player target = matches[0];

                if( target.world == player.world ) {
                    target.Send( PacketWriter.MakeSelfTeleport(player.pos) );

                } else if( target.CanJoin( player.world ) ) {
                    target.session.JoinWorld( player.world, player.pos );

                } else {
                    player.Message( "Cannot bring {0}&S because this world requires {0}+&S to join.",
                                    target.GetClassyName(),
                                    player.world.classAccess.GetClassyName() );
                }
            } else if( matches.Count > 1 ) {
                player.ManyPlayersMessage( matches );
            } else {
                player.NoPlayerMessage( name );
            }
        }



        static CommandDescriptor cdPatrol = new CommandDescriptor {
            name = "patrol",
            permissions = new Permission[] { Permission.Patrol },
            help = "Teleports you to the next player in need of checking.",
            handler = Patrol
        };

        internal static void Patrol( Player player, Command cmd ) {
            Player target = player.world.GetNextPatrolTarget();
            if( target == null ) {
                player.Message( "Patrol: No one to patrol in this world." );
                return;
            }

            if( target == player ) {
                target = player.world.GetNextPatrolTarget();
            }
            if( target == player ) {
                player.Message( "Patrol: No one to patrol in this world (except yourself)." );
                return;
            }

            player.Message( "Patrol: Teleporting to {0}", target.GetClassyName() );
            player.Send( PacketWriter.MakeSelfTeleport( target.pos ) );
        }

        #endregion


        #region Mute
        static CommandDescriptor cdMute = new CommandDescriptor {
            name = "mute",
            permissions = new Permission[] { Permission.Mute },
            help = "Mutes a player for a specified number of seconds.",
            usage = "/mute PlayerName Seconds",
            handler = Mute
        };

        internal static void Mute( Player player, Command cmd ) {
            string playerName = cmd.Next();
            int seconds;
            if( playerName != null && Player.IsValidName(playerName) && cmd.NextInt( out seconds ) && seconds > 0 ) {
                List<Player> matches = Server.FindPlayers( playerName );
                if( matches.Count == 1 ) {
                    Player target = matches[0];
                    target.Mute( seconds );
                    target.Message( "You were muted by {0} seconds by {1}", seconds, player.GetClassyName() );
                    Server.SendToAll( String.Format( "Player {0}&S was muted by {1}&S for {2} sec",
                                                     target.GetClassyName(), player.GetClassyName(), seconds),
                                      target );
                    Logger.Log( "Player {0} was muted by {1} for {2} seconds.", LogType.UserActivity,
                                target.name, player.name, seconds );

                } else if( matches.Count > 1 ) {
                    player.ManyPlayersMessage( matches );

                } else {
                    player.NoPlayerMessage( playerName );
                }
            } else {
                cdMute.PrintUsage( player );
            }
        }
        #endregion
    }
}