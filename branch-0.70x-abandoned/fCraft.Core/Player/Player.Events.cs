﻿// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Net;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    partial class Player {
        /// <summary> Occurs when a player is connecting (cancellable).
        /// Player name is verified and bans are checked before this event is raised,
        /// but before the player is registered with the server.
        /// Player's state at this point is SessionState.Connecting. </summary>
        public static event EventHandler<PlayerConnectingEventArgs> Connecting {
            add { ConnectingEvent.Add( value, Priority.Normal ); }
            remove { ConnectingEvent.Remove( value ); }
        }
        public static void ConnectingPriority( [NotNull] EventHandler<PlayerConnectingEventArgs> callback, Priority priority ) {
            if( callback == null ) throw new ArgumentNullException( "callback" );
            ConnectingEvent.Add( callback, priority );
        }
        static readonly PriorityEvent<PlayerConnectingEventArgs> ConnectingEvent = new PriorityEvent<PlayerConnectingEventArgs>();


        /// <summary> Occurs when a player has connected, but before the player has joined any world.
        /// Allows changing the player's starting world.
        /// Player's state at this point is SessionState.Connecting, and about to change to SessionState.LoadingMain </summary>
        public static event EventHandler<PlayerConnectedEventArgs> Connected {
            add { ConnectedEvent.Add( value, Priority.Normal ); }
            remove { ConnectedEvent.Remove( value ); }
        }
        public static void ConnectedPriority( [NotNull] EventHandler<PlayerConnectedEventArgs> callback, Priority priority ) {
            if( callback == null ) throw new ArgumentNullException( "callback" );
            ConnectedEvent.Add( callback, priority );
        }
        static readonly PriorityEvent<PlayerConnectedEventArgs> ConnectedEvent = new PriorityEvent<PlayerConnectedEventArgs>();


        /// <summary> Occurs after a player has connected and joined the starting world. 
        /// Player's state at this point has just changed from SessionState.LoadingMain to SessionState.Online </summary>
        public static event EventHandler<PlayerEventArgs> Ready {
            add { ReadyEvent.Add( value, Priority.Normal ); }
            remove { ReadyEvent.Remove( value ); }
        }
        public static void ReadyPriority( [NotNull] EventHandler<PlayerEventArgs> callback, Priority priority ) {
            if( callback == null ) throw new ArgumentNullException( "callback" );
            ReadyEvent.Add( callback, priority );
        }
        static readonly PriorityEvent<PlayerEventArgs> ReadyEvent = new PriorityEvent<PlayerEventArgs>();


        /// <summary> Occurs when player is about to move (cancellable). </summary>
        public static event EventHandler<PlayerMovingEventArgs> Moving {
            add { MovingEvent.Add( value, Priority.Normal ); }
            remove { MovingEvent.Remove( value ); }
        }
        public static void MovingPriority( [NotNull] EventHandler<PlayerMovingEventArgs> callback, Priority priority ) {
            if( callback == null ) throw new ArgumentNullException( "callback" );
            MovingEvent.Add( callback, priority );
        }
        static readonly PriorityEvent<PlayerMovingEventArgs> MovingEvent = new PriorityEvent<PlayerMovingEventArgs>();


        /// <summary> Occurs when player has moved. </summary>
        public static event EventHandler<PlayerMovedEventArgs> Moved {
            add { MovedEvent.Add( value, Priority.Normal ); }
            remove { MovedEvent.Remove( value ); }
        }
        public static void MovedPriority( [NotNull] EventHandler<PlayerMovedEventArgs> callback, Priority priority ) {
            if( callback == null ) throw new ArgumentNullException( "callback" );
            MovedEvent.Add( callback, priority );
        }
        static readonly PriorityEvent<PlayerMovedEventArgs> MovedEvent = new PriorityEvent<PlayerMovedEventArgs>();


        /// <summary> Occurs when player clicked a block (cancellable).
        /// Note that a click will not necessarily result in a block being placed or deleted. </summary>
        public static event EventHandler<PlayerClickingEventArgs> Clicking {
            add { ClickingEvent.Add( value, Priority.Normal ); }
            remove { ClickingEvent.Remove( value ); }
        }
        public static void ClickingPriority( [NotNull] EventHandler<PlayerClickingEventArgs> callback, Priority priority ) {
            if( callback == null ) throw new ArgumentNullException( "callback" );
            ClickingEvent.Add( callback, priority );
        }
        static readonly PriorityEvent<PlayerClickingEventArgs> ClickingEvent = new PriorityEvent<PlayerClickingEventArgs>();


        /// <summary> Occurs after a player has clicked a block.
        /// Note that a click will not necessarily result in a block being placed or deleted. </summary>
        public static event EventHandler<PlayerClickedEventArgs> Clicked {
            add { ClickedEvent.Add( value, Priority.Normal ); }
            remove { ClickedEvent.Remove( value ); }
        }
        public static void ClickedPriority( [NotNull] EventHandler<PlayerClickedEventArgs> callback, Priority priority ) {
            if( callback == null ) throw new ArgumentNullException( "callback" );
            ClickedEvent.Add( callback, priority );
        }
        static readonly PriorityEvent<PlayerClickedEventArgs> ClickedEvent = new PriorityEvent<PlayerClickedEventArgs>();


        /// <summary> Occurs when a player is about to place a block.
        /// Permission checks are done before calling this event, and their result may be overridden. </summary>
        public static event EventHandler<PlayerPlacingBlockEventArgs> PlacingBlock {
            add { PlacingBlockEvent.Add( value, Priority.Normal ); }
            remove { PlacingBlockEvent.Remove( value ); }
        }
        public static void PlacingBlockPriority( [NotNull] EventHandler<PlayerPlacingBlockEventArgs> callback, Priority priority ) {
            if( callback == null ) throw new ArgumentNullException( "callback" );
            PlacingBlockEvent.Add( callback, priority );
        }
        static readonly PriorityEvent<PlayerPlacingBlockEventArgs> PlacingBlockEvent = new PriorityEvent<PlayerPlacingBlockEventArgs>();


        /// <summary>  Occurs when a player has placed a block.
        /// This event does not occur if the block placement was disallowed. </summary>
        public static event EventHandler<PlayerPlacedBlockEventArgs> PlacedBlock {
            add { PlacedBlockEvent.Add( value, Priority.Normal ); }
            remove { PlacedBlockEvent.Remove( value ); }
        }
        public static void PlacedBlockPriority( [NotNull] EventHandler<PlayerPlacedBlockEventArgs> callback, Priority priority ) {
            if( callback == null ) throw new ArgumentNullException( "callback" );
            PlacedBlockEvent.Add( callback, priority );
        }
        static readonly PriorityEvent<PlayerPlacedBlockEventArgs> PlacedBlockEvent = new PriorityEvent<PlayerPlacedBlockEventArgs>();


        /// <summary> Occurs before a player is kicked (cancellable). 
        /// Kick may be caused by /Kick, /Ban, /BanIP, or /BanAll commands, or by idling.
        /// Callbacks may override whether the kick will be announced or recorded in PlayerDB. </summary>
        public static event EventHandler<PlayerBeingKickedEventArgs> BeingKicked {
            add { BeingKickedEvent.Add( value, Priority.Normal ); }
            remove { BeingKickedEvent.Remove( value ); }
        }
        public static void BeingKickedPriority( [NotNull] EventHandler<PlayerBeingKickedEventArgs> callback, Priority priority ) {
            if( callback == null ) throw new ArgumentNullException( "callback" );
            BeingKickedEvent.Add( callback, priority );
        }
        static readonly PriorityEvent<PlayerBeingKickedEventArgs> BeingKickedEvent = new PriorityEvent<PlayerBeingKickedEventArgs>();


        /// <summary> Occurs after a player has been kicked. Specifically, it happens after
        /// kick has been announced and recorded to PlayerDB (if applicable), just before the
        /// target player disconnects.
        /// Kick may be caused by /Kick, /Ban, /BanIP, or /BanAll commands, or by idling. </summary>
        public static event EventHandler<PlayerKickedEventArgs> Kicked {
            add { KickedEvent.Add( value, Priority.Normal ); }
            remove { KickedEvent.Remove( value ); }
        }
        public static void KickedPriority( [NotNull] EventHandler<PlayerKickedEventArgs> callback, Priority priority ) {
            if( callback == null ) throw new ArgumentNullException( "callback" );
            KickedEvent.Add( callback, priority );
        }
        static readonly PriorityEvent<PlayerKickedEventArgs> KickedEvent = new PriorityEvent<PlayerKickedEventArgs>();


        /// <summary> Happens after a player has hidden or unhidden. </summary>
        public static event EventHandler<PlayerEventArgs> HideChanged;


        /// <summary> Occurs when a player disconnects. </summary>
        public static event EventHandler<PlayerDisconnectedEventArgs> Disconnected;


        /// <summary> Occurs when a player intends to join a world (cancellable). </summary>
        public static event EventHandler<PlayerJoiningWorldEventArgs> JoiningWorld;


        /// <summary> Occurs after a player has joined a world. </summary>
        public static event EventHandler<PlayerJoinedWorldEventArgs> JoinedWorld;


        static bool RaisePlayerConnectingEvent( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            var e = new PlayerConnectingEventArgs( player );
            ConnectingEvent.Raise( e );
            return !e.Cancel;
        }


        internal static World RaisePlayerConnectedEvent( [NotNull] Player player, World world, bool isFake ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            var e = new PlayerConnectedEventArgs( player, world, isFake );
            ConnectedEvent.Raise( e );
            return e.StartingWorld;
        }


        static void RaisePlayerReadyEvent( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            ReadyEvent.Raise( new PlayerEventArgs( player ) );
        }


        static bool RaisePlayerMovingEvent( [NotNull] Player player, Position newPos ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            var e = new PlayerMovingEventArgs( player, newPos );
            MovingEvent.Raise( e );
            return !e.Cancel;
        }


        static void RaisePlayerMovedEvent( [NotNull] Player player, Position oldPos ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            MovedEvent.Raise( new PlayerMovedEventArgs( player, oldPos ) );
        }


        internal static void RaisePlayerPlacedBlockEvent( Player player, Map map, Vector3I coords,
                                                          Block oldBlock, Block newBlock, BlockChangeContext context ) {
            var e = new PlayerPlacedBlockEventArgs( player, map, coords, oldBlock, newBlock, context );
            PlacedBlockEvent.Raise( e );
        }


        internal static void RaisePlayerHideChangedEvent( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            var handler = HideChanged;
            if( handler != null ) handler( null, new PlayerEventArgs( player ) );
        }


        static void RaisePlayerDisconnectedEvent( [NotNull] Player player, LeaveReason leaveReason ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            var handler = Disconnected;
            if( handler != null ) handler( null, new PlayerDisconnectedEventArgs( player, leaveReason, false ) );
        }


        static bool RaisePlayerJoiningWorldEvent( [NotNull] Player player, [NotNull] World newWorld, WorldChangeContext context,
                                                  string textLine1, string textLine2 ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( newWorld == null ) throw new ArgumentNullException( "newWorld" );
            var handler = JoiningWorld;
            if( handler == null ) return true;
            var e = new PlayerJoiningWorldEventArgs( player, player.World, newWorld, context, textLine1, textLine2 );
            handler( null, e );
            return !e.Cancel;
        }


        static void RaisePlayerJoinedWorldEvent( Player player, World oldWorld, WorldChangeContext context ) {
            var handler = JoinedWorld;
            if( handler != null ) handler( null, new PlayerJoinedWorldEventArgs( player, oldWorld, player.World, context ) );
        }
    }
}

