﻿// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System.Diagnostics;

namespace fCraft {
    /// <summary>
    /// Enumeration of available configuration keys. See comment
    /// at the top of Config.cs for a history of changes.
    /// </summary>
    public enum ConfigKey {
        #region General

        [StringKey( ConfigSection.General, "Custom Minecraft Server (fCraft)",
@"The name of the server, as shown on the welcome screen and the
official server list (if server is public).",
            MinLength = 1, MaxLength = 64 )]
        ServerName,


        [StringKey( ConfigSection.General, "Welcome to the server!",
@"MOTD (Message Of The Day) is a message shown to connecting players 
right under the server name. It may be left blank.",
            MinLength = 0, MaxLength = 64 )]
        MOTD,


        [IntKey( ConfigSection.General, 20,
@"Maximum number of players on the server. Having more players
uses more RAM and more bandwidth. If a player's rank is given a
""reserved slot"" on the server, they can join even if server is full.",
            MinValue = 1, MaxValue = 1000 )]
        MaxPlayers,


        [IntKey( ConfigSection.General, 20,
@"Maximum number of players allowed to be on the same world at the same time.
Minecraft protocol limits total number of players per world to 128.
Note that having more people on a world increases everyone's bandwidth use.",
            MinValue = 1, MaxValue = 128 )]
        MaxPlayersPerWorld,


