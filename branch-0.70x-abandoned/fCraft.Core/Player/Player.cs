﻿// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Threading;
using fCraft.Drawing;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Callback for a player-made selection of one or more blocks on a map.
    /// A command may request a number of marks/blocks to select, and a specify callback
    /// to be executed when the desired number of marks/blocks is reached. </summary>
    /// <param name="player"> Player who made the selection. </param>
    /// <param name="marks"> An array of 3D marks/blocks, in terms of block coordinates. </param>
    /// <param name="tag"> An optional argument to pass to the callback,
    /// the value of player.selectionArgs </param>
    public delegate void SelectionCallback( Player player, Vector3I[] marks, object tag );


    /// <summary> Represents the method that responds to a confirmation command. </summary>
    /// <param name="player"> Player who confirmed the action. </param>
    /// <param name="tag"> Parameter that was passed to Player.Confirm() </param>
    public delegate void ConfirmationCallback( Player player, object tag );


    /// <summary> Object representing volatile state ("session") of a connected player.
    /// For persistent state of a known player account, see PlayerInfo. </summary>
    public sealed partial class Player : IClassy {

        /// <summary> The godly pseudo-player for commands called from the server console.
        /// Console has all the permissions granted. Note that Player.Console.World is always null,
        /// and that prevents console from calling certain commands (like /TP). </summary>
        public static Player Console;


        #region Properties

        /// <summary> Persistent information record associated with this player. </summary>
        public PlayerInfo Info { get; private set; }


        /// <summary> Whether the player has completed the login sequence. </summary>
        public SessionState State { get; private set; }

        /// <summary> Whether the player has completed the login sequence. </summary>
        public bool HasRegistered { get; internal set; }

        /// <summary> Whether the player registered and then finished loading the world. </summary>
        public bool HasFullyConnected { get; private set; }

        /// <summary> Whether the client is currently connected. </summary>
        public bool IsOnline {
            get {
                return State == SessionState.Online;
            }
        }

        /// <summary> Whether the player name was verified at login. </summary>
        public bool IsVerified { get; private set; }


        /// <summary> Whether the player is in paint mode (deleting blocks replaces them). Used by /Paint. </summary>
        public bool IsPainting { get; set; }

        /// <summary> Whether player has blocked all incoming chat.
        /// Deaf players can't hear anything. </summary>
        public bool IsDeaf { get; set; }


        /// <summary> The world that the player is currently on. May be null.
        /// Use Player.JoinWorld() to make players join another world. </summary>
        [CanBeNull]
        public World World { get; private set; }

        /// <summary> Map from the world that the player is on.
        /// Throws PlayerOpException if player does not have a world.
        /// Loads the map if it's not loaded. Guaranteed to not return null. </summary>
        [NotNull]
        public Map WorldMap {
            get {
                World world = World;
                if( world == null ) PlayerOpException.ThrowNoWorld( this );
                return world.LoadMap();
            }
        }

        /// <summary> Player's position in the current world. </summary>
        public Position Position;


        /// <summary> Time when the session connected. </summary>
        public DateTime LoginTime { get; private set; }

        /// <summary> Last time when the player was active (moving/messaging). UTC. </summary>
        public DateTime LastActiveTime { get; private set; }

        /// <summary> Last time when this player was patrolled by someone. </summary>
        public DateTime LastPatrolTime { get; set; }


        /// <summary> Last command called by the player. </summary>
        [CanBeNull]
        public CommandReader LastCommand { get; private set; }


        /// <summary> Plain version of the name (no formatting). </summary>
        [NotNull]
        public string Name {
            get { return Info.Name; }
        }

        /// <summary> Name formatted for display in the player list. </summary>
        [NotNull]
        public string ListName {
            get {
                string displayedName = Name;
                if( ConfigKey.RankPrefixesInList.Enabled() ) {
                    displayedName = Info.Rank.Prefix + displayedName;
                }
                if( ConfigKey.RankColorsInChat.Enabled() && Info.Rank.Color != Color.White ) {
                    displayedName = Info.Rank.Color + displayedName;
                }
                return displayedName;
            }
        }

        /// <summary> Name formatted for display in chat. </summary>
        public string ClassyName {
            get { return Info.ClassyName; }
        }

        /// <summary> Whether the client supports advanced WoM client functionality. </summary>
        public bool IsUsingWoM { get; private set; }


        /// <summary> Metadata associated with the session/player. </summary>
        [NotNull]
        public MetadataCollection<object> Metadata { get; private set; }

        #endregion


        // TODO: This should be replaced by a more generic solution, like an IEntity interface.
        /// <summary> This constructor is used to create pseudoplayers (such as Console and /dummy).
        /// Such players have unlimited permissions, but no world. </summary>
        /// <param name="id"> Assigned ID. Must be between 0 and 255. </param>
        /// <param name="name"> Player name. May not be empty null. </param>
        /// <param name="rank"> Rank to assign. May not be null. </param>
        /// <exception cref="ArgumentNullException"> If name or rank is null. </exception>
        /// <exception cref="ArgumentException"> If name is an empty string. </exception>
        public Player( ReservedPlayerID id, [NotNull] string name, [NotNull] Rank rank ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( rank == null ) throw new ArgumentNullException( "rank" );
            if( name.Length == 0 ) throw new ArgumentException( "Name must be at least 1 character long", "name" );
            Info = PlayerDB.AddSuperPlayer( id, name, rank );
            spamBlockLog = new Queue<DateTime>( Info.Rank.AntiGriefBlocks );
            IP = IPAddress.None;
            ResetAllBinds();
            State = SessionState.Offline;
        }


        #region Chat and Messaging

        static readonly TimeSpan ConfirmationTimeout = TimeSpan.FromSeconds( 60 );

        int muteWarnings;

        [CanBeNull]
        string partialMessage;


        /// <summary> Parses a message on behalf of this player. </summary>
        /// <param name="rawMessage"> Message to parse. </param>
        /// <exception cref="ArgumentNullException"> If rawMessage is null. </exception>
        public void ParseMessage( [NotNull] string rawMessage ) {
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );
            bool fromConsole = ( this == Console );

            // handle canceling selections and partial messages
            if( rawMessage.StartsWith( "/nvm", StringComparison.OrdinalIgnoreCase ) ||
                rawMessage.StartsWith( "/cancel", StringComparison.OrdinalIgnoreCase ) ) {
                if( partialMessage != null ) {
                    MessageNow( "Partial message canceled." );
                    partialMessage = null;
                } else if( IsMakingSelection ) {
                    SelectionCancel();
                    MessageNow( "Selection canceled." );
                } else {
                    MessageNow( "There is currently nothing to cancel." );
                }
                SendToSpectators( rawMessage );
                return;
            }

            if( partialMessage != null ) {
                rawMessage = partialMessage + rawMessage;
                partialMessage = null;
            }

            // replace %-codes with &-codes
            if( Can( Permission.UseColorCodes ) ) {
                rawMessage = Color.ReplacePercentCodes( rawMessage );
            }

            switch( Chat.GetRawMessageType( rawMessage ) ) {
                case RawMessageType.Chat: {
                        if( !Can( Permission.Chat ) ) return;

                        if( Info.IsMuted ) {
                            MessageMuted();
                            return;
                        }

                        if( DetectChatSpam() ) return;

                        if( rawMessage[0] == '!' ) {
                            rawMessage = rawMessage.Substring( 1 );
                        } else {
                            rawMessage = Chat.UnescapeLeadingSlashes( rawMessage );
                        }
                        rawMessage = Chat.UnescapeTrailingSlashes( rawMessage );

                        Chat.SendGlobal( this, rawMessage );
                    } break;

                case RawMessageType.WorldChat: {
                        if( !Can( Permission.Chat ) ) return;
                        if( Info.IsMuted ) {
                            MessageMuted();
                            return;
                        }
                        if( DetectChatSpam() ) return;
                        rawMessage = Chat.UnescapeLeadingSlashes( rawMessage );
                        rawMessage = Chat.UnescapeTrailingSlashes( rawMessage );
                        Chat.SendWorld( this, rawMessage );
                    } break;

                case RawMessageType.Command: {
                        if( rawMessage.EndsWith( "//" ) ) {
                            rawMessage = rawMessage.Substring( 0, rawMessage.Length - 1 );
                        }
                        CommandReader cmd = new CommandReader( rawMessage );
                        CommandDescriptor commandDescriptor = CommandManager.GetDescriptor( cmd.Name, true );

                        if( commandDescriptor == null ) {
                            MessageNow( "Unknown command \"{0}\". See &H/Commands", cmd.Name );
                        } else if( Info.IsFrozen && !commandDescriptor.UsableByFrozenPlayers ) {
                            MessageNow( "&WYou cannot use this command while frozen." );
                        } else {
                            if( !commandDescriptor.DisableLogging ) {
                                Logger.Log( LogType.UserCommand,
                                            "{0}: {1}", Name, rawMessage );
                            }
                            if( commandDescriptor.RepeatableSelection ) {
                                selectionRepeatCommand = cmd;
                            }
                            SendToSpectators( cmd.RawMessage );
                            CommandManager.ParseCommand( this, cmd, fromConsole );
                            if( !commandDescriptor.NotRepeatable ) {
                                LastCommand = cmd;
                            }
                        }
                    } break;


                case RawMessageType.RepeatCommand: {
                        if( LastCommand == null ) {
                            Message( "No command to repeat." );
                        } else {
                            if( Info.IsFrozen && !LastCommand.Descriptor.UsableByFrozenPlayers ) {
                                MessageNow( "&WYou cannot use this command while frozen." );
                                return;
                            }
                            LastCommand.Rewind();
                            Logger.Log( LogType.UserCommand,
                                        "{0} repeated: {1}",
                                        Name, LastCommand.RawMessage );
                            Message( "Repeat: {0}", LastCommand.RawMessage );
                            SendToSpectators( LastCommand.RawMessage );
                            CommandManager.ParseCommand( this, LastCommand, fromConsole );
                        }
                    } break;


                case RawMessageType.PrivateChat: {
                        if( !Can( Permission.Chat ) ) return;

                        if( Info.IsMuted ) {
                            MessageMuted();
                            return;
                        }

                        if( DetectChatSpam() ) return;

                        if( rawMessage.EndsWith( "//" ) ) {
                            rawMessage = rawMessage.Substring( 0, rawMessage.Length - 1 );
                        }

                        string otherPlayerName, messageText;
                        if( rawMessage[1] == ' ' ) {
                            otherPlayerName = rawMessage.Substring( 2, rawMessage.IndexOf( ' ', 2 ) - 2 );
                            messageText = rawMessage.Substring( rawMessage.IndexOf( ' ', 2 ) + 1 );
                        } else {
                            otherPlayerName = rawMessage.Substring( 1, rawMessage.IndexOf( ' ' ) - 1 );
                            messageText = rawMessage.Substring( rawMessage.IndexOf( ' ' ) + 1 );
                        }

                        if( otherPlayerName == "-" ) {
                            if( LastUsedPlayerName != null ) {
                                otherPlayerName = LastUsedPlayerName;
                            } else {
                                Message( "Cannot repeat player name: you haven't used any names yet." );
                                return;
                            }
                        }

                        // first, find ALL players (visible and hidden)
                        Player[] allPlayers = Server.FindPlayers( otherPlayerName, true );

                        // if there is more than 1 target player, exclude hidden players
                        if( allPlayers.Length > 1 ) {
                            allPlayers = Server.FindPlayers( this, otherPlayerName, true );
                        }

                        if( allPlayers.Length == 1 ) {
                            Player target = allPlayers[0];
                            if( target == this ) {
                                MessageNow( "Trying to talk to yourself?" );
                                return;
                            }
                            if( !target.IsIgnoring( Info ) && !target.IsDeaf ) {
                                Chat.SendPM( this, target, messageText );
                            }

                            if( !CanSee( target ) ) {
                                // message was sent to a hidden player
                                MessageNoPlayer( otherPlayerName );

                            } else {
                                // message was sent normally
                                LastUsedPlayerName = target.Name;
                                if( target.IsIgnoring( Info ) ) {
                                    if( CanSee( target ) ) {
                                        MessageNow( "&WCannot PM {0}&W: you are ignored.", target.ClassyName );
                                    }
                                } else if( target.IsDeaf ) {
                                    MessageNow( "&SCannot PM {0}&S: they are currently deaf.", target.ClassyName );
                                } else {
                                    MessageNow( "&Pto {0}: {1}",
                                                target.Name, messageText );
                                }
                            }

                        } else if( allPlayers.Length == 0 ) {
                            MessageNoPlayer( otherPlayerName );

                        } else {
                            MessageManyMatches( "player", allPlayers );
                        }
                    } break;


                case RawMessageType.RankChat: {
                        if( !Can( Permission.Chat ) ) return;

                        if( Info.IsMuted ) {
                            MessageMuted();
                            return;
                        }

                        if( DetectChatSpam() ) return;

                        if( rawMessage.EndsWith( "//" ) ) {
                            rawMessage = rawMessage.Substring( 0, rawMessage.Length - 1 );
                        }

                        Rank rank;
                        if( rawMessage[2] == ' ' ) {
                            rank = Info.Rank;
                        } else {
                            string rankName = rawMessage.Substring( 2, rawMessage.IndexOf( ' ' ) - 2 );
                            rank = RankManager.FindRank( rankName );
                            if( rank == null ) {
                                MessageNoRank( rankName );
                                break;
                            }
                        }

                        string messageText = rawMessage.Substring( rawMessage.IndexOf( ' ' ) + 1 );

                        Player[] spectators = Server.Players.NotRanked( Info.Rank )
                                                            .Where( p => p.spectatedPlayer == this )
                                                            .ToArray();
                        if( spectators.Length > 0 ) {
                            spectators.Message( "[Spectate]: &Fto rank {0}&F: {1}", rank.ClassyName, messageText );
                        }

                        Chat.SendRank( this, rank, messageText );
                    } break;


                case RawMessageType.Confirmation: {
                        if( Info.IsFrozen ) {
                            MessageNow( "&WYou cannot use any commands while frozen." );
                            return;
                        }
                        if( ConfirmCallback != null ) {
                            if( DateTime.UtcNow.Subtract( ConfirmRequestTime ) < ConfirmationTimeout ) {
                                SendToSpectators( "/ok" );
                                ConfirmCallback( this, ConfirmParameter );
                                ConfirmCallback = null;
                                ConfirmParameter = null;
                            } else {
                                MessageNow( "Confirmation timed out. Enter the command again." );
                            }
                        } else {
                            MessageNow( "There is no command to confirm." );
                        }
                    } break;


                case RawMessageType.PartialMessage:
                    partialMessage = rawMessage.Substring( 0, rawMessage.Length - 1 );
                    MessageNow( "Partial: &F{0}", partialMessage );
                    break;

                case RawMessageType.Invalid:
                    MessageNow( "Could not parse message." );
                    break;
            }
        }


        /// <summary> Sends a message to all players who are spectating this player, e.g. to forward typed-in commands and PMs. </summary>
        /// <param name="message"> Message to be displayed </param>
        /// <param name="args"> Additional arguments </param>
        /// <exception cref="ArgumentNullException"> If any of the parameters are null. </exception>
        public void SendToSpectators( [NotNull] string message, [NotNull] params object[] args ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            Player[] spectators = Server.Players.Where( p => p.spectatedPlayer == this ).ToArray();
            if( spectators.Length > 0 ) {
                spectators.Message( "[Spectate]: &F" + message, args );
            }
        }


        const string WoMAlertPrefix = "^detail.user.alert=";


        /// <summary> Sends a message as a WoM alert.
        /// Players who use World of Minecraft client will see this message on the left side of the screen.
        /// Other players will receive it as a normal message. </summary>
        /// <param name="message"> A composite format string for the message. "System color" code ("&S") will be prepended. </param>
        /// <param name="args"> An object array that contains zero or more objects to format. </param>
        /// <exception cref="ArgumentNullException"> If message is null. </exception>
        /// <exception cref="FormatException"> If message format is invalid. </exception>
        [StringFormatMethod( "message" )]
        public void MessageWoMAlert( [NotNull] string message, [NotNull] params object[] args ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            if( args.Length > 0 ) {
                message = String.Format( message, args );
            }
            if( this == Console ) {
                Logger.LogToConsole( message );
            } else if( IsUsingWoM ) {
                foreach( Packet p in LineWrapper.WrapPrefixed( WoMAlertPrefix, WoMAlertPrefix + Color.Sys + message ) ) {
                    Send( p );
                }
            } else {
                foreach( Packet p in LineWrapper.Wrap( Color.Sys + message ) ) {
                    Send( p );
                }
            }
        }


        /// <summary> Sends a text message to this player.
        /// If the message does not fit on one line, prefix ">" is prepended to wrapped line. </summary>
        /// <param name="message"> A composite format string for the message. "System color" code ("&S") will be prepended. </param>
        /// <param name="args"> An object array that contains zero or more objects to format. </param>
        /// <exception cref="ArgumentNullException"> If any of the method parameters are null. </exception>
        /// <exception cref="FormatException"> If message format is invalid. </exception>
        [StringFormatMethod( "message" )]
        public void Message( [NotNull] string message, [NotNull] params object[] args ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            if( args.Length > 0 ) {
                message = String.Format( message, args );
            }
            if( Info.IsSuper ) {
                Logger.LogToConsole( message );
            } else {
                foreach( Packet p in LineWrapper.Wrap( Color.Sys + message ) ) {
                    Send( p );
                }
            }
        }


        /// <summary> Sends a text message to this player, prefixing each line. </summary>
        /// <param name="prefix"> Prefix to prepend to each wrapped line. Not prepended to the first line. </param>
        /// <param name="message"> A composite format string for the message. "System color" code ("&S") will be prepended. </param>
        /// <param name="args"> An object array that contains zero or more objects to format. </param>
        /// <exception cref="ArgumentNullException"> If any of the method parameters are null. </exception>
        /// <exception cref="FormatException"> If message format is invalid. </exception>
        [StringFormatMethod( "message" )]
        public void MessagePrefixed( [NotNull] string prefix, [NotNull] string message, [NotNull] params object[] args ) {
            if( prefix == null ) throw new ArgumentNullException( "prefix" );
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            if( args.Length > 0 ) {
                message = String.Format( message, args );
            }
            if( this == Console ) {
                Logger.LogToConsole( message );
            } else {
                foreach( Packet p in LineWrapper.WrapPrefixed( prefix, message ) ) {
                    Send( p );
                }
            }
        }


        [StringFormatMethod( "message" )]
        internal void MessageNow( [NotNull] string message, [NotNull] params object[] args ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            if( IsDeaf ) return;
            if( args.Length > 0 ) {
                message = String.Format( message, args );
            }
            if( this == Console ) {
                Logger.LogToConsole( message );
            } else {
                if( Thread.CurrentThread != ioThread ) {
                    throw new InvalidOperationException( "MessageNow may only be called from player's own thread." );
                }
                foreach( Packet p in LineWrapper.Wrap( Color.Sys + message ) ) {
                    SendNow( p );
                }
            }
        }


        [StringFormatMethod( "message" )]
        internal void MessageNowPrefixed( [NotNull] string prefix, [NotNull] string message, [NotNull] params object[] args ) {
            if( prefix == null ) throw new ArgumentNullException( "prefix" );
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            if( IsDeaf ) return;
            if( args.Length > 0 ) {
                message = String.Format( message, args );
            }
            if( this == Console ) {
                Logger.LogToConsole( message );
            } else {
                if( Thread.CurrentThread != ioThread ) {
                    throw new InvalidOperationException( "MessageNow may only be called from player's own thread." );
                }
                foreach( Packet p in LineWrapper.WrapPrefixed( prefix, message ) ) {
                    Send( p );
                }
            }
        }


        #region Macros

        /// <summary> Prints "No players found matching ___" message. </summary>
        /// <param name="playerName"> Given name, for which no players were found. </param>
        /// <exception cref="ArgumentNullException"> If playerName is null. </exception>
        public void MessageNoPlayer( [NotNull] string playerName ) {
            if( playerName == null ) throw new ArgumentNullException( "playerName" );
            Message( "No players found matching \"{0}\"", playerName );
        }


        /// <summary> Prints "No worlds found matching ___" message. </summary>
        /// <param name="worldName"> Given name, for which no worlds were found. </param>
        /// <exception cref="ArgumentNullException"> If worldName is null. </exception>
        public void MessageNoWorld( [NotNull] string worldName ) {
            if( worldName == null ) throw new ArgumentNullException( "worldName" );
            Message( "No worlds found matching \"{0}\". See &H/Worlds", worldName );
        }


        const int MatchesToPrint = 30;

        /// <summary> Prints a comma-separated list of matches (up to 30): "More than one ___ matched: ___, ___, ..." </summary>
        /// <param name="itemType"> Type of item in the list. Should be singular (e.g. "player" or "world"). </param>
        /// <param name="items"> List of zero or more matches. ClassyName properties are used in the list. </param>
        /// <exception cref="ArgumentNullException"> If itemType or items is null. </exception>
        public void MessageManyMatches( [NotNull] string itemType, [NotNull] IEnumerable<IClassy> items ) {
            if( itemType == null ) throw new ArgumentNullException( "itemType" );
            if( items == null ) throw new ArgumentNullException( "items" );

            IClassy[] itemsEnumerated = items.ToArray();
            string nameList = itemsEnumerated.Take( MatchesToPrint ).JoinToString( ", ", p => p.ClassyName );
            int count = itemsEnumerated.Length;
            if( count > MatchesToPrint ) {
                Message( "More than {0} {1} matched: {2}",
                         count, itemType, nameList );
            } else {
                Message( "More than one {0} matched: {1}",
                         itemType, nameList );
            }
        }


        /// <summary> Prints "This command requires ___+ rank" message. </summary>
        /// <param name="permissions"> List of permissions required for the command. </param>
        /// <exception cref="ArgumentNullException"> If permissions is null. </exception>
        public void MessageNoAccess( [NotNull] params Permission[] permissions ) {
            if( permissions == null ) throw new ArgumentNullException( "permissions" );
            Rank reqRank = RankManager.GetMinRankWithAllPermissions( permissions );
            if( reqRank == null ) {
                Message( "None of the ranks have permissions for this command." );
            } else {
                Message( "This command requires {0}+&S rank.",
                         reqRank.ClassyName );
            }
        }


        /// <summary> Prints "This command requires ___+ rank" message. </summary>
        /// <param name="cmd"> Command to check. </param>
        /// <exception cref="ArgumentNullException"> If cmd is null. </exception>
        public void MessageNoAccess( [NotNull] CommandDescriptor cmd ) {
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            Rank reqRank = cmd.MinRank;
            if( reqRank == null ) {
                if( cmd.IsConsoleSafe ) {
                    Message( "\"{0}\" command is only allowed to be called from console on the server.",
                             cmd.Name );
                } else {
                    Message( "\"{0}\" command is disabled on the server.",
                             cmd.Name );
                }
            } else {
                Message( "\"{0}\" command requires {1}+&S rank.",
                         cmd.Name, reqRank.ClassyName );
            }
        }


        /// <summary> Prints "Unrecognized rank ___" message. </summary>
        /// <param name="rankName"> Given name, for which no rank was found. </param>
        public void MessageNoRank( [NotNull] string rankName ) {
            if( rankName == null ) throw new ArgumentNullException( "rankName" );
            Message( "Unrecognized rank \"{0}\". See &H/Ranks", rankName );
        }


        /// <summary> Prints "You cannot access files outside the map folder." message. </summary>
        public void MessageUnsafePath() {
            Message( "&WYou cannot access files outside the map folder." );
        }


        /// <summary> Prints "No zones found matching ___" message. </summary>
        /// <param name="zoneName"> Given name, for which no zones was found. </param>
        public void MessageNoZone( [NotNull] string zoneName ) {
            if( zoneName == null ) throw new ArgumentNullException( "zoneName" );
            Message( "No zones found matching \"{0}\". See &H/Zones", zoneName );
        }


        /// <summary> Prints "Unacceptable world name" message, and requirements for world names. </summary>
        /// <param name="worldName"> Given world name, deemed to be invalid. </param>
        public void MessageInvalidWorldName( [NotNull] string worldName ) {
            Message( "Unacceptable world name: \"{0}\"", worldName );
            Message( "World names must be 1-16 characters long, and only contain letters, numbers, and underscores." );
        }

        /// <summary> Prints "___ is not a valid player name" message. </summary>
        /// <param name="playerName"> Given player name, deemed to be invalid. </param>
        public void MessageInvalidPlayerName( [NotNull] string playerName ) {
            Message( "\"{0}\" is not a valid player name.", playerName );
        }


        /// <summary> Prints "You are muted for ___ longer" message. </summary>
        public void MessageMuted() {
            Message( "You are muted for {0} longer.",
                     Info.TimeMutedLeft.ToMiniString() );
        }

        /// <summary> Prints "Specify a time range up to ___" message </summary>
        public void MessageMaxTimeSpan() {
            Message( "Specify a time range up to {0}", DateTimeUtil.MaxTimeSpan.ToMiniString() );
        }

        #endregion


        #region Ignore

        readonly HashSet<PlayerInfo> ignoreList = new HashSet<PlayerInfo>();
        readonly object ignoreLock = new object();


        /// <summary> Checks whether this player is currently ignoring a given PlayerInfo.</summary>
        public bool IsIgnoring( [NotNull] PlayerInfo other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            lock( ignoreLock ) {
                return ignoreList.Contains( other );
            }
        }


        /// <summary> Adds a given PlayerInfo to the ignore list.
        /// Not that ignores are not persistent, and are reset when a player disconnects. </summary>
        /// <param name="other"> Player to ignore. </param>
        /// <returns> True if the player is now ignored,
        /// false is the player has already been ignored previously. </returns>
        public bool Ignore( [NotNull] PlayerInfo other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            if( other == Info ) PlayerOpException.ThrowCannotTargetSelf( this, Info, "ignore" );
            lock( ignoreLock ) {
                if( !ignoreList.Contains( other ) ) {
                    ignoreList.Add( other );
                    return true;
                } else {
                    return false;
                }
            }
        }


        /// <summary> Removes a given PlayerInfo from the ignore list. </summary>
        /// <param name="other"> PlayerInfo to unignore. </param>
        /// <returns> True if the player is no longer ignored,
        /// false if the player was already not ignored. </returns>
        public bool Unignore( [NotNull] PlayerInfo other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            lock( ignoreLock ) {
                return ignoreList.Remove( other );
            }
        }


        /// <summary> Returns a list of all currently-ignored players. </summary>
        [NotNull]
        public PlayerInfo[] IgnoreList {
            get {
                lock( ignoreLock ) {
                    return ignoreList.ToArray();
                }
            }
        }

        #endregion


        #region Confirmation

        /// <summary> Callback to be called when player types in "/ok" to confirm an action.
        /// Use Player.Confirm(...) methods to set this. </summary>
        [CanBeNull]
        public ConfirmationCallback ConfirmCallback { get; private set; }


        /// <summary> Custom parameter to be passed to Player.ConfirmCallback. </summary>
        [CanBeNull]
        public object ConfirmParameter { get; private set; }


        /// <summary> Time when the confirmation was requested. UTC. </summary>
        public DateTime ConfirmRequestTime { get; private set; }


        /// <summary> Request player to confirm continuing with the command.
        /// Player is prompted to type "/ok", and when he/she does,
        /// the command is called again with IsConfirmed flag set. </summary>
        /// <param name="cmd"> Command that needs confirmation. </param>
        /// <param name="message"> Message to print before "Type /ok to continue". </param>
        /// <param name="args"> Optional String.Format() arguments, for the message. </param>
        /// <exception cref="ArgumentNullException"> If cmd, message, or args is null. </exception>
        [StringFormatMethod( "message" )]
        public void Confirm( [NotNull] CommandReader cmd, [NotNull] string message, [NotNull] params object[] args ) {
            Confirm( ConfirmCommandCallback, cmd, message, args );
        }


        /// <summary> Request player to confirm an action.
        /// Player is prompted to type "/ok", and when he/she does, custom callback will be called </summary>
        /// <param name="callback"> Method to call when player confirms. </param>
        /// <param name="callbackParameter"> Argument to pass to the callback. May be null. </param>
        /// <param name="message"> Message to print before "Type /ok to continue". </param>
        /// <param name="args"> Optional String.Format() arguments, for the message. </param>
        /// <exception cref="ArgumentNullException"> If callback, message, or args is null. </exception>
        [StringFormatMethod( "message" )]
        public void Confirm( [NotNull] ConfirmationCallback callback, [CanBeNull] object callbackParameter, [NotNull] string message, [NotNull] params object[] args ) {
            if( callback == null ) throw new ArgumentNullException( "callback" );
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            ConfirmCallback = callback;
            ConfirmParameter = callbackParameter;
            ConfirmRequestTime = DateTime.UtcNow;
            Message( "{0} Type &H/ok&S to continue.", String.Format( message, args ) );
        }


        static void ConfirmCommandCallback( [NotNull] Player player, object tag ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            CommandReader cmd = (CommandReader)tag;
            cmd.Rewind();
            cmd.IsConfirmed = true;
            bool fromConsole = ( player == Console );
            CommandManager.ParseCommand( player, cmd, fromConsole );
        }

        #endregion


        #region AntiSpam
        
        /// <summary> Number of messages in a AntiSpamInterval seconds required to trigger the anti-spam filter </summary>
        public static int AntispamMessageCount = 3;

        /// <summary> Interval in seconds to record number of message for anti-spam filter </summary>
        public static int AntispamInterval = 4;
        readonly Queue<DateTime> spamChatLog = new Queue<DateTime>( AntispamMessageCount );

        internal bool DetectChatSpam() {
            if( Info.IsSuper ) return false;
            if( spamChatLog.Count >= AntispamMessageCount ) {
                DateTime oldestTime = spamChatLog.Dequeue();
                if( DateTime.UtcNow.Subtract( oldestTime ).TotalSeconds < AntispamInterval ) {
                    muteWarnings++;
                    if( muteWarnings > ConfigKey.AntispamMaxWarnings.GetInt() ) {
                        KickNow( "You were kicked for repeated spamming.", LeaveReason.MessageSpamKick );
                        Server.Message( "&W{0} was kicked for repeated spamming.", ClassyName );
                    } else {
                        TimeSpan autoMuteDuration = TimeSpan.FromSeconds( ConfigKey.AntispamMuteDuration.GetInt() );
                        Info.Mute( Console, autoMuteDuration, false, true );
                        Message( "You have been muted for {0} seconds. Slow down.", autoMuteDuration );
                    }
                    return true;
                }
            }
            spamChatLog.Enqueue( DateTime.UtcNow );
            return false;
        }

        #endregion

        #endregion


        #region Placing Blocks

        // For grief/spam detection
        readonly Queue<DateTime> spamBlockLog = new Queue<DateTime>();

        /// <summary> Last blocktype used by the player.
        /// Make sure to use in conjunction with Player.GetBind() to ensure that bindings are properly applied. </summary>
        public Block LastUsedBlockType { get; private set; }

        /// <summary> Max distance that player may be from a block to reach it (hack detection). </summary>
        public static int MaxBlockPlacementRange { get; set; }


        /// <summary> Handles manually-placed/deleted blocks.
        /// Returns true if player's action should result in a kick. </summary>
        public bool PlaceBlock( Vector3I coord, ClickAction action, Block type ) {
            if( World == null ) PlayerOpException.ThrowNoWorld( this );
            Map map = WorldMap;
            LastUsedBlockType = type;

            Vector3I coordBelow = new Vector3I( coord.X, coord.Y, coord.Z - 1 );

            // check if player is frozen or too far away to legitimately place a block
            if( Info.IsFrozen ||
                Math.Abs( coord.X * 32 - Position.X ) > MaxBlockPlacementRange ||
                Math.Abs( coord.Y * 32 - Position.Y ) > MaxBlockPlacementRange ||
                Math.Abs( coord.Z * 32 - Position.Z ) > MaxBlockPlacementRange ) {
                RevertBlockNow( coord );
                return false;
            }

            if( IsSpectating ) {
                RevertBlockNow( coord );
                Message( "You cannot build or delete while spectating." );
                return false;
            }

            if( World.IsLocked ) {
                RevertBlockNow( coord );
                Message( "This map is currently locked (read-only)." );
                return false;
            }

            if( CheckBlockSpam() ) return true;

            BlockChangeContext context = BlockChangeContext.Manual;
            if( IsPainting && action == ClickAction.Delete ) {
                context |= BlockChangeContext.Replaced;
            }

            // bindings
            bool requiresUpdate = (type != bindings[(byte)type] || IsPainting);
            if( action == ClickAction.Delete && !IsPainting ) {
                type = Block.Air;
            }
            type = bindings[(byte)type];

            // selection handling
            if( SelectionMarksExpected > 0 ) {
                RevertBlockNow( coord );
                SelectionAddMark( coord, true );
                return false;
            }

            bool stackStairs = (type == Block.Stair &&
                                coord.Z > 0 &&
                                map.GetBlock( coordBelow ) == Block.Stair);
            CanPlaceResult canPlaceResult;
            if( stackStairs ) {
                // stair stacking
                canPlaceResult = CanPlace( map, coordBelow, Block.DoubleStair, context );
            } else {
                // normal placement
                canPlaceResult = CanPlace( map, coord, type, context );
            }

            // if all is well, try placing it
            switch( canPlaceResult ) {
                case CanPlaceResult.Allowed:
                    BlockUpdate blockUpdate;
                    if( stackStairs ) {
                        // handle stair stacking
                        RevertBlockNow( coord );
                        blockUpdate = new BlockUpdate( this, coordBelow, Block.DoubleStair );
                        Info.ProcessBlockPlaced( Block.DoubleStair );
                        map.QueueUpdate( blockUpdate );
                        RaisePlayerPlacedBlockEvent( this, World.Map, coordBelow, Block.Stair, Block.DoubleStair, context );
                        SendNow( Packet.MakeSetBlock( coordBelow, Block.DoubleStair ) );

                    } else {
                        // handle normal blocks
                        blockUpdate = new BlockUpdate( this, coord, type );
                        Info.ProcessBlockPlaced( type );
                        Block old = map.GetBlock( coord );
                        map.QueueUpdate( blockUpdate );
                        RaisePlayerPlacedBlockEvent( this, World.Map, coord, old, type, context );
                        if( requiresUpdate || RelayAllUpdates ) {
                            SendNow( Packet.MakeSetBlock( coord, type ) );
                        }
                    }
                    break;

                case CanPlaceResult.BlocktypeDenied:
                    RevertBlockNow( coord );
                    Message( "&WYou are not permitted to affect this block type." );
                    break;

                case CanPlaceResult.RankDenied:
                    RevertBlockNow( coord );
                    Message( "&WYour rank is not allowed to build." );
                    break;

                case CanPlaceResult.WorldDenied:
                    RevertBlockNow( coord );
                    switch( World.BuildSecurity.CheckDetailed( Info ) ) {
                        case SecurityCheckResult.RankTooLow:
                            Message( "&WYour rank is not allowed to build in this world." );
                            break;
                        case SecurityCheckResult.BlackListed:
                            Message( "&WYou are not allowed to build in this world." );
                            break;
                    }
                    break;

                case CanPlaceResult.ZoneDenied:
                    RevertBlockNow( coord );
                    Zone deniedZone = WorldMap.Zones.FindDenied( coord, this );
                    if( deniedZone != null ) {
                        Message( "&WYou are not allowed to build in zone \"{0}\".", deniedZone.Name );
                    } else {
                        Message( "&WYou are not allowed to build here." );
                    }
                    break;

                case CanPlaceResult.PluginDenied:
                    RevertBlockNow( coord );
                    break;

                //case CanPlaceResult.PluginDeniedNoUpdate:
                //    break;
            }
            return false;
        }


        /// <summary> Gets the block from given location in player's world,
        /// and sends it (async) to the player.
        /// Used to undo player's attempted block placement/deletion. </summary>
        public void RevertBlock( Vector3I coords ) {
            SendLowPriority( Packet.MakeSetBlock( coords, WorldMap.GetBlock( coords ) ) );
        }


        /// <summary> Sends a block change to THIS PLAYER ONLY. Does not affect the map. </summary>
        /// <param name="coords"> Coordinates of the block. </param>
        /// <param name="block"> Block type to send. </param>
        public void SendBlock( Vector3I coords, Block block ) {
            if( !WorldMap.InBounds( coords ) ) throw new ArgumentOutOfRangeException( "coords" );
            SendLowPriority( Packet.MakeSetBlock( coords, block ) );
        }


        // Gets the block from given location in player's world, and sends it (sync) to the player.
        // Used to undo player's attempted block placement/deletion.
        // To avoid threading issues, only use this from this player's IoThread.
        void RevertBlockNow( Vector3I coords ) {
            SendNow( Packet.MakeSetBlock( coords, WorldMap.GetBlock( coords ) ) );
        }


        // returns true if the player is spamming and should be kicked.
        bool CheckBlockSpam() {
            if( Info.Rank.AntiGriefBlocks == 0 || Info.Rank.AntiGriefSeconds == 0 ) return false;
            if( spamBlockLog.Count >= Info.Rank.AntiGriefBlocks ) {
                DateTime oldestTime = spamBlockLog.Dequeue();
                double spamTimer = DateTime.UtcNow.Subtract( oldestTime ).TotalSeconds;
                if( spamTimer < Info.Rank.AntiGriefSeconds ) {
                    KickNow( "You were kicked by antigrief system. Slow down.", LeaveReason.BlockSpamKick );
                    Server.Message( "{0}&W was kicked for suspected griefing.", ClassyName );
                    Logger.Log( LogType.SuspiciousActivity,
                                "{0} was kicked for block spam ({1} blocks in {2} seconds)",
                                Name, Info.Rank.AntiGriefBlocks, spamTimer );
                    return true;
                }
            }
            spamBlockLog.Enqueue( DateTime.UtcNow );
            return false;
        }

        #endregion


        #region Binding

        readonly Block[] bindings = new Block[50];


        /// <summary> Binds one block type to another, for use with draw commands. </summary>
        public void Bind( Block originalType, Block replacementType ) {
            bindings[(byte)originalType] = replacementType;
        }

        /// <summary> Resets binding for given type(s). </summary>
        public void ResetBind( [NotNull] params Block[] originalTypes ) {
            if( originalTypes == null ) throw new ArgumentNullException( "originalTypes" );
            foreach( Block type in originalTypes ) {
                bindings[(byte)type] = type;
            }
        }


        /// <summary> Gets a binding for given block type.
        /// If no replacement is specified, returns the original type. </summary>
        public Block GetBind( Block originalType ) {
            return bindings[(byte)originalType];
        }


        /// <summary> Resets all bindings to default. </summary>
        public void ResetAllBinds() {
            for( int i = 0; i < bindings.Length; i++ ) {
                bindings[i] = (Block)i;
            }
        }

        #endregion


        #region Permission Checks

        /// <summary> Returns true if player has ALL of the given permissions. </summary>
        public bool Can( [NotNull] params Permission[] permissions ) {
            if( permissions == null ) throw new ArgumentNullException( "permissions" );
            return Info.IsSuper || permissions.All( Info.Rank.Can );
        }


        /// <summary> Returns true if player has ANY of the given permissions. </summary>
        public bool CanAny( [NotNull] params Permission[] permissions ) {
            if( permissions == null ) throw new ArgumentNullException( "permissions" );
            return Info.IsSuper || permissions.Any( Info.Rank.Can );
        }


        /// <summary> Returns true if player has the given permission. </summary>
        public bool Can( Permission permission ) {
            return Info.IsSuper || Info.Rank.Can( permission );
        }


        /// <summary> Returns true if player has the given permission,
        /// and is allowed to affect players of the given rank. </summary>
        public bool Can( Permission permission, [NotNull] Rank other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            return Info.IsSuper || Info.Rank.Can( permission, other );
        }


        /// <summary> Returns true if player has the given permission,
        /// and is allowed to affect players of the given rank. </summary>
        public bool Can( Permission permission, [NotNull] Player other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            return Info.IsSuper || Info.Rank.Can( permission, other.Info.Rank );
        }


        /// <summary> Returns true if player is allowed to run
        /// draw commands that affect a given number of blocks. </summary>
        public bool CanDraw( int volume ) {
            if( volume < 0 ) throw new ArgumentOutOfRangeException( "volume" );
            return Info.IsSuper || ( Info.Rank.DrawLimit == 0 ) || ( volume <= Info.Rank.DrawLimit );
        }


        /// <summary> Returns true if player is allowed to join a given world. </summary>
        public bool CanJoin( [NotNull] World worldToJoin ) {
            if( worldToJoin == null ) throw new ArgumentNullException( "worldToJoin" );
            return Info.IsSuper || worldToJoin.AccessSecurity.Check( Info );
        }


        /// <summary> Checks whether player is allowed to place a block on the current world at given coordinates.
        /// Raises the PlayerPlacingBlock event. </summary>
        public CanPlaceResult CanPlace( [NotNull] Map map, Vector3I coords, Block newBlock, BlockChangeContext context ) {
            if( map == null ) throw new ArgumentNullException( "map" );
            CanPlaceResult result;

            // check whether coordinate is in bounds
            Block oldBlock = map.GetBlock( coords );
            if( oldBlock == Block.None ) {
                result = CanPlaceResult.OutOfBounds;
                goto eventCheck;
            }

            // check special blocktypes
            if( newBlock == Block.Admincrete && !Can( Permission.PlaceAdmincrete ) ) {
                result = CanPlaceResult.BlocktypeDenied;
                goto eventCheck;
            } else if( (newBlock == Block.Water || newBlock == Block.StillWater) && !Can( Permission.PlaceWater ) ) {
                result = CanPlaceResult.BlocktypeDenied;
                goto eventCheck;
            } else if( (newBlock == Block.Lava || newBlock == Block.StillLava) && !Can( Permission.PlaceLava ) ) {
                result = CanPlaceResult.BlocktypeDenied;
                goto eventCheck;
            }

            // check admincrete-related permissions
            if( oldBlock == Block.Admincrete && !Can( Permission.DeleteAdmincrete ) ) {
                result = CanPlaceResult.BlocktypeDenied;
                goto eventCheck;
            }

            // check zones & world permissions
            PermissionOverride zoneCheckResult = map.Zones.Check( coords, this );
            switch( zoneCheckResult ) {
                case PermissionOverride.Allow:
                    result = CanPlaceResult.Allowed;
                    goto eventCheck;
                case PermissionOverride.Deny:
                    result = CanPlaceResult.ZoneDenied;
                    goto eventCheck;
            }

            World mapWorld = map.World;
            if( mapWorld != null ) {
                // Check world permissions
                switch( mapWorld.BuildSecurity.CheckDetailed( Info ) ) {
                    case SecurityCheckResult.Allowed:
                        // Check world's rank permissions
                        if( (Can( Permission.Build ) || newBlock == Block.Air) &&
                            (Can( Permission.Delete ) || oldBlock == Block.Air) ) {
                            result = CanPlaceResult.Allowed;
                        } else {
                            result = CanPlaceResult.RankDenied;
                        }
                        break;

                    case SecurityCheckResult.WhiteListed:
                        result = CanPlaceResult.Allowed;
                        break;

                    default:
                        result = CanPlaceResult.WorldDenied;
                        break;
                }
            } else {
                result = CanPlaceResult.Allowed;
            }

        eventCheck:
            var e = new PlayerPlacingBlockEventArgs( this, map, coords, oldBlock, newBlock, context, result );
            PlacingBlockEvent.Raise( e );
            return e.Result;
        }


        /// <summary> Whether this player can currently see another player as being online.
        /// Visibility is determined by whether the other player is hiding or spectating.
        /// Players can always see themselves. Super players (e.g. Console) can see all.
        /// Hidden players can only be seen by those of sufficient rank. </summary>
        public bool CanSee( [NotNull] Player other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            return other == this ||
                   Info.IsSuper ||
                   !other.Info.IsHidden ||
                   Info.Rank.CanSeeHidden( other.Info.Rank );
        }


        /// <summary> Whether this player can currently see another player moving.
        /// Behaves very similarly to CanSee method, except when spectating:
        /// Players can never see someone who's spectating them. If other player is spectating
        /// someone else, they are treated as hidden and can only be seen by those of sufficient rank. </summary>
        public bool CanSeeMoving( [NotNull] Player other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            return other == this ||
                   Info.IsSuper ||
                   other.spectatedPlayer == null && !other.Info.IsHidden ||
                   ( other.spectatedPlayer != this && Info.Rank.CanSeeHidden( other.Info.Rank ) );
        }


        /// <summary> Whether this player should see a given world on the /Worlds list by default. </summary>
        public bool CanSee( [NotNull] World world ) {
            if( world == null ) throw new ArgumentNullException( "world" );
            return CanJoin( world ) && !world.IsHidden;
        }

        #endregion


        #region Undo / Redo

        readonly LinkedList<UndoState> undoStack = new LinkedList<UndoState>();
        readonly LinkedList<UndoState> redoStack = new LinkedList<UndoState>();

        [NotNull]
        public UndoState RedoBegin( [CanBeNull] DrawOperation op ) {
            LastDrawOp = op;
            UndoState newState = new UndoState( op );
            undoStack.AddLast( newState );
            return newState;
        }


        [CanBeNull]
        public UndoState RedoPop() {
            if( redoStack.Count > 0 ) {
                var lastNode = redoStack.Last;
                redoStack.RemoveLast();
                return lastNode.Value;
            } else {
                return null;
            }
        }


        public void RedoClear() {
            redoStack.Clear();
        }


        [NotNull]
        public UndoState UndoBegin( [CanBeNull] DrawOperation op ) {
            LastDrawOp = op;
            UndoState newState = new UndoState( op );
            redoStack.AddLast( newState );
            return newState;
        }


        [CanBeNull]
        public UndoState UndoPop() {
            if( undoStack.Count > 0 ) {
                var lastNode = undoStack.Last;
                undoStack.RemoveLast();
                return lastNode.Value;
            } else {
                return null;
            }
        }


        public void UndoClear() {
            undoStack.Clear();
        }


        [NotNull]
        public UndoState DrawBegin( [CanBeNull] DrawOperation op ) {
            LastDrawOp = op;
            UndoState newState = new UndoState( op );
            undoStack.AddLast( newState );
            if( undoStack.Count > ConfigKey.MaxUndoStates.GetInt() ) {
                undoStack.RemoveFirst();
            }
            redoStack.Clear();
            return newState;
        }

        #endregion


        #region Drawing, Selection

        [NotNull]
        public IBrush Brush { get; set; }

        /// <summary> The last draw operation completed by the player </summary>
        [CanBeNull]
        public DrawOperation LastDrawOp { get; set; }


        /// <summary> Whether player is currently making a selection. </summary>
        public bool IsMakingSelection {
            get { return SelectionMarksExpected > 0; }
        }

        /// <summary> Number of selection marks so far. </summary>
        public int SelectionMarkCount {
            get { return selectionMarks.Count; }
        }

        /// <summary> Number of marks expected to complete the selection. </summary>
        public int SelectionMarksExpected { get; private set; }

        /// <summary> Whether player is repeating a selection (/static) </summary>
        public bool IsRepeatingSelection { get; set; }

        [CanBeNull]
        CommandReader selectionRepeatCommand;

        [CanBeNull]
        SelectionCallback selectionCallback;

        readonly Queue<Vector3I> selectionMarks = new Queue<Vector3I>();

        [CanBeNull]
        object selectionArgs;

        [CanBeNull]
        Permission[] selectionPermissions;

        /// <summary> Adds a mark to the player's current selection </summary>
        /// <param name="pos"> Position of the mark to be added </param>
        /// <param name="executeCallbackIfNeeded"> Determines if callback will be executed </param>
        public void SelectionAddMark( Vector3I pos, bool executeCallbackIfNeeded ) {
            if( !IsMakingSelection ) throw new InvalidOperationException( "No selection in progress." );
            selectionMarks.Enqueue( pos );
            if( SelectionMarkCount >= SelectionMarksExpected ) {
                if( executeCallbackIfNeeded ) {
                    SelectionExecute();
                } else {
                    Message( "Last block marked at {0}. Type &H/Mark&S or click any block to continue.", pos );
                }
            } else {
                Message( "Block #{0} marked at {1}. Place mark #{2}.",
                         SelectionMarkCount, pos, SelectionMarkCount + 1 );
            }
        }


        public void SelectionExecute() {
            if( !IsMakingSelection || selectionCallback == null ) {
                throw new InvalidOperationException( "No selection in progress." );
            }
            SelectionMarksExpected = 0;
            // check if player still has the permissions required to complete the selection.
            if( selectionPermissions == null || Can( selectionPermissions ) ) {
                selectionCallback( this, selectionMarks.ToArray(), selectionArgs );
                if( IsRepeatingSelection && selectionRepeatCommand != null ) {
                    selectionRepeatCommand.Rewind();
                    CommandManager.ParseCommand( this, selectionRepeatCommand, this == Console );
                }
                selectionMarks.Clear();
            } else {
                // More complex permission checks can be done in the callback function itself.
                Message( "&WYou are no longer allowed to complete this action." );
                MessageNoAccess( selectionPermissions );
            }
        }


        /// <summary> Initates the selection </summary>
        /// <param name="marksExpected"> Number of marks that are needed to create the selection </param>
        /// <param name="callback"> Selection callback </param>
        /// <param name="args"> Arguments for selection </param>
        /// <param name="requiredPermissions"> Permissions required in order to complete the command </param>
        public void SelectionStart( int marksExpected,
                                    [NotNull] SelectionCallback callback,
                                    [CanBeNull] object args,
                                    [CanBeNull] params Permission[] requiredPermissions ) {
            if( callback == null ) throw new ArgumentNullException( "callback" );
            selectionArgs = args;
            SelectionMarksExpected = marksExpected;
            selectionMarks.Clear();
            selectionCallback = callback;
            selectionPermissions = requiredPermissions;
        }


        /// <summary> Resets the player's marks used to create the selection </summary>
        public void SelectionResetMarks() {
            selectionMarks.Clear();
        }

        /// <summary> Cancels the player's current selection </summary>
        public void SelectionCancel() {
            selectionMarks.Clear();
            SelectionMarksExpected = 0;
            selectionCallback = null;
            selectionArgs = null;
            selectionPermissions = null;
        }

        #endregion


        #region Copy/Paste

        /// <summary> Gets raw copy/paste slot array.
        /// Use GetCopyInformation/SetCopyInformation to work with current slot instead. </summary>
        public CopyState[] CopyInformation {
            get { return copyInformation; }
        }
        CopyState[] copyInformation;


        /// <summary> Gets or sets current copy slot (zero-based index).
        /// Maximum CopySlot numeber is limited by rank (see Info.Rank.CopySlots). </summary>
        public int CopySlot {
            get { return copySlot; }
            set {
                if( value < 0 || value > Info.Rank.CopySlots ) {
                    throw new ArgumentOutOfRangeException( "value" );
                }
                copySlot = value;
            }
        }
        int copySlot;


        internal void InitCopySlots() {
            Array.Resize( ref copyInformation, Info.Rank.CopySlots );
            CopySlot = Math.Min( CopySlot, Info.Rank.CopySlots - 1 );
        }


        /// <summary> Gets copy information from the currently selected slot.</summary>
        /// <returns> CopyState from the current slot, or null if nothing is copied. </returns>
        [CanBeNull]
        public CopyState GetCopyInformation() {
            return CopyInformation[copySlot];
        }


        /// <summary> Stores given CopyState into the selected copy slot. </summary>
        public void SetCopyInformation( [CanBeNull] CopyState info ) {
            if( info != null ) info.Slot = copySlot;
            CopyInformation[copySlot] = info;
        }

        #endregion


        #region Spectating

        [NotNull]
        readonly object spectateLock = new object();

        [CanBeNull]
        Player spectatedPlayer;

        /// <summary> Player currently being spectated. Use Spectate/StopSpectate methods to set.
        /// May be null if player is not spectating anyone. </summary>
        [CanBeNull]
        public Player SpectatedPlayer {
            get { return spectatedPlayer; }
        }

        /// <summary> Last person to be spectated by this player. If player is currently spectating,
        /// this is same as SpectatedPlayer property. May be null if player has not spectated anyone. </summary>
        [CanBeNull]
        public PlayerInfo LastSpectatedPlayer { get; private set; }


        /// <summary> True if this player is currently spectating someone. </summary>
        public bool IsSpectating {
            get { return (spectatedPlayer != null); }
        }


        /// <summary> Starts spectating the target.
        /// May cause a world change (async) if target is on another world. 
        /// Throws PlayerOpException in case of insufficient permissions, or when trying to spectate self. </summary>
        /// <returns> True if spectating worked. False if target is already being spectated. </returns>
        public bool Spectate( [NotNull] Player target ) {
            if( target == null ) throw new ArgumentNullException( "target" );
            lock( spectateLock ) {
                if( target == this ) {
                    PlayerOpException.ThrowCannotTargetSelf( this, Info, "spectate" );
                }

                if( !Can( Permission.Spectate, target ) ) {
                    PlayerOpException.ThrowPermissionLimit( this, target.Info, "spectate", Permission.Spectate );
                }

                if( spectatedPlayer == target ) return false;

                spectatedPlayer = target;
                LastSpectatedPlayer = target.Info;
                Message( "Now spectating {0}&S. Type &H/unspec&S to stop.", target.ClassyName );
                return true;
            }
        }

        
        /// <summary> Stops spectating. </summary>
        /// <returns> True if this player was spectating someone and has now stopped.
        /// False if this player was not spectating anyone. </returns>
        public bool StopSpectating() {
            lock( spectateLock ) {
                if( spectatedPlayer == null ) return false;
                Message( "Stopped spectating {0}", spectatedPlayer.ClassyName );
                spectatedPlayer = null;
                return true;
            }
        }

        #endregion


        #region Static Utilities

        static readonly Uri PaidCheckUri = new Uri( "http://www.minecraft.net/haspaid.jsp?user=" );
        const int PaidCheckTimeout = 5000;


        /// <summary> Checks whether a given player has a paid minecraft.net account. </summary>
        /// <returns> True if the account is paid. False if it is not paid, or if information is unavailable. </returns>
        public static bool CheckPaidStatus( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create( PaidCheckUri + Uri.EscapeDataString( name ) );
            request.ServicePoint.BindIPEndPointDelegate = Server.BindIPEndPointCallback;
            request.Timeout = PaidCheckTimeout;
            request.CachePolicy = new RequestCachePolicy( RequestCacheLevel.NoCacheNoStore );

            try {
                using( WebResponse response = request.GetResponse() ) {
                    using( StreamReader responseReader = new StreamReader( response.GetResponseStream() ) ) {
                        string paidStatusString = responseReader.ReadToEnd();
                        bool isPaid;
                        return Boolean.TryParse( paidStatusString, out isPaid ) && isPaid;
                    }
                }
            } catch( WebException ex ) {
                Logger.Log( LogType.Warning,
                            "Could not check paid status of player {0}: {1}",
                            name, ex.Message );
                return false;
            }
        }


        /// <summary> Ensures that a player name has the correct length and character set. </summary>
        public static bool IsValidName( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( name.Length < 2 || name.Length > 16 ) return false;
            return ContainsValidCharacters( name );
        }


        /// <summary> Ensures that a player name has the correct length and character set. </summary>
        public static bool ContainsValidCharacters( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            for( int i = 0; i < name.Length; i++ ) {
                char ch = name[i];
                if( (ch < '0' && ch != '.') || (ch > '9' && ch < 'A') || (ch > 'Z' && ch < '_') || (ch > '_' && ch < 'a') || ch > 'z' ) {
                    return false;
                }
            }
            return true;
        }

        #endregion


        /// <summary> Teleports this player to a location within the current world. </summary>
        public void TeleportTo( Position pos ) {
            if( World == null ) PlayerOpException.ThrowNoWorld( this );
            StopSpectating();
            Send( Packet.MakeSelfTeleport( pos ) );
            Position = pos;
        }


        /// <summary> Time since the player was last active (moved, talked, or clicked). </summary>
        public TimeSpan IdleTime {
            get {
                return DateTime.UtcNow.Subtract( LastActiveTime );
            }
        }


        /// <summary> Resets the IdleTimer to 0. </summary>
        public void ResetIdleTimer() {
            LastActiveTime = DateTime.UtcNow;
        }


        #region Kick

        /// <summary> Advanced kick command. </summary>
        /// <param name="player"> Player who is kicking. </param>
        /// <param name="reason"> Reason for kicking. May be null or blank if allowed by server configuration. </param>
        /// <param name="context"> Classification of kick context. </param>
        /// <param name="announce"> Whether the kick should be announced publicly on the server and IRC. </param>
        /// <param name="raiseEvents"> Whether Player.BeingKicked and Player.Kicked events should be raised. </param>
        /// <param name="recordToPlayerDB"> Whether the kick should be counted towards player's record.</param>
        public void Kick( [NotNull] Player player, [CanBeNull] string reason, LeaveReason context,
                          bool announce, bool raiseEvents, bool recordToPlayerDB ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( !Enum.IsDefined( typeof( LeaveReason ), context ) ) {
                throw new ArgumentOutOfRangeException( "context" );
            }
            if( reason != null && reason.Trim().Length == 0 ) reason = null;

            // Check if player can ban/unban in general
            if( !player.Can( Permission.Kick ) ) {
                PlayerOpException.ThrowPermissionMissing( player, Info, "kick", Permission.Kick );
            }

            // Check if player is trying to ban/unban self
            if( player == this ) {
                PlayerOpException.ThrowCannotTargetSelf( player, Info, "kick" );
            }

            // Check if player has sufficiently high permission limit
            if( !player.Can( Permission.Kick, Info.Rank ) ) {
                PlayerOpException.ThrowPermissionLimit( player, Info, "kick", Permission.Kick );
            }

            // check if kick reason is missing but required
            PlayerOpException.CheckKickReason( reason, player, Info );

            // raise Player.BeingKicked event
            if( raiseEvents ) {
                var e = new PlayerBeingKickedEventArgs( this, player, reason, announce, recordToPlayerDB, context );
                BeingKickedEvent.Raise( e );
                if( e.Cancel ) PlayerOpException.ThrowCanceled( player, Info );
                recordToPlayerDB = e.RecordToPlayerDB;
                announce = e.Announce;
                reason = e.Reason;
            }

            // actually kick
            string kickReason;
            if( reason != null ) {
                kickReason = String.Format( "Kicked by {0}: {1}", player.Name, reason );
            } else {
                kickReason = String.Format( "Kicked by {0}", player.Name );
            }
            Kick( kickReason, context );

            // log and record kick to PlayerDB
            Logger.Log( LogType.UserActivity,
                        "{0} kicked {1}. Reason: {2}",
                        player.Name, Name, reason ?? "" );
            if( recordToPlayerDB ) {
                Info.ProcessKick( player, reason );
            }

            // announce kick
            if( announce ) {
                if( reason != null && ConfigKey.AnnounceKickAndBanReasons.Enabled() ) {
                    Server.Message( "{0}&W was kicked by {1}&W: {2}",
                                    ClassyName, player.ClassyName, reason );
                } else {
                    Server.Message( "{0}&W was kicked by {1}",
                                    ClassyName, player.ClassyName );
                }
            }

            // raise Player.Kicked event
            if( raiseEvents ) {
                var e = new PlayerKickedEventArgs( this, player, reason, announce, recordToPlayerDB, context );
                KickedEvent.Raise( e );
            }
        }

        #endregion


        /// <summary> Last player name typed in by this player. Always a full/exact name. May be null. </summary>
        [CanBeNull]
        public string LastUsedPlayerName { get; set; }


        /// <summary> Last world name typed in by this player. Always a full/exact name. May be null. </summary>
        [CanBeNull]
        public string LastUsedWorldName { get; set; }


        /// <summary> Object description in the form Player(Name) or Player(IP) </summary>
        public override string ToString() {
            if( Info != null ) {
                return String.Format( "Player({0})", Info.Name );
            } else {
                return String.Format( "Player({0})", IP );
            }
        }
    }


    sealed class PlayerListSorter : IComparer<Player> {
        public static readonly PlayerListSorter Instance = new PlayerListSorter();

        public int Compare( Player x, Player y ) {
            if( x.Info.Rank == y.Info.Rank ) {
                return StringComparer.OrdinalIgnoreCase.Compare( x.Name, y.Name );
            } else {
                return x.Info.Rank.Index - y.Info.Rank.Index;
            }
        }
    }
}