namespace fCraft.Events {
    /// <summary> An EventArgs for an event that directly relates to a particular player. </summary>
    public interface IPlayerEvent {
        Player Player { get; }
    }


    /// <summary> Provides basic data for player-related events. </summary>
    public sealed class PlayerEventArgs : EventArgs, IPlayerEvent {
        internal PlayerEventArgs( [NotNull] Player player ) {
            Player = player;
        }

        /// <summary> Name of the player who has initiated or is affected by the event. </summary>
        [NotNull]
        public Player Player { get; private set; }
    }


    /// <summary> Provides data for Server.SessionConnecting event. Cancellable. </summary>
    public sealed class SessionConnectingEventArgs : EventArgs, ICancelableEvent {
        internal SessionConnectingEventArgs( [NotNull] IPAddress ip ) {
            if( ip == null ) throw new ArgumentNullException( "ip" );
            IP = ip;
        }

        /// <summary> IP Address of the connecting player. </summary>
        [NotNull]
        public IPAddress IP { get; private set; }

        /// <summary> If set to 'true', rejects connection and kicks the player. </summary>
        public bool Cancel { get; set; }
    }


    /// <summary> Provides data for Server.SessionDisconnected event. Immutable. </summary>
    public sealed class SessionDisconnectedEventArgs : EventArgs, IPlayerEvent {
        internal SessionDisconnectedEventArgs( [NotNull] Player player, LeaveReason leaveReason ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            Player = player;
            LeaveReason = leaveReason;
        }