        [RankKey( ConfigSection.General, RankKeyAttribute.BlankValueMeaning.LowestRank, 
@"New players will be assigned this rank by default.
It's generally a good idea not to give new players
many powers until they prove themselves trustworthy." )]
        DefaultRank,


        [BoolKey( ConfigSection.General, false,
            @"Public servers are listed on minecraft.net server list, so expect
random players to join. Private servers can only be joined by players
who already know the server port/address or URL. Note that the URL
changes if your computer's IP or server's port change." )]
        IsPublic,


        [IntKey( ConfigSection.General, 25565,
@"Port number on your local machine that fCraft uses to listen for
incoming connections. If you are behind a router, you may need
to set up port forwarding. You may also need to add a firewall 
exception for fCraftUI/fCraftConsole/ConfigTool. Note that your
server's URL will change if you change the port number.",
            MinValue = 1, MaxValue = 65535 )]
        Port,

        [IntKey( ConfigSection.General, 100, 
@"Total available upload bandwidth, in kilobytes. This number
is used to pace drawing commands to prevent server from
overwhelming the Internet connection with data.",
            MinValue = 1, MaxValue = short.MaxValue )]
        UploadBandwidth,

        #endregion


        #region Chat

        [BoolKey( ConfigSection.Chat, true,
@"Color player names in chat and in-game based on their rank." )]
        RankColorsInChat,

        [BoolKey( ConfigSection.Chat, true,
@"Color world names in chat based on their build and access permissions.")]
        RankColorsInWorldNames,

        [BoolKey( ConfigSection.Chat, false,
@"Show 1-character prefixes in chat before player names. This can be
used to set up IRC-style ""+"" and ""@"" prefixes for ops." )]
        RankPrefixesInChat,

        [BoolKey( ConfigSection.Chat, false,
@"Show prefixes in the player list. As a side-effect, Minecraft client
will not show custom skins for players with prefixed names.")]
        RankPrefixesInList,

        [BoolKey( ConfigSection.Chat, true,
@"Announce players joining or leaving the server in chat.")]
        ShowConnectionMessages,

        [BoolKey( ConfigSection.Chat, true,
@"Announce IP-banned players trying to connect to the server.")]
        ShowBannedConnectionMessages,

        [BoolKey( ConfigSection.Chat, true,
@"Show messages when players change worlds.")]
        ShowJoinedWorldMessages,

        [ColorKey( ConfigSection.Chat, Color.SysDefault,
@"Color of normal system messages.")]
        SystemMessageColor,

        [ColorKey( ConfigSection.Chat, Color.HelpDefault,
@"Color of command usage examples in help.")]
        HelpColor,

        [ColorKey( ConfigSection.Chat, Color.SayDefault,
@"Color of messages produced by ""/say"" command.")]
        SayColor,

        [ColorKey( ConfigSection.Chat, Color.AnnouncementDefault,
@"Color of announcements and rules. Default is dark-green.
Note that this default color can be overriden by
colorcodes in announcement and rule files." )]
        AnnouncementColor,

        [ColorKey( ConfigSection.Chat, Color.PMDefault,
@"Color of private and rank-wide messages." )]
        PrivateMessageColor,

        [ColorKey( ConfigSection.Chat, Color.MeDefault,
@"Color of ""/me"" command messages.")]
        MeColor,

        [ColorKey( ConfigSection.Chat, Color.WarningDefault,
@"Color of error and warning messages.")]
        WarningColor,

        [IntKey( ConfigSection.Chat, 0,
@"Announcement interval, in minutes. Set to 0 to disable announcements.
Announcements are shown to all players, one line at a time, in random order.",
            MinValue=0)]
        AnnouncementInterval,

        #endregion


        #region Worlds

        [RankKey( ConfigSection.Worlds,RankKeyAttribute.BlankValueMeaning.DefaultRank,
@"When new maps are loaded with the /wload command,
the build permission for new maps will default to this rank." )]
        DefaultBuildRank,

        [StringKey( ConfigSection.Worlds, "maps",
@"Custom path for storing map files. If you change this value,
make sure to move the map files before starting the server again.")]
        MapPath,

        #endregion


        #region Security

        [EnumKey( ConfigSection.Security, NameVerificationMode.Balanced,
@"Name verification ensures that connecting players are not impersonating
someone else. Strict verification uses only the main verification method.
Sometimes it can produce false negatives - for example if server has just
restarted, or if minecraft.net heartbeats are timing out. Balanced verification
checks player's current and on-record IP address to eliminate false negatives." )]
        VerifyNames,

        [IntKey( ConfigSection.Security, 0,
@"Restricts the number of connections allowed from any one IP address.
Note that all players on the same LAN will share an IP, and may be prevented
from joining together. Enabling this option is not recommended unless there
is a specific need/threat.",
            MinValue=0)]
        MaxConnectionsPerIP,

        [BoolKey( ConfigSection.Security, false,
@"Allow players from your local network (LAN) to connect without name verification.
May be useful if minecraft.net is blocked on your LAN for some reason.
Warning: Unverified players can log in with ANY name - even as you!" )]
        AllowUnverifiedLAN,

        [RankKey( ConfigSection.Security, RankKeyAttribute.BlankValueMeaning.DefaultRank,
@"When players use the /patrol command, they will be  teleported
to players of this (or lower) rank. ""Patrolling"" means teleporting
to other players to check on them, usually while hidden." )]
        PatrolledRank,

        [IntKey( ConfigSection.Security, 4,
@"Number of messages that a player needs to type to trigger the antispam warning.
Set this to 0 to disable antispam.",
            AlwaysAllowZero = true, MinValue = 2, MaxValue = 64 )]
        AntispamMessageCount,

        [IntKey( ConfigSection.Security, 5,
@"Number of seconds over which the player needs to type messages to trigger antispam warning.
Set this to 0 to disable antispam.",
            AlwaysAllowZero = true, MinValue = 1, MaxValue = 64 )]
        AntispamInterval,

        [IntKey( ConfigSection.Security, 5,
@"Duration of automatic mute if Antispam is triggered, in seconds.
Set this to 0 to disable automatic mute (and only leave the warning).",
            MinValue = 0, MaxValue = 86400 )]
        AntispamMuteDuration,

        [IntKey( ConfigSection.Security, 2,
@"Number of warnings given to a player (number of times antispam is triggered)
before the player is muted. Set this to 0 to disable automatic kicks.",
            AlwaysAllowZero=true, MinValue = 0, MaxValue = 64 )]
        AntispamMaxWarnings,


        [BoolKey( ConfigSection.Security, false,
@"Only allow players who have a paid Minecraft account (not recommended).
This will help filter out griefers with throwaway accounts,
but will also prevent many legitimate players from joining." )]
        PaidPlayersOnly,


        [BoolKey( ConfigSection.Security, false,
@"Require players to specify a reason/memo when banning or unbanning someone." )]
        RequireBanReason,

        [BoolKey( ConfigSection.Security, false,
@"Require players to specify a reason/memo when kicking someone.")]
        RequireKickReason,

        [BoolKey( ConfigSection.Security, false,
@"Require players to specify a reason/memo when promoting or demoting someone." )]
        RequireRankChangeReason,


        [BoolKey( ConfigSection.Security, true,
@"Announce the reason/memo in chat when someone gets kicked/banned/unbanned." )]
        AnnounceKickAndBanReasons,

        [BoolKey( ConfigSection.Security, true,
@"Announce promotions and demotions in chat.")]
        AnnounceRankChanges,

        [BoolKey( ConfigSection.Security, true,
@"Announce the reason/memo in chat when someone gets promoted or demoted." )]
        AnnounceRankChangeReasons,

        #endregion


        #region Saving and Backup

        [IntKey( ConfigSection.SavingAndBackup, 90,
@"Whether to save maps (if modified) automatically once in a while.
If disabled, maps are only saved when a world is unloaded or server is shut down.
A higher setting (120+ seconds) is recommended for busy servers with many maps.",
            AlwaysAllowZero = true, MinValue = 10 )]
        SaveInterval,

        [BoolKey( ConfigSection.SavingAndBackup, true,
@"Create a backup of every map when the server starts." )]
        BackupOnStartup,

        [BoolKey( ConfigSection.SavingAndBackup, false,
@"Create backups any time a player joins a map.
Both a timestamp and player's name are included in the filename." )]
        BackupOnJoin,

        [BoolKey( ConfigSection.SavingAndBackup, true,
@"Only create backups if the map was modified since last backup." )]
        BackupOnlyWhenChanged,

        [IntKey( ConfigSection.SavingAndBackup, 20,
@"Create backups of loaded maps automatically once in a while.
A world is considered ""loaded"" if there is at least one player on it.",
            MinValue = 0 )]
        BackupInterval,

        [IntKey( ConfigSection.SavingAndBackup, 0,
@"Maximum number of backup files that fCraft should keep.
If exceeded, oldest backups will be deleted first." )]
        MaxBackups,

        [IntKey( ConfigSection.SavingAndBackup, 0,
@"Maximum combined filesize of all backups.
If exceeded, oldest backups will be deleted first." )]
        MaxBackupSize, // in megabytes

        #endregion


        #region Logging

        [EnumKey( ConfigSection.Logging, LogSplittingType.OneFile,
@"The way log files are organized.")]
        LogMode,

        [IntKey( ConfigSection.Logging, 0,
@"Maximum number of log files to keep.
If exceeded, oldest logs will be erased first. Set this to 0 to keep all logs.")]
        MaxLogs,

        #endregion


        #region IRC

        [BoolKey( ConfigSection.IRC, false,
@"fCraft includes an IRC (Internet Relay Chat) client for
relaying messages to and from any IRC network.
Note that encrypted IRC (via SSL) is not supported." )]
        IRCBotEnabled,

        [StringKey( ConfigSection.IRC, "MinecraftBot",
@"IRC bot's nickname. If the nickname is taken, fCraft will append
an underscore (_) to the name and retry.",
            MinLength = 1, MaxLength = 32 )]
        IRCBotNick,

        [StringKey( ConfigSection.IRC, "irc.esper.net",
@"Host or address of the IRC network." )]
        IRCBotNetwork,

        [IntKey( ConfigSection.IRC, 6667,
@"Port number of the IRC network. Most networks use port 6667.",
            MinValue = 1, MaxValue = 65535 )]
        IRCBotPort,

        [StringKey( ConfigSection.IRC, "#changeme",
@"Comma-separated list of channels to join. Channel names should include the hash (#).
One some IRC networks, channel names are case-sensitive." )]
        IRCBotChannels,

        [BoolKey( ConfigSection.IRC, false,
@"If checked, all chat messages on IRC are shown in the game.
Otherwise, only IRC messages starting with a hash (#) will be relayed." )]
        IRCBotForwardFromServer,

        [BoolKey( ConfigSection.IRC, false,
@"If checked, all chat messages from the server are shown on IRC.
Otherwise, only chat messages starting with a hash (#) will be relayed." )]
        IRCBotForwardFromIRC,

        [BoolKey( ConfigSection.IRC, false,
@"Show a message on IRC when someone joins of leaves the server." )]
        IRCBotAnnounceServerJoins,

        [BoolKey( ConfigSection.IRC, false,
@"Show a message in-gam,e when someone joins of leaves the IRC channel." )]
        IRCBotAnnounceIRCJoins,

        [BoolKey( ConfigSection.IRC, false,
@"Announce server events (kicks, bans, promotions, demotions) on IRC.")]
        IRCBotAnnounceServerEvents,

        [BoolKey( ConfigSection.IRC, false,
@"Check this if bot's nickname is registered
or requires identification/authentication." )]
        IRCRegisteredNick,

        [StringKey( ConfigSection.IRC, "NickServ",
@"Name of the registration service bot (usually NickServ).",
            MinLength = 1, MaxLength = 32 )]
        IRCNickServ,

        [StringKey( ConfigSection.IRC, "IDENTIFY passwordGoesHere",
@"Message to send to registration service bot." )]
        IRCNickServMessage,

        [ColorKey( ConfigSection.IRC, Color.IRCDefault,
@"Color of IRC messages, as seen on the server/in-game.")]
        IRCMessageColor,

        [IntKey( ConfigSection.IRC, 750,
@"Minimum delay (in milliseconds) between IRC messages. Many networks
have strict message rate limits, so a delay of at least 500ms is recommended.",
            MinValue = 0 )]
        IRCDelay,

        [IntKey( ConfigSection.IRC, 1,
@"Number of IRC bots to operate simultaneously.
Using multiple bots helps bypass message rate limits on some servers.
Note that some networks frown upon having multiple connections from one IP.
It is recommended to leave this at 1 unless you are having specific issues
with IRC bots falling behind on messages.",
            MinValue = 1 )]
        IRCThreads,

        [BoolKey( ConfigSection.IRC, true,
@"Whether the bots should use colors and formatting on IRC.")]
        IRCUseColor,

        #endregion


        #region Advanced

        [BoolKey( ConfigSection.Advanced, false,
@"When a player places or deletes a block, vanilla Minecraft server
relays the action back. This is not needed, and only wastes bandwidth." )]
        RelayAllBlockUpdates,

        [EnumKey( ConfigSection.Advanced, fCraft.UpdaterMode.Prompt,
@"fCraft can automatically update to latest stable versions.
If enabled, the update check is done on-startup.")]
        UpdaterMode,

        [StringKey( ConfigSection.Advanced, "",
@"Command to execute (via operating system's shell) before the update is applied.")]
        RunBeforeUpdate,

        [StringKey( ConfigSection.Advanced, "",
@"Command to execute (via operating system's shell) after the update is applied." )]
        RunAfterUpdate,

        [BoolKey( ConfigSection.Advanced, true,
@"It is recommended to save a backup of all data files before updating.
This setting allows the updater to do that for you.")]
        BackupBeforeUpdate,

        [BoolKey( ConfigSection.Advanced, false,
@"Minecraft protocol specifies 4 different movement packet types.
One of them sends absolute position, and other 3 send incremental relative positions.
You may use this option to disable the relative updates." )]
        NoPartialPositionUpdates,

        [EnumKey( ConfigSection.Advanced, ProcessPriorityClass.Normal,
@"It is recommended to leave fCraft at default priority.
Setting this below ""Normal"" may starve fCraft of resources.
Setting this above ""Normal"" may slow down other software on your machine." )]
        ProcessPriority,

        [IntKey( ConfigSection.Advanced, 2048,
@"The maximum number of block changes that can be sent to each client per second.
Unmodified Minecraft client can only handle about 2500 updates per second.
Setting this any higher may cause lag. Setting this lower will slow down
drawing commands (like cuboid).",
            MinValue = 10 )]
        BlockUpdateThrottling,

        [IntKey( ConfigSection.Advanced, 100,
@"The rate at which fCraft applies block updates, in milliseconds. Lowering this will slightly
reduce bandwidth and CPU use, but will add latency to block placement.",
            MinValue = 10, MaxValue = 10000 )]
        TickInterval,

        [BoolKey( ConfigSection.Advanced, false,
@"This mode reduces lag by up to 200ms, at the cost of vastly increased
bandwidth use. It's only practical if you have a very fast connection
with few players, or if your server is LAN-only." )]
        LowLatencyMode,

        [BoolKey( ConfigSection.Advanced, true,
@"Crash reports are created when serious unexpected errors occur.
Being able to receive crash reports helps identify bugs and improve fCraft!
The report consists of the error information, OS and runtime versions,
a copy of config.xml, and last 25 lines of the log file.
Reports are confidential and are not displayed publicly." )]
        SubmitCrashReports,

        [IntKey( ConfigSection.Advanced, 2000000,
@"The number of blocks that players can undo at a time.
Only the most-recent draw command can be undo, so the actual
limit also depends on rank draw limits. Saving undo information
takes up 16 bytes per block.",
            MinValue = 0 )]
        MaxUndo,

        [StringKey( ConfigSection.Advanced, "(console)",
@"Displayed name of the Console pseudoplayer. You may use any printable characters, and even colorcodes here.",
            MinLength = 1, MaxLength = 64 )]
        ConsoleName,

        [BoolKey( ConfigSection.Advanced, false,
@"Enable autorank (experimental, unsupported, use at your own risk)" )]
        AutoRankEnabled,

        [BoolKey( ConfigSection.Advanced, true,
@"Enable heartbeat to minecraft.net.
If disabled, heartbeat data is written to heartbeatdata.txt." )]
        HeartbeatEnabled,

        [IPKey( ConfigSection.Advanced, IPKeyAttribute.BlankValueMeaning.Any,
@"If the machine has more than one available IP address (for example
if you have more than one NIC) you can use this setting to make
fCraft bind to the same IP every time." )]
        IP,

        [EnumKey(ConfigSection.Advanced, fCraft.BandwidthUseMode.Normal,
@"Determines the bandwidth use mode.
High/Highest settings will reduce jitter of player movement, but increase bandwidth use.
Low/Lowest settings will introduce some popping in/out of players and increase jitter,
but will reduce bandwidth use.")]
        BandwidthUseMode,

        [IntKey(ConfigSection.Advanced, 0,
@"Automatically restarts the server after a given number of seconds.")]
        RestartInterval

        #endregion
    }
}