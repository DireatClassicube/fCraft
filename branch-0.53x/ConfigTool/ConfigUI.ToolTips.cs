﻿// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using fCraft;

namespace ConfigTool {
    partial class ConfigUI {

        void FillToolTipsGeneral() {

            toolTip.SetToolTip( lServerName, ConfigKey.ServerName.GetDescription() );
            toolTip.SetToolTip( tServerName, ConfigKey.ServerName.GetDescription() );

            toolTip.SetToolTip( lMOTD, ConfigKey.MOTD.GetDescription() );
            toolTip.SetToolTip( tMOTD, ConfigKey.MOTD.GetDescription() );

            toolTip.SetToolTip( lMaxPlayers, ConfigKey.MaxPlayers.GetDescription() );
            toolTip.SetToolTip( nMaxPlayers, ConfigKey.MaxPlayers.GetDescription() );

            toolTip.SetToolTip( lMaxPlayersPerWorld, ConfigKey.MaxPlayersPerWorld.GetDescription() );
            toolTip.SetToolTip( nMaxPlayersPerWorld, ConfigKey.MaxPlayersPerWorld.GetDescription() );

            toolTip.SetToolTip( lDefaultRank, ConfigKey.DefaultRank.GetDescription() );
            toolTip.SetToolTip( cDefaultRank, ConfigKey.DefaultRank.GetDescription() );

            toolTip.SetToolTip( lPublic, ConfigKey.IsPublic.GetDescription() );
            toolTip.SetToolTip( cPublic, ConfigKey.IsPublic.GetDescription() );

            toolTip.SetToolTip( nPort, ConfigKey.Port.GetDescription() );
            toolTip.SetToolTip( lPort, ConfigKey.Port.GetDescription() );

            toolTip.SetToolTip( bPortCheck,
@"Check if the selected port is connectible.
If port check fails, you may need to set up
port forwarding on your router." );

            toolTip.SetToolTip( nUploadBandwidth, ConfigKey.UploadBandwidth.GetDescription() );
            toolTip.SetToolTip( lUploadBandwidth, ConfigKey.UploadBandwidth.GetDescription() );

            toolTip.SetToolTip( bMeasure,
@"Test your connection's upload speed with speedtest.net
Note: to convert from megabits to kilobytes, multiply the
number by 128" );

            toolTip.SetToolTip( bRules,
@"Edit the list of rules displayed by the ""/rules"" command.
This list is stored in rules.txt, and can also be edited with any text editor.
If rules.txt is missing or empty, ""/rules"" shows this message:
""Use common sense!""" );

            string tipAnnouncements =
@"Show a random announcement every once in a while.
Announcements are shown to all players, one line at a time, in random order.";
            toolTip.SetToolTip( xAnnouncements, tipAnnouncements );

            toolTip.SetToolTip( nAnnouncements, ConfigKey.AnnouncementInterval.GetDescription() );
            toolTip.SetToolTip( lAnnouncementsUnits, ConfigKey.AnnouncementInterval.GetDescription() );

            toolTip.SetToolTip( bAnnouncements,
@"Edit the list of announcements (announcements.txt).
One line is shown at a time, in random order.
You can include any color codes in the announcements.
You can also edit announcements.txt with any text editor." );

            toolTip.SetToolTip( bGreeting,
@"Edit a custom greeting that's shown to connecting players.
You can use any color codes, and these special variables:
    {SERVER_NAME} = server name (as defined in config)
    {RANK} = connecting player's rank" );

        }


        void FillToolTipsChat() {

            toolTip.SetToolTip( xRankColorsInChat, ConfigKey.RankColorsInChat.GetDescription() );

            toolTip.SetToolTip( xRankColorsInWorldNames, ConfigKey.RankColorsInWorldNames.GetDescription() );

            toolTip.SetToolTip( xRankPrefixesInChat, ConfigKey.RankPrefixesInChat.GetDescription() );

            toolTip.SetToolTip( xRankPrefixesInList, ConfigKey.RankPrefixesInList.GetDescription() );

            toolTip.SetToolTip( xShowConnectionMessages, ConfigKey.ShowConnectionMessages.GetDescription() );

            // TODO: ShowBannedConnectionMessages

            toolTip.SetToolTip( xShowJoinedWorldMessages, ConfigKey.ShowJoinedWorldMessages.GetDescription() );

            toolTip.SetToolTip( bColorSys, ConfigKey.SystemMessageColor.GetDescription() );
            toolTip.SetToolTip( lColorSys, ConfigKey.SystemMessageColor.GetDescription() );

            toolTip.SetToolTip( bColorHelp, ConfigKey.HelpColor.GetDescription() );
            toolTip.SetToolTip( lColorHelp, ConfigKey.HelpColor.GetDescription() );

            toolTip.SetToolTip( bColorSay, ConfigKey.SayColor.GetDescription() );
            toolTip.SetToolTip( lColorSay, ConfigKey.SayColor.GetDescription() );

            toolTip.SetToolTip( bColorAnnouncement, ConfigKey.AnnouncementColor.GetDescription() );
            toolTip.SetToolTip( lColorAnnouncement, ConfigKey.AnnouncementColor.GetDescription() );

            toolTip.SetToolTip( bColorPM, ConfigKey.PrivateMessageColor.GetDescription() );
            toolTip.SetToolTip( lColorPM, ConfigKey.PrivateMessageColor.GetDescription() );

            toolTip.SetToolTip( bColorMe, ConfigKey.MeColor.GetDescription() );
            toolTip.SetToolTip( lColorMe, ConfigKey.MeColor.GetDescription() );

            toolTip.SetToolTip( bColorWarning, ConfigKey.WarningColor.GetDescription() );
            toolTip.SetToolTip( lColorWarning, ConfigKey.WarningColor.GetDescription() );
        }


        void FillToolTipsWorlds() {
            toolTip.SetToolTip( bAddWorld, "Add a new world to the list." );
            toolTip.SetToolTip( bWorldEdit, "Edit or replace an existing world." );
            toolTip.SetToolTip( cMainWorld, "Main world is the first world that players see when they join the server." );
            toolTip.SetToolTip( bWorldDelete, "Delete a world from the list." );

            toolTip.SetToolTip( lDefaultBuildRank, ConfigKey.DefaultBuildRank.GetDescription() );
            toolTip.SetToolTip( cDefaultBuildRank, ConfigKey.DefaultBuildRank.GetDescription() );

            toolTip.SetToolTip( tMapPath, ConfigKey.MapPath.GetDescription() );
            toolTip.SetToolTip( xMapPath, ConfigKey.MapPath.GetDescription() );
        }


        void FillToolTipsRanks() {

            toolTip.SetToolTip( xAllowSecurityCircumvention,
@"Allows players to manupulate whitelists/blacklists or rank requirements
in order to join restricted worlds, or to build in worlds/zones. Normally
players with ManageWorlds and ManageZones permissions are not allowed to do this.
Affected commands:
    /waccess
    /wbuild
    /wmain
    /zedit" );

            toolTip.SetToolTip( bAddRank, "Add a new rank to the list." );
            toolTip.SetToolTip( bDeleteRank,
@"Delete a rank from the list. You will be prompted to specify a replacement
rank - to be able to convert old references to the deleted rank." );
            toolTip.SetToolTip( bRaiseRank,
@"Raise a rank (and all players of the rank) on the hierarchy.
The hierarchy is used for all permission checks." );
            toolTip.SetToolTip( bLowerRank,
@"Lower a rank (and all players of the rank) on the hierarchy.
The hierarchy is used for all permission checks." );

            string tipRankName = "Name of the rank - between 2 and 16 alphanumeric characters.";
            toolTip.SetToolTip( lRankName, tipRankName );
            toolTip.SetToolTip( tRankName, tipRankName );

            string tipRankColor =
@"Color associated with this rank.
Rank colors may be applied to player and world names.";
            toolTip.SetToolTip( lRankColor, tipRankColor );
            toolTip.SetToolTip( bColorRank, tipRankColor );

            string tipPrefix =
@"1-character prefix that may be shown above player names.
The option to show prefixes in chat is on ""General"" tab.";
            toolTip.SetToolTip( lPrefix, tipPrefix );
            toolTip.SetToolTip( tPrefix, tipPrefix );



            string tipKickLimit =
@"Limit on who can be kicked by players of this rank.
By default, players can only kick players of same or lower rank.";
            toolTip.SetToolTip( lKickLimit, tipKickLimit );
            toolTip.SetToolTip( cKickLimit, tipKickLimit );

            string tipBanLimit =
@"Limit on who can be banned by players of this rank.
By default, players can only ban players of same or lower rank.";
            toolTip.SetToolTip( lBanLimit, tipBanLimit );
            toolTip.SetToolTip( cBanLimit, tipBanLimit );

            string tipPromoteLimit =
@"Limit on how much can players of this rank promote others.
By default, players can only promote up to the same or lower rank.";
            toolTip.SetToolTip( lPromoteLimit, tipPromoteLimit );
            toolTip.SetToolTip( cPromoteLimit, tipPromoteLimit );

            string tipDemoteLimit =
@"Limit on who can be demoted by players of this rank.
By default, players can only demote players of same or lower rank.";
            toolTip.SetToolTip( lDemoteLimit, tipDemoteLimit );
            toolTip.SetToolTip( cDemoteLimit, tipDemoteLimit );

            string tipHideLimit =
@"Limit on whom can players of this rank hide from.
By default, players can only hide from players of same or lower rank.";
            toolTip.SetToolTip( lMaxHideFrom, tipHideLimit );
            toolTip.SetToolTip( cMaxHideFrom, tipHideLimit );

            string tipFreezeLimit =
@"Limit on who can be frozen by players of this rank.
By default, players can only freeze players of same or lower rank.";
            toolTip.SetToolTip( lFreezeLimit, tipFreezeLimit );
            toolTip.SetToolTip( cFreezeLimit, tipFreezeLimit );

            string tipMuteLimit =
@"Limit on who can be muted by players of this rank.
By default, players can only mute players of same or lower rank.";
            toolTip.SetToolTip( lMuteLimit, tipMuteLimit );
            toolTip.SetToolTip( cMuteLimit, tipMuteLimit );

            string tipBringLimit =
@"Limit on who can be brought (forcibly teleported) by players of this rank.
By default, players can only bring players of same or lower rank.";
            toolTip.SetToolTip( lBringLimit, tipBringLimit );
            toolTip.SetToolTip( cBringLimit, tipBringLimit );



            toolTip.SetToolTip( xReserveSlot,
@"Allows players of this rank to join the server
even if it reached the maximum number of players." );

            string tipKickIdle = "Allows kicking players who have been inactive/AFK for some time.";
            toolTip.SetToolTip( xKickIdle, tipKickIdle );
            toolTip.SetToolTip( nKickIdle, tipKickIdle );
            toolTip.SetToolTip( lKickIdleUnits, tipKickIdle );

            toolTip.SetToolTip( xAntiGrief,
@"Antigrief is an automated system for kicking players who build
or delete at abnormally high rates. This helps stop certain kinds
of malicious software (like MCTunnel) from doing large-scale damage
to server maps. False positives can sometimes occur if server or
player connection is very laggy." );

            toolTip.SetToolTip( nAntiGriefBlocks,
@"Maximum number of blocks that players of this rank are
allowed to build in a specified time period." );

            toolTip.SetToolTip( nAntiGriefBlocks,
@"Minimum time interval that players of this rank are
expected to spent to build a specified number of blocks." );

            string tipDrawLimit =
@"Limit on the number of blocks that a player is
allowed to affect with drawing or copy/paste commands
at one time. If unchecked, there is no limit.";
            toolTip.SetToolTip( xDrawLimit, tipDrawLimit );
            toolTip.SetToolTip( nDrawLimit, tipDrawLimit );
            toolTip.SetToolTip( lDrawLimitUnits, tipDrawLimit );




            vPermissions.Items[(int)Permission.Ban].ToolTipText =
@"Ability to ban/unban other players from the server.
Affected commands:
    /ban
    /unban";

            vPermissions.Items[(int)Permission.BanAll].ToolTipText =
@"Ability to ban/unban a player account, his IP, and all other accounts that used the IP.
BanAll/UnbanAll commands can be used on players who keep evading bans.
Required permissions: Ban & BanIP
Affected commands:
    /banall
    /unbanall";

            vPermissions.Items[(int)Permission.BanIP].ToolTipText =
@"Ability to ban/unban players by IP.
Required permission: Ban
Affected commands:
    /banip
    /unbanip";

            vPermissions.Items[(int)Permission.Bring].ToolTipText =
@"Ability to bring/summon other players to your location.
This works a bit like reverse-teleport - other player is sent to you.
Affected commands:
    /bring
    /bringall";

            vPermissions.Items[(int)Permission.BringAll].ToolTipText =
@"Ability to bring/summon many players at a time to your location.
Affected command:
    /bringall";

            vPermissions.Items[(int)Permission.Build].ToolTipText =
@"Ability to place blocks on maps. This is a baseline permission
that can be overriden by world-specific and zone-specific permissions.";

            vPermissions.Items[(int)Permission.Chat].ToolTipText =
@"Ability to chat and PM players. Note that players without this
permission can still type in commands, receive PMs, and read chat.
Affected commands:
    /say
    @ (pm)
    @@ (rank chat)";

            vPermissions.Items[(int)Permission.CopyAndPaste].ToolTipText =
@"Ability to copy (or cut) and paste blocks. The total number of
blocks that can be copied or pasted at a time is affected by
the draw limit.
Affected commands:
    /copy
    /cut
    /mirror
    /paste, /pastenot
    /rotate";

            vPermissions.Items[(int)Permission.Delete].ToolTipText =
@"Ability to delete or replace blocks on maps. This is a baseline permission
that can be overriden by world-specific and zone-specific permissions.";

            vPermissions.Items[(int)Permission.DeleteAdmincrete].ToolTipText =
@"Ability to delete admincrete (aka adminium) blocks. Even if someone
has this permission, it can be overriden by world-specific and
zone-specific permissions.
Required permission: Delete";

            vPermissions.Items[(int)Permission.Demote].ToolTipText =
@"Ability to demote other players to a lower rank.
Affected commands:
    /rank";

            vPermissions.Items[(int)Permission.Draw].ToolTipText =
@"Ability to use drawing tools (commands capable of affecting many blocks
at once). This permission can be overriden by world-specific and
zone-specific permissions.
Required permission: Build, Delete
Affected commands:
    /cancel
    /cuboid, /cuboidh, and /cuboidw
    /ellipsoid
    /line
    /mark
    /replace and /replacenot
    /undo";

            vPermissions.Items[(int)Permission.EditPlayerDB].ToolTipText =
@"Ability to edit the player database directly. This also adds the ability to
promote/demote/ban players by name, even if they have not visited the server yet.
Also allows to manipulate players' records, and to promote/demote players in batches.
Affected commands:
    /autorankall
    /autorankreload
    /editplayerinfo
    /massrank
    /setinfo";

            vPermissions.Items[(int)Permission.Freeze].ToolTipText =
@"Ability to freeze/unfreeze players. Frozen players cannot
move or build/delete.
Affected commands:
    /freeze
    /unfreeze";

            vPermissions.Items[(int)Permission.Hide].ToolTipText =
@"Ability to appear hidden from other players. You can still chat,
build/delete blocks, use all commands, and join worlds while hidden.
Hidden players are completely invisible to other players.
Affected commands:
    /hide
    /unhide";

            vPermissions.Items[(int)Permission.Import].ToolTipText =
@"Ability to import rank and ban lists from files. Useful if you
are switching from another server software.
Affected commands:
    /importranks
    /importbans";

            vPermissions.Items[(int)Permission.Kick].ToolTipText =
@"Ability to kick players from the server.
Affected commands:
    /kick";

            vPermissions.Items[(int)Permission.Lock].ToolTipText =
@"Ability to lock/unlock maps (locking puts a map into read-only state).
Affected commands:
    /lock
    /unlock
    /lockall
    /unlockall";

            vPermissions.Items[(int)Permission.ManageWorlds].ToolTipText =
@"Ability to manipulate the world list: adding, renaming, and deleting worlds,
loading/saving maps, change per-world permissions, and using the map generator.
Affected commands:
    /wload
    /wunload
    /wrename
    /wmain
    /waccess and /wbuild
    /wflush
    /gen";

            vPermissions.Items[(int)Permission.ManageZones].ToolTipText =
@"Ability to manipulate zones: adding, editing, renaming, and removing zones.
Affected commands:
    /zadd
    /zedit
    /zremove";

            vPermissions.Items[(int)Permission.Mute].ToolTipText =
@"Ability to temporarily mute players. Muted players cannot write chat or 
send PMs, but they can still type in commands, receive PMs, and read chat.
Affected commands:
    /mute
    /unmute";

            vPermissions.Items[(int)Permission.Patrol].ToolTipText =
@"Ability to patrol lower-ranked players. ""Patrolling"" means teleporting
to other players to check on them, usually while hidden.
Required permission: Teleport
Affected commands:
    /patrol";

            vPermissions.Items[(int)Permission.PlaceAdmincrete].ToolTipText =
@"Ability to place admincrete/adminium. This also affects draw commands.
Required permission: Build
Affected commands:
    /solid
    /bind";

            vPermissions.Items[(int)Permission.PlaceGrass].ToolTipText =
@"Ability to place grass blocks. This also affects draw commands.
Required permission: Build
Affected commands:
    /grass
    /bind";

            vPermissions.Items[(int)Permission.PlaceLava].ToolTipText =
@"Ability to place lava blocks. This also affects draw commands.
Required permission: Build
Affected commands:
    /lava
    /bind";

            vPermissions.Items[(int)Permission.PlaceWater].ToolTipText =
@"Ability to place water blocks. This also affects draw commands.
Required permission: Build
Affected commands:
    /water
    /bind";

            vPermissions.Items[(int)Permission.Promote].ToolTipText =
@"Ability to promote players to a higher rank.
Affected commands:
    /rank";

            vPermissions.Items[(int)Permission.ReadStaffChat].ToolTipText =
@"Ability to read staff chat.";

            vPermissions.Items[(int)Permission.ReloadConfig].ToolTipText =
@"Ability to reload the configuration file without restarting.
Affected commands:
    /reloadconfig";

            vPermissions.Items[(int)Permission.Say].ToolTipText =
@"Ability to use /say command.
Required permission: Chat
Affected commands:
    /say";

            vPermissions.Items[(int)Permission.SetSpawn].ToolTipText =
@"Ability to change the spawn point of a world or a player.
Affected commands:
    /setspawn";

            vPermissions.Items[(int)Permission.ShutdownServer].ToolTipText =
@"Ability to shut down or restart the server remotely.
Useful for servers that run on dedicated machines.
Affected commands:
    /shutdown
    /restart";

/*            vPermissions.Items[(int)Permission.Spectate].ToolTipText =
@"Ability to spectate/follow other players in first-person view.
Affected commands:
    /spectate";*/

            vPermissions.Items[(int)Permission.Teleport].ToolTipText =
@"Ability to teleport to other players.
Affected commands:
    /tp";

            vPermissions.Items[(int)Permission.UseColorCodes].ToolTipText =
@"Ability to use color codes in chat messages.";

            vPermissions.Items[(int)Permission.UseSpeedHack].ToolTipText =
@"Ability to move at a faster-than-normal rate (using hacks).
WARNING: Speedhack detection is often inaccurate, and may produce many
false positives - especially on laggy servers.";

            vPermissions.Items[(int)Permission.ViewOthersInfo].ToolTipText =
@"Ability to view extended information about other players.
Affected commands:
    /info
    /baninfo
    /where";

            vPermissions.Items[(int)Permission.ViewPlayerIPs].ToolTipText =
@"Ability to view players' IP addresses.
Affected commands:
    /info
    /baninfo
    /banip, /banall, /unbanip, /unbanall";
        }


        void FillToolTipsSecurity() {
            toolTip.SetToolTip( lVerifyNames, ConfigKey.VerifyNames.GetDescription() );
            toolTip.SetToolTip( cVerifyNames, ConfigKey.VerifyNames.GetDescription() );

            toolTip.SetToolTip( xMaxConnectionsPerIP, ConfigKey.MaxConnectionsPerIP.GetDescription() );
            toolTip.SetToolTip( nMaxConnectionsPerIP, ConfigKey.MaxConnectionsPerIP.GetDescription() );

            toolTip.SetToolTip( xAllowUnverifiedLAN, ConfigKey.AllowUnverifiedLAN.GetDescription() );

            toolTip.SetToolTip( xRequireBanReason, ConfigKey.RequireBanReason.GetDescription() );
            toolTip.SetToolTip( xRequireKickReason, ConfigKey.RequireKickReason.GetDescription() );
            toolTip.SetToolTip( xRequireRankChangeReason, ConfigKey.RequireRankChangeReason.GetDescription() );

            toolTip.SetToolTip( xAnnounceKickAndBanReasons, ConfigKey.AnnounceKickAndBanReasons.GetDescription() );
            toolTip.SetToolTip( xAnnounceRankChanges, ConfigKey.AnnounceRankChanges.GetDescription() );
            toolTip.SetToolTip( xAnnounceRankChangeReasons, ConfigKey.AnnounceRankChanges.GetDescription() );


            toolTip.SetToolTip( lPatrolledRank, ConfigKey.PatrolledRank.GetDescription() );
            toolTip.SetToolTip( cPatrolledRank, ConfigKey.PatrolledRank.GetDescription() );
            toolTip.SetToolTip( lPatrolledRankAndBelow, ConfigKey.PatrolledRank.GetDescription() );

            toolTip.SetToolTip( nAntispamMessageCount, ConfigKey.AntispamMessageCount.GetDescription() );
            toolTip.SetToolTip( lAntispamMessageCount, ConfigKey.AntispamMessageCount.GetDescription() );
            toolTip.SetToolTip( nAntispamInterval, ConfigKey.AntispamInterval.GetDescription() );
            toolTip.SetToolTip( lAntispamInterval, ConfigKey.AntispamInterval.GetDescription() );

            toolTip.SetToolTip( xAntispamKicks, "Kick players who repeatedly trigger antispam warnings." );
            toolTip.SetToolTip( nAntispamMaxWarnings, ConfigKey.AntispamMaxWarnings.GetDescription() );
            toolTip.SetToolTip( lAntispamMaxWarnings, ConfigKey.AntispamMaxWarnings.GetDescription() );

            toolTip.SetToolTip( xPaidPlayersOnly, ConfigKey.PaidPlayersOnly.GetDescription() );
        }


        void FillToolTipsSavingAndBackup() {

            toolTip.SetToolTip( xSaveInterval, ConfigKey.SaveInterval.GetDescription() );
            toolTip.SetToolTip( nSaveInterval, ConfigKey.SaveInterval.GetDescription() );
            toolTip.SetToolTip( lSaveIntervalUnits, ConfigKey.SaveInterval.GetDescription() );

            toolTip.SetToolTip( xBackupOnStartup, ConfigKey.BackupOnStartup.GetDescription() );

            toolTip.SetToolTip( xBackupOnJoin, ConfigKey.BackupOnJoin.GetDescription() );

            toolTip.SetToolTip( xBackupInterval, ConfigKey.BackupInterval.GetDescription() );
            toolTip.SetToolTip( nBackupInterval, ConfigKey.BackupInterval.GetDescription() );
            toolTip.SetToolTip( lBackupIntervalUnits, ConfigKey.BackupInterval.GetDescription() );

            toolTip.SetToolTip( xBackupOnlyWhenChanged, ConfigKey.BackupInterval.GetDescription() );

            toolTip.SetToolTip( xMaxBackups, ConfigKey.MaxBackups.GetDescription() );
            toolTip.SetToolTip( nMaxBackups, ConfigKey.MaxBackups.GetDescription() );
            toolTip.SetToolTip( lMaxBackups, ConfigKey.MaxBackups.GetDescription() );

            toolTip.SetToolTip( xMaxBackupSize, ConfigKey.MaxBackupSize.GetDescription() );
            toolTip.SetToolTip( nMaxBackupSize, ConfigKey.MaxBackupSize.GetDescription() );
            toolTip.SetToolTip( lMaxBackupSize, ConfigKey.MaxBackupSize.GetDescription() );
        }


        void FillToolTipsLogging() {
            toolTip.SetToolTip( lLogMode, ConfigKey.LogMode.GetDescription() );
            toolTip.SetToolTip( cLogMode, ConfigKey.LogMode.GetDescription() );

            toolTip.SetToolTip( xLogLimit, ConfigKey.MaxLogs.GetDescription() );
            toolTip.SetToolTip( nLogLimit, ConfigKey.MaxLogs.GetDescription() );
            toolTip.SetToolTip( lLogLimitUnits, ConfigKey.MaxLogs.GetDescription() );

            vLogFileOptions.Items[(int)LogType.ConsoleInput].ToolTipText = "Commands typed in from the server console.";
            vLogFileOptions.Items[(int)LogType.ConsoleOutput].ToolTipText =
@"Things sent directly in response to console input,
e.g. output of commands called from console.";
            vLogFileOptions.Items[(int)LogType.Debug].ToolTipText = "Technical information that may be useful to find bugs.";
            vLogFileOptions.Items[(int)LogType.Error].ToolTipText = "Major errors and problems.";
            vLogFileOptions.Items[(int)LogType.SeriousError].ToolTipText = "Errors that prevent server from starting or result in crashes.";
            vLogFileOptions.Items[(int)LogType.GlobalChat].ToolTipText = "Normal chat messages written by players.";
            vLogFileOptions.Items[(int)LogType.IRC].ToolTipText =
@"IRC-related status and error messages.
Does not include IRC chatter (see IRCChat).";
            vLogFileOptions.Items[(int)LogType.PrivateChat].ToolTipText = "PMs (Private Messages) exchanged between players (@player message).";
            vLogFileOptions.Items[(int)LogType.RankChat].ToolTipText = "Rank-wide messages (@@rank message).";
            vLogFileOptions.Items[(int)LogType.SuspiciousActivity].ToolTipText = "Suspicious activity - hack attempts, failed logins, unverified names.";
            vLogFileOptions.Items[(int)LogType.SystemActivity].ToolTipText = "Status messages regarding normal system activity.";
            vLogFileOptions.Items[(int)LogType.UserActivity].ToolTipText = "Status messages regarding players' actions.";
            vLogFileOptions.Items[(int)LogType.UserCommand].ToolTipText = "Commands types in by players.";
            vLogFileOptions.Items[(int)LogType.Warning].ToolTipText = "Minor, recoverable errors and problems.";

            foreach( LogType type in Enum.GetValues( typeof( LogType ) ) ) {
                vConsoleOptions.Items[(int)type].ToolTipText = vLogFileOptions.Items[(int)type].ToolTipText;
            }
        }


        void FillToolTipsIRC() {
            toolTip.SetToolTip( xIRCBotEnabled, ConfigKey.IRCBotEnabled.GetDescription() );

            string tipIRCList =
@"Choose one of these popular IRC networks,
or type in address/port manually below.";
            toolTip.SetToolTip( lIRCList, tipIRCList );
            toolTip.SetToolTip( cIRCList, tipIRCList );

            toolTip.SetToolTip( lIRCBotNick, ConfigKey.IRCBotNick.GetDescription() );
            toolTip.SetToolTip( tIRCBotNick, ConfigKey.IRCBotNick.GetDescription() );

            toolTip.SetToolTip( lIRCBotNetwork, ConfigKey.IRCBotNetwork.GetDescription() );
            toolTip.SetToolTip( tIRCBotNetwork, ConfigKey.IRCBotNetwork.GetDescription() );

            toolTip.SetToolTip( lIRCBotPort, ConfigKey.IRCBotPort.GetDescription() );
            toolTip.SetToolTip( nIRCBotPort, ConfigKey.IRCBotPort.GetDescription() );

            toolTip.SetToolTip( lIRCDelay, ConfigKey.IRCDelay.GetDescription() );
            toolTip.SetToolTip( nIRCDelay, ConfigKey.IRCDelay.GetDescription() );
            toolTip.SetToolTip( lIRCDelayUnits, ConfigKey.IRCDelay.GetDescription() );

            toolTip.SetToolTip( tIRCBotChannels, ConfigKey.IRCBotChannels.GetDescription() );

            toolTip.SetToolTip( xIRCRegisteredNick, ConfigKey.IRCRegisteredNick.GetDescription() );

            toolTip.SetToolTip( lIRCNickServ, ConfigKey.IRCNickServ.GetDescription() );
            toolTip.SetToolTip( tIRCNickServ, ConfigKey.IRCNickServ.GetDescription() );

            toolTip.SetToolTip( lIRCNickServMessage, ConfigKey.IRCNickServMessage.GetDescription() );
            toolTip.SetToolTip( tIRCNickServMessage, ConfigKey.IRCNickServMessage.GetDescription() );

            toolTip.SetToolTip( lColorIRC, ConfigKey.IRCMessageColor.GetDescription() );
            toolTip.SetToolTip( bColorIRC, ConfigKey.IRCMessageColor.GetDescription() );

            toolTip.SetToolTip( xIRCBotForwardFromIRC, ConfigKey.IRCBotForwardFromIRC.GetDescription() );
            toolTip.SetToolTip( xIRCBotAnnounceIRCJoins, ConfigKey.IRCBotAnnounceIRCJoins.GetDescription() );

            toolTip.SetToolTip( xIRCBotForwardFromServer, ConfigKey.IRCBotForwardFromServer.GetDescription() );
            toolTip.SetToolTip( xIRCBotAnnounceServerJoins, ConfigKey.IRCBotAnnounceServerJoins.GetDescription() );
            toolTip.SetToolTip( xIRCBotAnnounceServerEvents, ConfigKey.IRCBotAnnounceServerEvents.GetDescription() );

            // TODO: IRCThreads

            toolTip.SetToolTip( xIRCUseColor, ConfigKey.IRCUseColor.GetDescription() );
        }


        void FillToolTipsAdvanced() {
            toolTip.SetToolTip( xRelayAllBlockUpdates, ConfigKey.RelayAllBlockUpdates.GetDescription() );

            toolTip.SetToolTip( xNoPartialPositionUpdates, ConfigKey.NoPartialPositionUpdates.GetDescription() );

            toolTip.SetToolTip( xLowLatencyMode, ConfigKey.LowLatencyMode.GetDescription() );

            toolTip.SetToolTip( lProcessPriority, ConfigKey.ProcessPriority.GetDescription() );
            toolTip.SetToolTip( cProcessPriority, ConfigKey.ProcessPriority.GetDescription() );

            toolTip.SetToolTip( lUpdater, ConfigKey.UpdaterMode.GetDescription() );
            toolTip.SetToolTip( cUpdaterMode, ConfigKey.UpdaterMode.GetDescription() );

            toolTip.SetToolTip( lThrottling, ConfigKey.BlockUpdateThrottling.GetDescription() );
            toolTip.SetToolTip( nThrottling, ConfigKey.BlockUpdateThrottling.GetDescription() );
            toolTip.SetToolTip( lThrottlingUnits, ConfigKey.BlockUpdateThrottling.GetDescription() );

            toolTip.SetToolTip( lTickInterval, ConfigKey.TickInterval.GetDescription() );
            toolTip.SetToolTip( nTickInterval, ConfigKey.TickInterval.GetDescription() );
            toolTip.SetToolTip( lTickIntervalUnits, ConfigKey.TickInterval.GetDescription() );

            toolTip.SetToolTip( xMaxUndo, ConfigKey.MaxUndo.GetDescription() );
            toolTip.SetToolTip( nMaxUndo, ConfigKey.MaxUndo.GetDescription() );
            toolTip.SetToolTip( lMaxUndoUnits, ConfigKey.MaxUndo.GetDescription() );

            toolTip.SetToolTip( xIP, ConfigKey.IP.GetDescription() );
            toolTip.SetToolTip( tIP, ConfigKey.IP.GetDescription() );
        }
    }
}