        /// <summary> Player who disconnected. </summary>
        [NotNull]
        public Player Player { get; private set; }

        /// <summary> Reason for leaving the server. </summary>
        public LeaveReason LeaveReason { get; private set; }
    }


    /// <summary> Provides data for Player.Connecting event. Cancellable. </summary>
    public sealed class PlayerConnectingEventArgs : EventArgs, IPlayerEvent, ICancelableEvent {
        internal PlayerConnectingEventArgs( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            Player = player;
        }

        /// <summary> Player who is connecting. </summary>
        [NotNull]
        public Player Player { get; private set; }

        /// <summary> If set to 'true', rejects player's connection and kicks them.
        /// Use Player.Kick() in conjunction with .Cancel to set the kick message. </summary>
        public bool Cancel { get; set; }
    }


    /// <summary> Provides data for Player.Connected event. StartingWorld property may be changed.
    /// Make sure to check IsFake property. </summary>
    public sealed class PlayerConnectedEventArgs : EventArgs, IPlayerEvent {
        internal PlayerConnectedEventArgs( [NotNull] Player player, World startingWorld, bool isFake ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            Player = player;
            StartingWorld = startingWorld;
            IsFake = isFake;
        }

        /// <summary> Player who just connected, and is about to join main. </summary>
        [NotNull]
        public Player Player { get; private set; }

        /// <summary> Player's main world.
        /// May be WorldManager.MainWorld or rank-specific main. Can be changed. </summary>
        public World StartingWorld { get; set; }

        /// <summary> Whether the player has *actually* connected, or if this connect is faked by /Hide. </summary>
        public bool IsFake { get; private set; }
    }


    /// <summary> Provides data for Player.Moving event. Cancellable. </summary>
    public sealed class PlayerMovingEventArgs : EventArgs, IPlayerEvent, ICancelableEvent {
        internal PlayerMovingEventArgs( [NotNull] Player player, Position newPos ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            Player = player;
            OldPosition = player.Position;
            NewPosition = newPos;
        }

        /// <summary> Player intends to move. </summary>
        [NotNull]
        public Player Player { get; private set; }

        /// <summary> Player's current position. </summary>
        public Position OldPosition { get; private set; }

        /// <summary> Desired new position. </summary>
        public Position NewPosition { get; private set; }

        public bool Cancel { get; set; }
    }


    /// <summary> Provides data for Player.Moved event. Immutable. </summary>
    public sealed class PlayerMovedEventArgs : EventArgs, IPlayerEvent {
        internal PlayerMovedEventArgs( [NotNull] Player player, Position oldPos ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            Player = player;
            OldPosition = oldPos;
            NewPosition = player.Position;
        }

        /// <summary> Player who has just moved. </summary>
        [NotNull]
        public Player Player { get; private set; }

        /// <summary> Player's previous position. </summary>
        public Position OldPosition { get; private set; }

        /// <summary> Player's new position. </summary>
        public Position NewPosition { get; private set; }
    }


    /// <summary> Provides data for Player.Clicking event. Cancellable.
    /// Coords, Block, and Action properties may be changed. </summary>
    public sealed class PlayerClickingEventArgs : EventArgs, IPlayerEvent, ICancelableEvent {
        internal PlayerClickingEventArgs( [NotNull] Player player, Vector3I coords,
                                          ClickAction action, Block block ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            Player = player;
            Coords = coords;
            Action = action;
            Block = block;
        }

        /// <summary> Player who is attempting to click. </summary>
        [NotNull]
        public Player Player { get; private set; }

        /// <summary> Click coordinates, in terms of blocks. Must be within map bounds.
        /// Can be changed. </summary>
        public Vector3I Coords { get; set; }

        /// <summary> Block type that the player is currently holding.
        /// Can be changed. </summary>
        public Block Block { get; set; }

        /// <summary> Whether the player is building a block (right-click) or deleting it (left-click).
        /// Can be changed. </summary>
        public ClickAction Action { get; set; }

        public bool Cancel { get; set; }
    }


    /// <summary> Provides data for Player.Clicked event. Immutable. </summary>
    public sealed class PlayerClickedEventArgs : EventArgs, IPlayerEvent {
        internal PlayerClickedEventArgs( [NotNull] Player player, Vector3I coords, ClickAction action, Block block ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            Player = player;
            Coords = coords;
            Block = block;
            Action = action;
        }

        /// <summary> Player who has just clicked. </summary>
        [NotNull]
        public Player Player { get; private set; }

        /// <summary> Click coordinates, in terms of blocks. </summary>
        public Vector3I Coords { get; private set; }

        /// <summary> Block type that the player is currently holding. </summary>
        public Block Block { get; private set; }

        /// <summary> Whether the player is building a block (right-click) or deleting it (left-click). </summary>
        public ClickAction Action { get; private set; }
    }


    /// <summary> Provides data for Player.PlacedBlock event. Immutable. </summary>
    public sealed class PlayerPlacedBlockEventArgs : EventArgs, IPlayerEvent {
        internal PlayerPlacedBlockEventArgs( [NotNull] Player player, [NotNull] Map map, Vector3I coords,
                                             Block oldBlock, Block newBlock, BlockChangeContext context ) {
            if( map == null ) throw new ArgumentNullException( "map" );
            Player = player;
            Map = map;
            Coords = coords;
            OldBlock = oldBlock;
            NewBlock = newBlock;
            Context = context;
        }


        /// <summary> Player who has just placed a block. </summary>
        [NotNull]
        public Player Player { get; private set; }

        /// <summary> Map on which the block was placed. </summary>
        [NotNull]
        public Map Map { get; private set; }

        /// <summary> Coordinates at which the block was placed. </summary>
        public Vector3I Coords { get; private set; }

        /// <summary> Previous blocktype at this coordinate. </summary>
        public Block OldBlock { get; private set; }

        /// <summary> Current (placed) blocktype at this location. </summary>
        public Block NewBlock { get; private set; }

        /// <summary> Context in which the block was placed. </summary>
        public BlockChangeContext Context { get; private set; }
    }


    /// <summary> Provides data for Player.PlacingBlock event. Result may be overridden. </summary>
    public sealed class PlayerPlacingBlockEventArgs : EventArgs, IPlayerEvent {
        internal PlayerPlacingBlockEventArgs( [NotNull] Player player, [NotNull] Map map, Vector3I coords,
                                              Block oldBlock, Block newBlock, BlockChangeContext context,
                                              CanPlaceResult result ) {
            if( map == null ) throw new ArgumentNullException( "map" );
            Player = player;
            Map = map;
            Coords = coords;
            OldBlock = oldBlock;
            NewBlock = newBlock;
            Context = context;
            Result = result;
        }

        /// <summary> Player who intends to place a block. </summary>
        [NotNull]
        public Player Player { get; private set; }

        /// <summary> Map on which the block would be placed. </summary>
        [NotNull]
        public Map Map { get; private set; }

        /// <summary> Coordinates at which the block would be placed. </summary>
        public Vector3I Coords { get; private set; }

        /// <summary> Current blocktype at this coordinate. </summary>
        public Block OldBlock { get; private set; }

        /// <summary> Blocktype that the player intends to place. </summary>
        public Block NewBlock { get; private set; }

        /// <summary> Context in which the block was placed. </summary>
        public BlockChangeContext Context { get; private set; }

        /// <summary> Result of Player.CanPlace permission check. Can be changed. </summary>
        public CanPlaceResult Result { get; set; }
    }


    /// <summary> Provides data for Player.BeingKicked event. Cancellable. </summary>
    public sealed class PlayerBeingKickedEventArgs : EventArgs, IPlayerEvent, ICancelableEvent {
        internal PlayerBeingKickedEventArgs( [NotNull] Player player, [NotNull] Player kicker, [CanBeNull] string reason,
                                              bool announce, bool recordToPlayerDB, LeaveReason context ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( kicker == null ) throw new ArgumentNullException( "kicker" );
            Player = player;
            Kicker = kicker;
            Reason = reason;
            Announce = announce;
            RecordToPlayerDB = recordToPlayerDB;
            Context = context;
        }

        /// <summary> Player who is being kicked (target). </summary>
        [NotNull]
        public Player Player { get; private set; }

        /// <summary> Player who is kicking. </summary>
        [NotNull]
        public Player Kicker { get; private set; }

        /// <summary> Given kick reason. May be null. Can be changed. </summary>
        [CanBeNull]
        public string Reason { get; set; }

        /// <summary> Whether the kick should be announced in-game and on IRC. Can be changed. </summary>
        public bool Announce { get; set; }

        /// <summary> Whether kick should be added to the target's record. Can be changed. </summary>
        public bool RecordToPlayerDB { get; set; }

        /// <summary> Circumstances that resulted in a kick (e.g. Kick, Ban, BanIP, IdleKick, etc). </summary>
        public LeaveReason Context { get; private set; }

        public bool Cancel { get; set; }
    }


    /// <summary> Provides data for Player.Kicked event. Immutable. </summary>
    public sealed class PlayerKickedEventArgs : EventArgs, IPlayerEvent {
        internal PlayerKickedEventArgs( [NotNull] Player player, [NotNull] Player kicker, [CanBeNull] string reason,
                                        bool announce, bool recordToPlayerDB, LeaveReason context ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( kicker == null ) throw new ArgumentNullException( "kicker" );
            Player = player;
            Kicker = kicker;
            Reason = reason;
            Announce = announce;
            RecordToPlayerDB = recordToPlayerDB;
            Context = context;
        }

        /// <summary> Player who has just been kicked (target). </summary>
        [NotNull]
        public Player Player { get; private set; }

        /// <summary> Player who kicked. </summary>
        [NotNull]
        public Player Kicker { get; private set; }

        /// <summary> Given kick reason (may have been blank). </summary>
        [CanBeNull]
        public string Reason { get; private set; }

        /// <summary> Whether the kick was announced in-game and on IRC. </summary>
        public bool Announce { get; private set; }

        /// <summary> Whether kick was added to the target's record. </summary>
        public bool RecordToPlayerDB { get; private set; }

        /// <summary> Circumstances that resulted in a kick (e.g. Kick, Ban, BanIP, IdleKick, etc). </summary>
        public LeaveReason Context { get; private set; }
    }


    /// <summary> Provides data for Player.Disconnected event. Immutable.
    /// Make sure to check IsFake property. </summary>
    public sealed class PlayerDisconnectedEventArgs : EventArgs, IPlayerEvent {
        internal PlayerDisconnectedEventArgs( [NotNull] Player player, LeaveReason leaveReason, bool isFake ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            Player = player;
            LeaveReason = leaveReason;
            IsFake = isFake;
        }

        /// <summary> Player who has just disconnected. </summary>
        [NotNull]
        public Player Player { get; private set; }

        /// <summary> Reason for leaving the server. </summary>
        public LeaveReason LeaveReason { get; private set; }

        /// <summary> Whether the player has *actually* disconnected, or if this disconnect is faked by /Hide. </summary>
        public bool IsFake { get; private set; }
    }


    /// <summary> Provides data for Player.JoiningWorld event. Cancellable.
    /// Allows overriding the text that is shown on connection screen. </summary>
    public sealed class PlayerJoiningWorldEventArgs : EventArgs, IPlayerEvent, ICancelableEvent {
        internal PlayerJoiningWorldEventArgs( [NotNull] Player player, [CanBeNull] World oldWorld,
                                              [NotNull] World newWorld, WorldChangeContext context,
                                              string textLine1, string textLine2 ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( newWorld == null ) throw new ArgumentNullException( "newWorld" );
            Player = player;
            OldWorld = oldWorld;
            NewWorld = newWorld;
            Context = context;
            TextLine1 = textLine1;
            TextLine2 = textLine2;
        }

        /// <summary> Player who intends to join a world. </summary>
        [NotNull]
        public Player Player { get; private set; }

        /// <summary> Player's current world. May be null if player just connected, and is joining main. </summary>
        [CanBeNull]
        public World OldWorld { get; private set; }

        /// <summary> The world that player intends to join. May be same as OldWorld, if rejoining. </summary>
        [NotNull]
        public World NewWorld { get; private set; }

        /// <summary> Context of the world change. </summary>
        public WorldChangeContext Context { get; private set; }

        /// <summary> First line of text that is shown to the player on the loading screen.
        /// Defaults to server name. May be changed. </summary>
        public string TextLine1 { get; set; } // TODO

        /// <summary> First line of text that is shown to the player on the loading screen.
        /// Defaults to world name or WoM cfg string. May be changed. </summary>
        public string TextLine2 { get; set; } // TODO

        public bool Cancel { get; set; }
    }


    /// <summary> Provides data for Player.JoinedWorld event. Immutable. </summary>
    public sealed class PlayerJoinedWorldEventArgs : EventArgs, IPlayerEvent {
        internal PlayerJoinedWorldEventArgs( [NotNull] Player player, [CanBeNull] World oldWorld, [NotNull] World newWorld, WorldChangeContext context ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            Player = player;
            OldWorld = oldWorld;
            NewWorld = newWorld;
            Context = context;
        }

        /// <summary> Player who has just joined a world. </summary>
        [NotNull]
        public Player Player { get; private set; }

        /// <summary> Players' previous world. May be null if player just connected, and is joining main. </summary>
        [CanBeNull]
        public World OldWorld { get; private set; }

        /// <summary> Player's current (newly-joined) world. May be same as OldWorld, if rejoining. </summary>
        [NotNull]
        public World NewWorld { get; private set; }

        /// <summary> Context of the world change. </summary>
        public WorldChangeContext Context { get; private set; }
    }
}