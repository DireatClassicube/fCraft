﻿// Copyright 2009-2013 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using fCraft.AutoRank;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Several yet-undocumented commands, mostly related to AutoRank. </summary>
    static class MaintenanceCommands {

        internal static void Init() {
            CommandManager.RegisterCommand( CdDumpStats );

            CommandManager.RegisterCommand( CdMassRank );
            CommandManager.RegisterCommand( CdSetInfo );

            CommandManager.RegisterCommand( CdReload );

            CommandManager.RegisterCommand( CdShutdown );
            CommandManager.RegisterCommand( CdRestart );

            CommandManager.RegisterCommand( CdPruneDB );

            CommandManager.RegisterCommand( CdImport );
            //CommandManager.RegisterCommand( CdImportRankList );
            //CommandManager.RegisterCommand( CdExport );

            CommandManager.RegisterCommand( CdInfoSwap );

#if DEBUG
            CommandManager.RegisterCommand( new CommandDescriptor {
                Name = "BUM",
                IsHidden = true,
                Category = CommandCategory.Maintenance | CommandCategory.Debug,
                Help = "Bandwidth Use Mode statistics.",
                Handler = delegate( Player player, CommandReader cmd ) {
                    string newModeName = cmd.Next();
                    if( newModeName == null ) {
                        player.Message( "{0}: S: {1}  R: {2}  S/s: {3:0.0}  R/s: {4:0.0}",
                                        player.BandwidthUseMode,
                                        player.BytesSent,
                                        player.BytesReceived,
                                        player.BytesSentRate,
                                        player.BytesReceivedRate );
                    } else {
                        var newMode = (BandwidthUseMode)Enum.Parse( typeof( BandwidthUseMode ), newModeName, true );
                        player.BandwidthUseMode = newMode;
                        player.Info.BandwidthUseMode = newMode;
                    }
                }
            } );

            CommandManager.RegisterCommand( new CommandDescriptor {
                Name = "BDBDB",
                IsHidden = true,
                Category = CommandCategory.Maintenance | CommandCategory.Debug,
                Help = "BlockDB Debug",
                Handler = delegate( Player player, CommandReader cmd ) {
                    if( player.World == null ) PlayerOpException.ThrowNoWorld( player );
                    BlockDB db = player.World.BlockDB;
                    using( db.GetReadLock() ) {
                        player.Message( "BlockDB: CAP={0} SZ={1} FI={2}",
                                        db.CacheCapacity, db.CacheSize, db.LastFlushedIndex );
                    }
                }
            } );
#endif
        }


        #region DumpStats

        static readonly CommandDescriptor CdDumpStats = new CommandDescriptor {
            Name = "DumpStats",
            Category = CommandCategory.Maintenance,
            IsConsoleSafe = true,
            IsHidden = true,
            Permissions = new[] { Permission.EditPlayerDB },
            Help = "Writes out a number of statistics about the server. " +
                   "Only non-banned players active in the last 30 days are counted.",
            Usage = "/DumpStats FileName",
            Handler = DumpStatsHandler
        };

        const int TopPlayersToList = 5;

        static void DumpStatsHandler( Player player, CommandReader cmd ) {
            string fileName = cmd.Next();
            if( fileName == null ) {
                CdDumpStats.PrintUsage( player );
                return;
            }

            try {
                if( !Paths.Contains( Paths.WorkingPath, fileName ) ) {
                    player.MessageUnsafePath();
                    return;
                }
            } catch( ArgumentException e ) {
                player.Message( "DumpStats: {0}", e.Message );
                return;
            }

            // ReSharper disable AssignNullToNotNullAttribute
            if( Paths.IsProtectedFileName( Path.GetFileName( fileName ) ) ) {
                // ReSharper restore AssignNullToNotNullAttribute
                player.Message( "You may not use this file." );
                return;
            }

            string extension = Path.GetExtension( fileName );
            if( extension == null || !extension.Equals( ".txt", StringComparison.OrdinalIgnoreCase ) ) {
                player.Message( "Stats file name must end with .txt" );
                return;
            }

            if( File.Exists( fileName ) && !cmd.IsConfirmed ) {
                Logger.Log( LogType.UserActivity,
                            "DumpStats: Asked {0} for confirmation to overwrite \"{1}\"",
                            player.Name, fileName );
                player.Confirm( cmd, "File \"{0}\" already exists. Overwrite?", Path.GetFileName( fileName ) );
                return;
            }

            if( !Paths.TestFile( "DumpStats file", fileName, false, FileAccess.Write ) ) {
                player.Message( "Cannot create specified file. See log for details." );
                return;
            }

            using( FileStream fs = File.Create( fileName ) ) {
                using( StreamWriter writer = new StreamWriter( fs ) ) {
                    PlayerInfo[] infos = PlayerDB.PlayerInfoList;
                    if( infos.Length == 0 ) {
                        writer.WriteLine( "(TOTAL) (0 players)" );
                        writer.WriteLine();
                    } else {
                        DumpPlayerGroupStats( writer, infos, "(TOTAL)" );
                    }

                    List<PlayerInfo> rankPlayers = new List<PlayerInfo>();
                    foreach( Rank rank in RankManager.Ranks ) {
                        rankPlayers.AddRange( infos.Where( t => t.Rank == rank ) );
                        if( rankPlayers.Count == 0 ) {
                            writer.WriteLine( "{0}: 0 players, 0 banned, 0 inactive", rank.Name );
                            writer.WriteLine();
                        } else {
                            DumpPlayerGroupStats( writer, rankPlayers, rank.Name );
                        }
                        rankPlayers.Clear();
                    }
                }
            }

            player.Message( "Stats saved to \"{0}\"", fileName );
        }

        static void DumpPlayerGroupStats( TextWriter writer, IList<PlayerInfo> infos, string groupName ) {
            RankStats stat = new RankStats();
            foreach( Rank rank2 in RankManager.Ranks ) {
                stat.PreviousRank.Add( rank2, 0 );
            }

            int totalCount = infos.Count;
            int bannedCount = infos.Count( info => info.IsBanned );
            int inactiveCount = infos.Count( info => info.TimeSinceLastSeen.TotalDays >= 30 );
            infos = infos.Where( info => (info.TimeSinceLastSeen.TotalDays < 30 && !info.IsBanned) ).ToList();

            if( infos.Count == 0 ) {
                writer.WriteLine( "{0}: {1} players, {2} banned, {3} inactive",
                                  groupName, totalCount, bannedCount, inactiveCount );
                writer.WriteLine();
                return;
            }

            for( int i = 0; i < infos.Count; i++ ) {
                stat.TimeSinceFirstLogin += infos[i].TimeSinceFirstLogin;
                stat.TimeSinceLastLogin += infos[i].TimeSinceLastLogin;
                stat.TotalTime += infos[i].TotalTime;
                stat.BlocksBuilt += infos[i].BlocksBuilt;
                stat.BlocksDeleted += infos[i].BlocksDeleted;
                stat.BlocksDrawn += infos[i].BlocksDrawn;
                stat.TimesVisited += infos[i].TimesVisited;
                stat.MessagesWritten += infos[i].MessagesWritten;
                stat.TimesKicked += infos[i].TimesKicked;
                stat.TimesKickedOthers += infos[i].TimesKickedOthers;
                stat.TimesBannedOthers += infos[i].TimesBannedOthers;
                if( infos[i].PreviousRank != null ) stat.PreviousRank[infos[i].PreviousRank]++;
            }

            stat.BlockRatio = stat.BlocksBuilt / (double)Math.Max( stat.BlocksDeleted, 1 );
            stat.BlocksChanged = stat.BlocksDeleted + stat.BlocksBuilt;


            stat.TimeSinceFirstLoginMedian = DateTime.UtcNow.Subtract( infos.OrderByDescending( info => info.FirstLoginDate )
                                                                            .ElementAt( infos.Count / 2 ).FirstLoginDate );
            stat.TimeSinceLastLoginMedian = DateTime.UtcNow.Subtract( infos.OrderByDescending( info => info.LastLoginDate )
                                                                           .ElementAt( infos.Count / 2 ).LastLoginDate );
            stat.TotalTimeMedian = infos.OrderByDescending( info => info.TotalTime ).ElementAt( infos.Count / 2 ).TotalTime;
            stat.BlocksBuiltMedian = infos.OrderByDescending( info => info.BlocksBuilt ).ElementAt( infos.Count / 2 ).BlocksBuilt;
            stat.BlocksDeletedMedian = infos.OrderByDescending( info => info.BlocksDeleted ).ElementAt( infos.Count / 2 ).BlocksDeleted;
            stat.BlocksDrawnMedian = infos.OrderByDescending( info => info.BlocksDrawn ).ElementAt( infos.Count / 2 ).BlocksDrawn;
            PlayerInfo medianBlocksChangedPlayerInfo = infos.OrderByDescending( info => (info.BlocksDeleted + info.BlocksBuilt) ).ElementAt( infos.Count / 2 );
            stat.BlocksChangedMedian = medianBlocksChangedPlayerInfo.BlocksDeleted + medianBlocksChangedPlayerInfo.BlocksBuilt;
            PlayerInfo medianBlockRatioPlayerInfo = infos.OrderByDescending( info => (info.BlocksBuilt / (double)Math.Max( info.BlocksDeleted, 1 )) )
                                                    .ElementAt( infos.Count / 2 );
            stat.BlockRatioMedian = medianBlockRatioPlayerInfo.BlocksBuilt / (double)Math.Max( medianBlockRatioPlayerInfo.BlocksDeleted, 1 );
            stat.TimesVisitedMedian = infos.OrderByDescending( info => info.TimesVisited ).ElementAt( infos.Count / 2 ).TimesVisited;
            stat.MessagesWrittenMedian = infos.OrderByDescending( info => info.MessagesWritten ).ElementAt( infos.Count / 2 ).MessagesWritten;
            stat.TimesKickedMedian = infos.OrderByDescending( info => info.TimesKicked ).ElementAt( infos.Count / 2 ).TimesKicked;
            stat.TimesKickedOthersMedian = infos.OrderByDescending( info => info.TimesKickedOthers ).ElementAt( infos.Count / 2 ).TimesKickedOthers;
            stat.TimesBannedOthersMedian = infos.OrderByDescending( info => info.TimesBannedOthers ).ElementAt( infos.Count / 2 ).TimesBannedOthers;


            stat.TopTimeSinceFirstLogin = infos.OrderBy( info => info.FirstLoginDate ).ToArray();
            stat.TopTimeSinceLastLogin = infos.OrderBy( info => info.LastLoginDate ).ToArray();
            stat.TopTotalTime = infos.OrderByDescending( info => info.TotalTime ).ToArray();
            stat.TopBlocksBuilt = infos.OrderByDescending( info => info.BlocksBuilt ).ToArray();
            stat.TopBlocksDeleted = infos.OrderByDescending( info => info.BlocksDeleted ).ToArray();
            stat.TopBlocksDrawn = infos.OrderByDescending( info => info.BlocksDrawn ).ToArray();
            stat.TopBlocksChanged = infos.OrderByDescending( info => (info.BlocksDeleted + info.BlocksBuilt) ).ToArray();
            stat.TopBlockRatio = infos.OrderByDescending( info => (info.BlocksBuilt / (double)Math.Max( info.BlocksDeleted, 1 )) ).ToArray();
            stat.TopTimesVisited = infos.OrderByDescending( info => info.TimesVisited ).ToArray();
            stat.TopMessagesWritten = infos.OrderByDescending( info => info.MessagesWritten ).ToArray();
            stat.TopTimesKicked = infos.OrderByDescending( info => info.TimesKicked ).ToArray();
            stat.TopTimesKickedOthers = infos.OrderByDescending( info => info.TimesKickedOthers ).ToArray();
            stat.TopTimesBannedOthers = infos.OrderByDescending( info => info.TimesBannedOthers ).ToArray();


            writer.WriteLine( "{0}: {1} players, {2} banned, {3} inactive",
                              groupName, totalCount, bannedCount, inactiveCount );
            writer.WriteLine( "    TimeSinceFirstLogin: {0} mean,  {1} median,  {2} total",
                              TimeSpan.FromTicks( stat.TimeSinceFirstLogin.Ticks / infos.Count ).ToCompactString(),
                              stat.TimeSinceFirstLoginMedian.ToCompactString(),
                              stat.TimeSinceFirstLogin.ToCompactString() );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTimeSinceFirstLogin.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimeSinceFirstLogin.ToCompactString(), info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTimeSinceFirstLogin.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimeSinceFirstLogin.ToCompactString(), info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTimeSinceFirstLogin ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimeSinceFirstLogin.ToCompactString(), info.Name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    TimeSinceLastLogin: {0} mean,  {1} median,  {2} total",
                              TimeSpan.FromTicks( stat.TimeSinceLastLogin.Ticks / infos.Count ).ToCompactString(),
                              stat.TimeSinceLastLoginMedian.ToCompactString(),
                              stat.TimeSinceLastLogin.ToCompactString() );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTimeSinceLastLogin.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimeSinceLastLogin.ToCompactString(), info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTimeSinceLastLogin.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimeSinceLastLogin.ToCompactString(), info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTimeSinceLastLogin ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimeSinceLastLogin.ToCompactString(), info.Name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    TotalTime: {0} mean,  {1} median,  {2} total",
                              TimeSpan.FromTicks( stat.TotalTime.Ticks / infos.Count ).ToCompactString(),
                              stat.TotalTimeMedian.ToCompactString(),
                              stat.TotalTime.ToCompactString() );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTotalTime.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TotalTime.ToCompactString(), info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTotalTime.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TotalTime.ToCompactString(), info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTotalTime ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TotalTime.ToCompactString(), info.Name );
                }
            }
            writer.WriteLine();



            writer.WriteLine( "    BlocksBuilt: {0} mean,  {1} median,  {2} total",
                              stat.BlocksBuilt / infos.Count,
                              stat.BlocksBuiltMedian,
                              stat.BlocksBuilt );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopBlocksBuilt.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.BlocksBuilt, info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopBlocksBuilt.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.BlocksBuilt, info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopBlocksBuilt ) {
                    writer.WriteLine( "        {0,20}  {1}", info.BlocksBuilt, info.Name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    BlocksDeleted: {0} mean,  {1} median,  {2} total",
                              stat.BlocksDeleted / infos.Count,
                              stat.BlocksDeletedMedian,
                              stat.BlocksDeleted );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopBlocksDeleted.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.BlocksDeleted, info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopBlocksDeleted.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.BlocksDeleted, info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopBlocksDeleted ) {
                    writer.WriteLine( "        {0,20}  {1}", info.BlocksDeleted, info.Name );
                }
            }
            writer.WriteLine();



            writer.WriteLine( "    BlocksChanged: {0} mean,  {1} median,  {2} total",
                              stat.BlocksChanged / infos.Count,
                              stat.BlocksChangedMedian,
                              stat.BlocksChanged );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopBlocksChanged.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", (info.BlocksDeleted + info.BlocksBuilt), info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopBlocksChanged.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", (info.BlocksDeleted + info.BlocksBuilt), info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopBlocksChanged ) {
                    writer.WriteLine( "        {0,20}  {1}", (info.BlocksDeleted + info.BlocksBuilt), info.Name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    BlocksDrawn: {0} mean,  {1} median,  {2} total",
                              stat.BlocksDrawn / infos.Count,
                              stat.BlocksDrawnMedian,
                              stat.BlocksDrawn );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopBlocksDrawn.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.BlocksDrawn, info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopBlocksDrawn.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.BlocksDrawn, info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopBlocksDrawn ) {
                    writer.WriteLine( "        {0,20}  {1}", info.BlocksDrawn, info.Name );
                }
            }


            writer.WriteLine( "    BlockRatio: {0:0.000} mean,  {1:0.000} median",
                              stat.BlockRatio,
                              stat.BlockRatioMedian );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopBlockRatio.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20:0.000}  {1}", (info.BlocksBuilt / (double)Math.Max( info.BlocksDeleted, 1 )), info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopBlockRatio.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20:0.000}  {1}", (info.BlocksBuilt / (double)Math.Max( info.BlocksDeleted, 1 )), info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopBlockRatio ) {
                    writer.WriteLine( "        {0,20:0.000}  {1}", (info.BlocksBuilt / (double)Math.Max( info.BlocksDeleted, 1 )), info.Name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    TimesVisited: {0} mean,  {1} median,  {2} total",
                              stat.TimesVisited / infos.Count,
                              stat.TimesVisitedMedian,
                              stat.TimesVisited );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTimesVisited.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesVisited, info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTimesVisited.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesVisited, info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTimesVisited ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesVisited, info.Name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    MessagesWritten: {0} mean,  {1} median,  {2} total",
                              stat.MessagesWritten / infos.Count,
                              stat.MessagesWrittenMedian,
                              stat.MessagesWritten );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopMessagesWritten.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.MessagesWritten, info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopMessagesWritten.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.MessagesWritten, info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopMessagesWritten ) {
                    writer.WriteLine( "        {0,20}  {1}", info.MessagesWritten, info.Name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    TimesKicked: {0:0.0} mean,  {1} median,  {2} total",
                              stat.TimesKicked / (double)infos.Count,
                              stat.TimesKickedMedian,
                              stat.TimesKicked );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTimesKicked.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesKicked, info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTimesKicked.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesKicked, info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTimesKicked ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesKicked, info.Name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    TimesKickedOthers: {0:0.0} mean,  {1} median,  {2} total",
                              stat.TimesKickedOthers / (double)infos.Count,
                              stat.TimesKickedOthersMedian,
                              stat.TimesKickedOthers );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTimesKickedOthers.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesKickedOthers, info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTimesKickedOthers.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesKickedOthers, info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTimesKickedOthers ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesKickedOthers, info.Name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    TimesBannedOthers: {0:0.0} mean,  {1} median,  {2} total",
                              stat.TimesBannedOthers / (double)infos.Count,
                              stat.TimesBannedOthersMedian,
                              stat.TimesBannedOthers );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTimesBannedOthers.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesBannedOthers, info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTimesBannedOthers.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesBannedOthers, info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTimesBannedOthers ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesBannedOthers, info.Name );
                }
            }
            writer.WriteLine();
        }


        sealed class RankStats {
            public TimeSpan TimeSinceFirstLogin;
            public TimeSpan TimeSinceLastLogin;
            public TimeSpan TotalTime;
            public long BlocksBuilt;
            public long BlocksDeleted;
            public long BlocksChanged;
            public long BlocksDrawn;
            public double BlockRatio;
            public long TimesVisited;
            public long MessagesWritten;
            public long TimesKicked;
            public long TimesKickedOthers;
            public long TimesBannedOthers;
            public readonly Dictionary<Rank, int> PreviousRank = new Dictionary<Rank, int>();

            public TimeSpan TimeSinceFirstLoginMedian;
            public TimeSpan TimeSinceLastLoginMedian;
            public TimeSpan TotalTimeMedian;
            public int BlocksBuiltMedian;
            public int BlocksDeletedMedian;
            public int BlocksChangedMedian;
            public long BlocksDrawnMedian;
            public double BlockRatioMedian;
            public int TimesVisitedMedian;
            public int MessagesWrittenMedian;
            public int TimesKickedMedian;
            public int TimesKickedOthersMedian;
            public int TimesBannedOthersMedian;

            public PlayerInfo[] TopTimeSinceFirstLogin;
            public PlayerInfo[] TopTimeSinceLastLogin;
            public PlayerInfo[] TopTotalTime;
            public PlayerInfo[] TopBlocksBuilt;
            public PlayerInfo[] TopBlocksDeleted;
            public PlayerInfo[] TopBlocksChanged;
            public PlayerInfo[] TopBlocksDrawn;
            public PlayerInfo[] TopBlockRatio;
            public PlayerInfo[] TopTimesVisited;
            public PlayerInfo[] TopMessagesWritten;
            public PlayerInfo[] TopTimesKicked;
            public PlayerInfo[] TopTimesKickedOthers;
            public PlayerInfo[] TopTimesBannedOthers;
        }

        #endregion


        #region MassRank

        static readonly CommandDescriptor CdMassRank = new CommandDescriptor {
            Name = "MassRank",
            Category = CommandCategory.Maintenance | CommandCategory.Moderation,
            IsHidden = true,
            IsConsoleSafe = true,
            Permissions = new[] {Permission.EditPlayerDB, Permission.Promote, Permission.Demote},
            Help = "Demotes/promotes all existing players of a certain rank. " +
                   "Rank reason will be \"~MassRank\". You will be asked to confirm before proceeding. " +
                   "Be careful with this command, as it can not be undone.",
            Usage = "/MassRank FromRank ToRank Reason",
            Handler = MassRankHandler
        };

        static void MassRankHandler( Player player, CommandReader cmd ) {
            string fromRankName = cmd.Next();
            string toRankName = cmd.Next();
            string reason = cmd.NextAll();
            if( fromRankName == null || toRankName == null ) {
                CdMassRank.PrintUsage( player );
                return;
            }

            Rank fromRank = RankManager.FindRank( fromRankName );
            if( fromRank == null ) {
                player.MessageNoRank( fromRankName );
                return;
            }

            Rank toRank = RankManager.FindRank( toRankName );
            if( toRank == null ) {
                player.MessageNoRank( toRankName );
                return;
            }

            if( fromRank == toRank ) {
                player.Message( "Ranks must be different" );
                return;
            }

            int playerCount = fromRank.PlayerCount;
            string verb = (fromRank > toRank ? "demot" : "promot");

            if( !cmd.IsConfirmed ) {
                Logger.Log( LogType.UserActivity,
                            "MassRank: Asked {0} to confirm {1}ion of {2} players.",
                            player.Name, verb, playerCount );
                player.Confirm( cmd, "{0}e {1} players?", verb.UppercaseFirst(), playerCount );
                return;
            }

            player.Message( "MassRank: {0}ing {1} players...",
                            verb, playerCount );

            int affected = PlayerDB.MassRankChange( player, fromRank, toRank, reason );
            player.Message( "MassRank: done, {0} records affected.", affected );
        }


        static readonly CommandDescriptor CdImportRankList = new CommandDescriptor {
            Name = "ImportRankList",
            Category = CommandCategory.Maintenance,
            IsHidden = true,
            IsConsoleSafe = true,
            Permissions = new[] {Permission.Import},
            Help = "", // TODO
            Usage = "/ImportRankList FileName ToRank Reason",
            Handler = ImportRankListHandler
        };

        // TODO: document the fact that this only promotes (unlike "/Import Ranks")
        static void ImportRankListHandler( Player player, CommandReader cmd ) {
            string fileName = cmd.Next();
            string rankName = cmd.Next();
            string reason = cmd.NextAll();

            if( fileName == null || rankName == null ) {
                CdImportRankList.PrintUsage( player );
                return;
            }

            // parse rank name
            Rank rank = RankManager.FindRank( rankName );
            if( rank == null ) {
                player.MessageNoRank( rankName );
                return;
            }
            
            // Make sure that the target file is legit
            if( !Paths.Contains( Paths.WorkingPath, fileName ) ) {
                Logger.Log( LogType.SuspiciousActivity,
                            "ImportRankList: Player {0} tried to import from \"{1}\"",
                            player.Name,
                            fileName );
                player.MessageUnsafePath();
                return;
            }

            // Make sure file exists
            if( !File.Exists( fileName ) ) {
                player.Message( "Rank list file not found: " + fileName );
                return;
            }

            // Read list of names from file.
            // Using List list to preserve capitalization and a HashSet to avoid duplicates.
            List<string> nameList = new List<string>();
            using( StreamReader reader = new StreamReader( fileName ) ) {
                HashSet<string> lowerNameSet = new HashSet<string>();
                while( true ) {
                    string nextName = reader.ReadLine();
                    if( nextName == null ) break;
                    if( !Player.IsValidPlayerName( nextName ) ) {
                        player.Message( "ImportRankList: Invalid player name skipped: {0}", nextName );
                        continue;
                    }
                    string nameToLower = nextName.ToLowerInvariant();
                    if( lowerNameSet.Contains( nameToLower ) ) {
                        player.Message( "ImportRankList: Skipping a duplicate name: {0}", nextName );
                        continue;
                    }
                    nameList.Add( nextName );
                    lowerNameSet.Add( nameToLower );
                }
            }

            // Ask player to confirm before continuing
            if( !cmd.IsConfirmed ) {
                Logger.Log( LogType.UserActivity,
                            "Import: Asked {0} to confirm importing {1} ranks from {2}",
                            player.Name,
                            nameList.Count,
                            fileName );
                player.Confirm( cmd,
                                "ImportRankList: Are you sure you want to rank {0} players to {1}&S?",
                                nameList.Count,
                                rank.ClassyName );
                return;
            }

            // Carry out the promotions
            int newPlayers = 0,
                promotedPlayers = 0,
                skippedPlayers = 0;
            foreach( string name in nameList ) {
                PlayerInfo info = PlayerDB.FindPlayerInfoExact( name );
                if( info == null ) {
                    // Create and promote a new record
                    newPlayers++;
                    PlayerInfo newInfo = PlayerDB.CreateNewPlayerInfo( name, RankChangeType.Promoted );
                    newInfo.ChangeRank( player, rank, reason, true, true, false );
                    Logger.Log( LogType.UserActivity, "ImportRankList: Created a new player record for {0}", name );

                } else {
                    // Check if an existing record needs updating
                    if( info.Rank < rank && // don't demote anyone
                        !info.IsBanned && // don't promote banned players
                        info.RankChangeType != RankChangeType.Demoted && // don't re-promote demoted players
                        info.RankChangeType != RankChangeType.AutoDemoted ) {
                        // Promote!
                        info.ChangeRank( player, rank, reason, true, true, false );
                        promotedPlayers++;

                    } else {
                        skippedPlayers++;
                    }
                }
            }
            string successMsg = String.Format(
                "ImportRankList: Created {0} new records, promoted {1} players, " +
                "skipped {2} records from file \"{3}\"",
                newPlayers,
                promotedPlayers,
                skippedPlayers,
                Path.GetFileName( fileName ) );
            Logger.Log( LogType.UserActivity, successMsg );
            player.Message( successMsg );
        }

        #endregion


        #region SetInfo

        static readonly CommandDescriptor CdSetInfo = new CommandDescriptor {
            Name = "SetInfo",
            Category = CommandCategory.Maintenance | CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.EditPlayerDB },
            Help = "Allows direct editing of players' database records. List of editable properties: " +
                   "Bandwidth, BanReason, DisplayedName, KickReason, Name (capitalization only), " +
                   "PreviousRank, RankChangeType, RankReason, TimesKicked, TotalTime, UnbanReason. " +
                   "For detailed help see &H/Help SetInfo <Property>",
            HelpSections = new Dictionary<string, string>{
                { "bandwidth",      "&H/SetInfo <PlayerName> Bandwidth <Mode>\n&S" +
                                    "Sets custom bandwidth use mode for given player. " +
                                    "<Type> can be: Default, VeryLow, Low, Normal, High, VeryHigh. Shortcut: BW" },
                { "banreason",      "&H/SetInfo <PlayerName> BanReason <Reason>\n&S" +
                                    "Changes ban reason for the given player. Original ban reason is preserved in the logs. " +
                                    "Shortcut: BR" },
                { "displayedname",  "&H/SetInfo <RealPlayerName> DisplayedName <DisplayedName>\n&S" +
                                    "Sets or resets the way player's name is displayed in chat. "+
                                    "Any printable symbols or color codes may be used in the displayed name. "+
                                    "Note that player's real name is still used in logs and on the in-game player list. "+
                                    "To remove a custom name, type \"&H/SetInfo <RealName> DisplayedName&S\" (omit the name). " +
                                    "Shortcut: DN" },
                { "kickreason",     "&H/SetInfo <PlayerName> KickReason <Reason>\n&S" +
                                    "Changes reason of most-recent kick for the given player. " +
                                    "Original kick reason is preserved in the logs. Shortcut: KR" },
                { "name",           "&H/SetInfo <PlayerName> Name <Name>\n&S" +
                                    "Changes capitalization of player's name. " +
                                    "No spelling changes are allowed (use DisplayedName for those, if you must). " +
                                    "Shortcut: N" },
                { "previousrank",   "&H/SetInfo <PlayerName> PreviousRank <RankName>\n&S" +
                                    "Changes previous rank held by the player. " +
                                    "To reset previous rank to \"none\" (will show as \"default\" in &H/Info&S), " +
                                    "type \"&H/SetInfo <Name> PreviousRank&S\" (omit the rank name). Shortcut: PR" },
                { "rankchangetype", "&H/SetInfo <PlayerName> RankChangeType <Type>\n&S" +
                                    "Sets the type of rank change. <Type> can be: Promoted, Demoted, AutoPromoted, AutoDemoted. " +
                                    "Shortcut: RCT" },
                { "rankreason",     "&H/SetInfo <PlayerName> RankReason <Reason>\n&S" +
                                    "Changes promotion/demotion reason for the given player. "+
                                    "Original promotion/demotion reason is preserved in the logs. Shortcut: RR" },
                { "timeskicked",    "&H/SetInfo <PlayerName> TimesKicked <#>\n&S" +
                                    "Changes the number of times that a player has been kicked. "+
                                    "Acceptable value range: 0-9999. Shortcut: TK" },
                { "totaltime",      "&H/SetInfo <PlayerName> TotalTime <Time>\n&S" +
                                    "Changes the amount of game time that the player has on record. " +
                                    "Accepts values in the common compact time-span format. Shortcut: TT" },
                { "unbanreason",    "&H/SetInfo <PlayerName> UnbanReason <Reason>\n&S" +
                                    "Changes unban reason for the given player. " +
                                    "Original unban reason is preserved in the logs. Shortcut: UR" }
            },
            Usage = "/SetInfo <PlayerName> <Property> <Value>",
            Handler = SetInfoHandler
        };

        static void SetInfoHandler( Player player, CommandReader cmd ) {
            string targetName = cmd.Next();
            string propertyName = cmd.Next();
            string valName = cmd.NextAll();

            if( targetName == null || propertyName == null ) {
                CdSetInfo.PrintUsage( player );
                return;
            }

            PlayerInfo info = PlayerDB.FindPlayerInfoOrPrintMatches( player, targetName, SearchOptions.IncludeSelf );
            if( info == null ) return;

            switch( propertyName.ToLower() ) {
                case "timeskicked":
                case "tk":
                    int oldTimesKicked = info.TimesKicked;
                    if( ValidateInt( valName, 0, 9999 ) ) {
                        info.TimesKicked = Int32.Parse( valName );
                        player.Message( "SetInfo: TimesKicked for {0}&S changed from {1} to {2}",
                                        info.ClassyName,
                                        oldTimesKicked,
                                        info.TimesKicked );
                        break;
                    } else {
                        player.Message( "SetInfo: TimesKicked value out of range (acceptable: 0-9999)" );
                        return;
                    }

                case "previousrank":
                case "pr":
                    Rank newPreviousRank;
                    if( valName.Length > 0 ) {
                        newPreviousRank = RankManager.FindRank( valName );
                        if( newPreviousRank == null ) {
                            player.MessageNoRank( valName );
                            return;
                        }
                    } else {
                        newPreviousRank = null;
                    }

                    Rank oldPreviousRank = info.PreviousRank;

                    if( newPreviousRank == null && oldPreviousRank == null ) {
                        player.Message( "SetInfo: PreviousRank for {0}&S is not set.",
                                        info.ClassyName );
                        return;
                    } else if( newPreviousRank == oldPreviousRank ) {
                        player.Message( "SetInfo: PreviousRank for {0}&S is already set to {1}",
                                        info.ClassyName,
                                        newPreviousRank.ClassyName );
                        return;
                    }
                    info.PreviousRank = newPreviousRank;

                    if( oldPreviousRank == null ) {
                        player.Message( "SetInfo: PreviousRank for {0}&S set to {1}&",
                                        info.ClassyName,
                                        newPreviousRank.ClassyName );
                    } else if( newPreviousRank == null ) {
                        player.Message( "SetInfo: PreviousRank for {0}&S was reset (was {1}&S)",
                                        info.ClassyName,
                                        oldPreviousRank.ClassyName );
                    } else {
                        player.Message( "SetInfo: PreviousRank for {0}&S changed from {1}&S to {2}",
                                        info.ClassyName,
                                        oldPreviousRank.ClassyName,
                                        newPreviousRank.ClassyName );
                    }
                    break;

                case "totaltime":
                case "tt":
                    TimeSpan newTotalTime;
                    TimeSpan oldTotalTime = info.TotalTime;
                    if( valName.TryParseMiniTimeSpan( out newTotalTime ) ) {
                        if( newTotalTime > DateTimeUtil.MaxTimeSpan ) {
                            player.MessageMaxTimeSpan();
                            return;
                        }
                        info.TotalTime = newTotalTime;
                        player.Message( "SetInfo: TotalTime for {0}&S changed from {1} ({2}) to {3} ({4})",
                                        info.ClassyName,
                                        oldTotalTime.ToMiniString(),
                                        oldTotalTime.ToCompactString(),
                                        info.TotalTime.ToMiniString(),
                                        info.TotalTime.ToCompactString() );
                        break;
                    } else {
                        player.Message( "SetInfo: Could not parse value given for TotalTime." );
                        return;
                    }

                case "rankchangetype":
                case "rct":
                    RankChangeType oldType = info.RankChangeType;
                    RankChangeType newType;
                    if(!EnumUtil.TryParse(valName, out newType, true)){
                        player.Message( "SetInfo: Could not parse RankChangeType. Allowed values: {0}",
                                        String.Join( ", ", Enum.GetNames( typeof( RankChangeType ) ) ) );
                        return;
                    }
                    info.RankChangeType = newType;
                    player.Message( "SetInfo: RankChangeType for {0}&S changed from {1} to {2}",
                                    info.ClassyName,
                                    oldType,
                                    info.RankChangeType );
                    break;

                case "banreason":
                case "br":
                    if( valName.Length == 0 ) valName = null;
                    if( SetPlayerInfoField( player, "BanReason", info, info.BanReason, valName ) ) {
                        info.BanReason = valName;
                        break;
                    } else {
                        return;
                    }

                case "bandwidth":
                case "bw":
                    BandwidthUseMode oldMode = info.BandwidthUseMode;
                    BandwidthUseMode newMode;
                    if( !EnumUtil.TryParse( valName, out newMode, true ) ) {
                        player.Message( "SetInfo: Could not parse BandwidthUseMode. Allowed values: {0}",
                                        String.Join( ", ", Enum.GetNames( typeof( BandwidthUseMode ) ) ) );
                        return;
                    }
                    info.BandwidthUseMode = newMode;
                    player.Message( "SetInfo: BandwidthUseMode for {0}&S changed from {1} to {2}",
                                    info.ClassyName,
                                    oldMode, newMode );
                    break;

                case "unbanreason":
                case "ur":
                    if( valName.Length == 0 ) valName = null;
                    if( SetPlayerInfoField( player, "UnbanReason", info, info.UnbanReason, valName ) ) {
                        info.UnbanReason = valName;
                        break;
                    } else {
                        return;
                    }

                case "rankreason":
                case "rr":
                    if( valName.Length == 0 ) valName = null;
                    if( SetPlayerInfoField( player, "RankReason", info, info.RankChangeReason, valName ) ) {
                        info.RankChangeReason = valName;
                        break;
                    } else {
                        return;
                    }

                case "kickreason":
                case "kr":
                    if( valName.Length == 0 ) valName = null;
                    if( SetPlayerInfoField( player, "KickReason", info, info.LastKickReason, valName ) ) {
                        info.LastKickReason = valName;
                        break;
                    } else {
                        return;
                    }

                case "displayedname":
                case "dn":
                    string oldDisplayedName = info.DisplayedName;
                    if( valName.Length == 0 ) valName = null;

                    if( valName != null && ( valName.Contains( '\n' ) || valName.Contains( "&n" ) || valName.Contains( "&N" ) ) ) {
                        player.Message( "SetInfo: DisplayedName may not contain line breaks." );
                        return;
                    }

                    if( valName == info.DisplayedName ) {
                        if( valName == null ) {
                            player.Message( "SetInfo: DisplayedName for {0} is not set.",
                                            info.Name );
                        } else {
                            player.Message( "SetInfo: DisplayedName for {0} is already set to \"{1}&S\"",
                                            info.Name,
                                            valName );
                        }
                        return;
                    }
                    info.DisplayedName = valName;

                    if( oldDisplayedName == null ) {
                        player.Message( "SetInfo: DisplayedName for {0} set to \"{1}&S\"",
                                        info.Name,
                                        valName );
                    } else if( valName == null ) {
                        player.Message( "SetInfo: DisplayedName for {0} was reset (was \"{1}&S\")",
                                        info.Name,
                                        oldDisplayedName );
                    } else {
                        player.Message( "SetInfo: DisplayedName for {0} changed from \"{1}&S\" to \"{2}&S\"",
                                        info.Name,
                                        oldDisplayedName,
                                        valName );
                    }
                    break;

                case "name":
                case "n":
                    if( valName.Equals( info.Name, StringComparison.OrdinalIgnoreCase ) ) {
                        info.Name = valName;
                    } else {
                        player.Message( "SetInfo: Only capitalization changes are allowed in the name. " +
                                        "Type out the whole name ({0}) please.",
                                        info.Name );
                        return;
                    }
                    break;

                default:
                    player.Message( "Only the following properties are editable: " +
                                    "TimesKicked, PreviousRank, TotalTime, RankChangeType, " +
                                    "BanReason, UnbanReason, RankReason, KickReason, DisplayedName" );
                    return;
            }
            info.LastModified = DateTime.UtcNow;
        }

        static bool SetPlayerInfoField( [NotNull] Player player, [NotNull] string fieldName, [NotNull] PlayerInfo info,
                                        [CanBeNull] string oldValue, [CanBeNull] string newValue ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( fieldName == null ) throw new ArgumentNullException( "fieldName" );
            if( info == null ) throw new ArgumentNullException( "info" );
            if( newValue == oldValue ) {
                if( newValue == null ) {
                    player.Message( "SetInfo: {0} for {1}&S is not set.",
                                    fieldName, info.ClassyName );
                } else {
                    player.Message( "SetInfo: {0} for {1}&S is already set to \"{2}&S\"",
                                    fieldName, info.ClassyName, oldValue );
                }
                return false;
            }

            if( oldValue == null  ) {
                player.Message( "SetInfo: {0} for {1}&S set to \"{2}&S\"",
                                fieldName, info.ClassyName, newValue );
            } else if( newValue == null ) {
                player.Message( "SetInfo: {0} for {1}&S was reset (was \"{2}&S\")",
                                fieldName, info.ClassyName, oldValue );
            } else {
                player.Message( "SetInfo: {0} for {1}&S changed from \"{2}&S\" to \"{3}&S\"",
                                fieldName, info.ClassyName,
                                oldValue, newValue );
            }
            return true;
        }

        static bool ValidateInt( string stringVal, int min, int max ) {
            int val;
            if( Int32.TryParse( stringVal, out val ) ) {
                return (val >= min && val <= max);
            } else {
                return false;
            }
        }

        #endregion


        #region Reload

        static readonly CommandDescriptor CdReload = new CommandDescriptor {
            Name = "Reload",
            Aliases = new[] { "configreload", "reloadconfig", "autorankreload", "reloadautorank" },
            Category = CommandCategory.Maintenance,
            Permissions = new[] { Permission.ReloadConfig },
            IsConsoleSafe = true,
            Usage = "/Reload config/autorank/salt",
            Help = "Reloads a given configuration file or setting. "+
                   "Config note: changes to ranks and IRC settings still require a full restart. "+
                   "Salt note: Until server synchronizes with Minecraft.net, " +
                   "connecting players may have trouble verifying names.",
            Handler = ReloadHandler
        };

        static void ReloadHandler( Player player, CommandReader cmd ) {
            string whatToReload = cmd.Next();
            if( whatToReload == null ) {
                CdReload.PrintUsage( player );
                return;
            }

            whatToReload = whatToReload.ToLower();

            using( LogRecorder rec = new LogRecorder() ) {
                bool success;

                switch( whatToReload ) {
                    case "config":
                        try {
                            Config.Load( true, true );
                            success = true;
                        } catch( Exception ex ) {
                            Logger.LogAndReportCrash( "Config failed to reload", "ConfigGUI", ex, false );
                            success = false;
                        }
                        break;

                    case "autorank":
                        success = AutoRankManager.Init();
                        AutoRankManager.DoAutoRankAll( player, PlayerDB.PlayerInfoList, false, "(AutoRanked)" );
                        break;

                    case "salt":
                        Heartbeat.Salt = Server.GetRandomString( 32 );
                        player.Message( "&WNote: Until server synchronizes with Minecraft.net, " +
                                        "connecting players may have trouble verifying names." );
                        success = true;
                        break;

                    default:
                        CdReload.PrintUsage( player );
                        return;
                }

                if( rec.HasMessages ) {
                    foreach( string msg in rec.MessageList ) {
                        player.Message( msg );
                    }
                }

                if( success ) {
                    player.Message( "Reload: reloaded {0}.", whatToReload );
                } else {
                    player.Message( "&WReload: Error(s) occurred while reloading {0}.", whatToReload );
                }
            }
        }

        #endregion


        #region Shutdown, Restart

        static readonly CommandDescriptor CdShutdown = new CommandDescriptor {
            Name = "Shutdown",
            Category = CommandCategory.Maintenance,
            Permissions = new[] { Permission.ShutdownServer },
            IsConsoleSafe = true,
            Help = "Shuts down the server remotely after a given delay. " +
                   "A shutdown reason or message can be specified to be shown to players. " +
                   "Type &H/Shutdown abort&S to cancel.",
            Usage = "/Shutdown Delay [Reason]&S or &H/Shutdown abort",
            Handler = ShutdownHandler
        };

        static readonly TimeSpan DefaultShutdownTime = TimeSpan.FromSeconds( 5 );

        static void ShutdownHandler( Player player, CommandReader cmd ) {
            string delayString = cmd.Next();
            TimeSpan delayTime = DefaultShutdownTime;
            string reason = "";

            if( delayString != null ) {
                if( delayString.Equals( "abort", StringComparison.OrdinalIgnoreCase ) ) {
                    if( Server.CancelShutdown() ) {
                        Logger.Log( LogType.UserActivity,
                                    "Shutdown aborted by {0}.", player.Name );
                        Server.Message( "&WShutdown aborted by {0}", player.ClassyName );
                    } else {
                        player.MessageNow( "Cannot abort shutdown - too late." );
                    }
                    return;
                } else if( !delayString.TryParseMiniTimeSpan( out delayTime ) ) {
                    CdShutdown.PrintUsage( player );
                    return;
                }
                if( delayTime > DateTimeUtil.MaxTimeSpan ) {
                    player.MessageMaxTimeSpan();
                    return;
                }
                reason = cmd.NextAll();
            }

            if( delayTime.TotalMilliseconds > Int32.MaxValue - 1 ) {
                player.Message( "WShutdown: Delay is too long, maximum is {0}",
                                TimeSpan.FromMilliseconds( Int32.MaxValue - 1 ).ToMiniString() );
                return;
            }

            Server.Message( "&WServer shutting down in {0}", delayTime.ToMiniString() );

            if( String.IsNullOrEmpty( reason ) ) {
                Logger.Log( LogType.UserActivity,
                            "{0} scheduled a shutdown ({1} delay).",
                            player.Name, delayTime.ToCompactString() );
                ShutdownParams sp = new ShutdownParams( ShutdownReason.ShutdownCommand, delayTime, false );
                Server.Shutdown( sp, false );
            } else {
                Server.Message( "&SShutdown reason: {0}", reason );
                Logger.Log( LogType.UserActivity,
                            "{0} scheduled a shutdown ({1} delay). Reason: {2}",
                            player.Name, delayTime.ToCompactString(), reason );
                ShutdownParams sp = new ShutdownParams( ShutdownReason.ShutdownCommand, delayTime, false, reason, player );
                Server.Shutdown( sp, false );
            }
        }



        static readonly CommandDescriptor CdRestart = new CommandDescriptor {
            Name = "Restart",
            Category = CommandCategory.Maintenance,
            Permissions = new[] { Permission.ShutdownServer },
            IsConsoleSafe = true,
            Help = "Restarts the server remotely after a given delay. " +
                   "A restart reason or message can be specified to be shown to players. " +
                   "Type &H/Restart abort&S to cancel.",
            Usage = "/Restart Delay [Reason]&S or &H/Restart abort",
            Handler = RestartHandler
        };

        static void RestartHandler( Player player, CommandReader cmd ) {
            string delayString = cmd.Next();
            TimeSpan delayTime = DefaultShutdownTime;
            string reason = "";

            if( delayString != null ) {
                if( delayString.Equals( "abort", StringComparison.OrdinalIgnoreCase ) ) {
                    if( Server.CancelShutdown() ) {
                        Logger.Log( LogType.UserActivity,
                                    "Restart aborted by {0}.", player.Name );
                        Server.Message( "&WRestart aborted by {0}", player.ClassyName );
                    } else {
                        player.MessageNow( "Cannot abort restart - too late." );
                    }
                    return;
                } else if( !delayString.TryParseMiniTimeSpan( out delayTime ) ) {
                    CdShutdown.PrintUsage( player );
                    return;
                }
                if( delayTime > DateTimeUtil.MaxTimeSpan ) {
                    player.MessageMaxTimeSpan();
                    return;
                }
                reason = cmd.NextAll();
            }

            if( delayTime.TotalMilliseconds > Int32.MaxValue - 1 ) {
                player.Message( "Restart: Delay is too long, maximum is {0}",
                                TimeSpan.FromMilliseconds( Int32.MaxValue - 1 ).ToMiniString() );
                return;
            }

            Server.Message( "&WServer restarting in {0}", delayTime.ToMiniString() );

            if( String.IsNullOrEmpty( reason ) ) {
                Logger.Log( LogType.UserActivity,
                            "{0} scheduled a restart ({1} delay).",
                            player.Name, delayTime.ToCompactString() );
                ShutdownParams sp = new ShutdownParams( ShutdownReason.RestartCommand, delayTime, true );
                Server.Shutdown( sp, false );
            } else {
                Server.Message( "&WRestart reason: {0}", reason );
                Logger.Log( LogType.UserActivity,
                            "{0} scheduled a restart ({1} delay). Reason: {2}",
                            player.Name, delayTime.ToCompactString(), reason );
                ShutdownParams sp = new ShutdownParams( ShutdownReason.RestartCommand, delayTime, true, reason, player );
                Server.Shutdown( sp, false );
            }
        }

        #endregion


        #region PruneDB

        static readonly CommandDescriptor CdPruneDB = new CommandDescriptor {
            Name = "PruneDB",
            Category = CommandCategory.Maintenance,
            IsConsoleSafe = true,
            IsHidden = true,
            Permissions = new[] { Permission.EditPlayerDB },
            Help = "Removes inactive players from the player database. Use with caution.",
            Handler = PruneDBHandler
        };

        static void PruneDBHandler( Player player, CommandReader cmd ) {
            if( !cmd.IsConfirmed ) {
                player.MessageNow( "PruneDB: Finding inactive players..." );
                int inactivePlayers = PlayerDB.CountInactivePlayers();
                if( inactivePlayers == 0 ) {
                    player.Message( "PruneDB: No inactive players found." );
                } else {
                    Logger.Log( LogType.UserActivity,
                                "PruneDB: Asked {0} to confirm erasing {1} records.",
                                player.Name, inactivePlayers );
                    player.Confirm( cmd, "PruneDB: Erase {0} records of inactive players?",
                                    inactivePlayers );
                }
            } else {
                var task = Scheduler.NewBackgroundTask( PruneDBTask, player );
                task.IsCritical = true;
                task.RunOnce();
            }
        }


        static void PruneDBTask( SchedulerTask task ) {
            int removedCount = PlayerDB.RemoveInactivePlayers();
            Player player = (Player)task.UserState;
            player.Message( "PruneDB: Removed {0} inactive players!", removedCount );
        }

        #endregion


        #region Import / Export

        static readonly CommandDescriptor CdImport = new CommandDescriptor {
            Name = "Import",
            Aliases = new[] { "ImportBans", "ImportRanks" },
            Category = CommandCategory.Maintenance,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Import },
            Usage = "/Import Bans Software File&S or &H/Import Ranks Software File Rank",
            Help = "Imports data from formats used by other servers. " +
                   "Currently only MCSharp/MCZall/MCLawl/MCForge files are supported.",
            Handler = ImportHandler
        };

        static void ImportHandler( Player player, CommandReader cmd ) {
            string action = cmd.Next();
            if( action == null ) {
                CdImport.PrintUsage( player );
                return;
            }

            switch( action.ToLower() ) {
                case "bans":
                    if( !player.Can( Permission.Ban ) ) {
                        player.MessageNoAccess( Permission.Ban );
                        return;
                    }
                    ImportBans( player, cmd );
                    break;

                case "ranks":
                    if( !player.Can( Permission.Promote ) ) {
                        player.MessageNoAccess( Permission.Promote );
                        return;
                    }
                    ImportRanks( player, cmd );
                    break;

                default:
                    CdImport.PrintUsage( player );
                    break;
            }
        }


        static void ImportBans( Player player, CommandReader cmd ) {
            string serverName = cmd.Next();
            string fileName = cmd.Next();

            // Make sure all parameters are specified
            if( serverName == null || fileName == null ) {
                CdImport.PrintUsage( player );
                return;
            }

            // Check if file exists
            if( !File.Exists( fileName ) ) {
                player.Message( "File not found: {0}", fileName );
                return;
            }

            int playersBanned = 0,
                linesSkipped = 0,
                playersAlreadyBanned = 0;
            switch( serverName.ToLower() ) {
                case "mcsharp":
                case "mczall":
                case "mclawl":
                case "mcforge":
                    string[] names;
                    try {
                        names = File.ReadAllLines( fileName );
                    } catch( Exception ex ) {
                        player.Message( "Import: Could not open \"{0}\" to import bans.",
                                        fileName );
                        Logger.Log( LogType.Error,
                                    "ImportBans: Could not open \"{0}\": {1}",
                                    fileName, ex );
                        return;
                    }
                    if( !cmd.IsConfirmed ) {
                        Logger.Log( LogType.UserActivity,
                                    "Import: Asked {0} to confirm importing {1} bans from \"{2}\"",
                                    player.Name, names.Length, fileName );
                        player.Confirm( cmd, "Import: Import {0} bans?", names.Length );
                        return;
                    }

                    const string reason = "(imported from MCSharp)";
                    foreach( string name in names ) {
                        try {
                            IPAddress ip;
                            if( IPAddressUtil.IsIP( name ) && IPAddress.TryParse( name, out ip ) ) {
                                ip.BanIP( player, reason, true, true );
                            } else if( Player.IsValidPlayerName( name ) ) {
                                PlayerInfo info = PlayerDB.FindPlayerInfoExact( name ) ??
                                                  PlayerDB.CreateNewPlayerInfo( name, RankChangeType.Default );
                                info.Ban( player, reason, true, true );
                                playersBanned++;

                            } else {
                                linesSkipped++;
                            }
                        } catch( PlayerOpException ex ) {
                            if( ex.ErrorCode == PlayerOpExceptionCode.NoActionNeeded ) {
                                playersAlreadyBanned++;
                                continue;
                            }
                            Logger.Log( LogType.Warning, "ImportBans: " + ex.Message );
                            player.Message( ex.MessageColored );
                        }
                    }
                    PlayerDB.Save();
                    IPBanList.Save();
                    break;

                case "commandbook":
                    if( !fileName.EndsWith( ".csv", StringComparison.OrdinalIgnoreCase ) ) {
                        player.Message( "Import: Please provide bans.csv file for CommandBook" );
                        return;
                    }

                    string[] lines;
                    try {
                        lines = File.ReadAllLines( fileName );
                    } catch( Exception ex ) {
                        player.Message( "Import: Could not open \"{0}\" to import bans.",
                                        fileName );
                        Logger.Log( LogType.Error,
                                    "ImportBans: Could not open \"{0}\": {1}",
                                    fileName, ex );
                        return;
                    }
                    if( !cmd.IsConfirmed ) {
                        Logger.Log( LogType.UserActivity,
                                    "Import: Asked {0} to confirm importing {1} bans from \"{2}\"",
                                    player.Name, lines.Length, fileName );
                        player.Confirm( cmd, "Import: Import {0} bans?", lines.Length );
                        return;
                    }
                    for( int i = 0; i < lines.Length; i++ ) {
                        string[] record = ParseCsvRow( lines[i] );
                        if( record.Length != 5 ) {
                            linesSkipped++;
                            continue;
                        }
                        string playerName = record[0];
                        string banReason = String.Format( "{0} (imported from CommandBook on {1})",
                                                          record[2],
                                                          DateTime.UtcNow.ToCompactString() ).Trim();

                        PlayerInfo info = PlayerDB.FindPlayerInfoExact( playerName ) ??
                                          PlayerDB.CreateNewPlayerInfo( playerName, RankChangeType.Default );

                        try {
                            info.Ban( player, banReason, true, true );
                            playersBanned++;
                        } catch( PlayerOpException ex ) {
                            if( ex.ErrorCode == PlayerOpExceptionCode.NoActionNeeded ) {
                                playersAlreadyBanned++;
                                continue;
                            }
                            Logger.Log( LogType.Warning, "ImportBans: " + ex.Message );
                            player.Message( ex.MessageColored );
                            continue;
                        }

                        long timestamp;
                        if( record[3].Length > 1 && Int64.TryParse( record[3], out timestamp ) ) {
                            DateTime originalBanDate = DateTimeUtil.UnixEpoch.AddMilliseconds( timestamp );
                            info.BanDate = originalBanDate;
                        }
                    }
                    PlayerDB.Save();
                    IPBanList.Save();

                    break;

                default:
                    player.Message( "fCraft does not support importing from \"{0}\". " +
                                    "Only MCSharp and CommandBook ban lists are supported.",
                                    serverName );
                    return;
            }
            player.Message( "Import: Banned {0} players, found {1} already-banned players, skipped {2} lines.",
                            playersBanned, playersAlreadyBanned, linesSkipped );
        }


        // by Chris Wilson
        static string[] ParseCsvRow( string r ) {
            List<string> resp = new List<string>();
            bool cont = false;
            string cs = "";
            string[] c = r.Split( new[] { ',' }, StringSplitOptions.None );
            foreach( string y in c ) {
                string x = y;
                if( cont ) {
                    // End of field
                    if( x.EndsWith( "\"" ) ) {
                        cs += "," + x.Substring( 0, x.Length - 1 );
                        resp.Add( cs );
                        cs = "";
                        cont = false;
                        continue;

                    } else {
                        // Field still not ended
                        cs += "," + x;
                        continue;
                    }
                }
                // Fully encapsulated with no comma within
                if( x.StartsWith( "\"" ) && x.EndsWith( "\"" ) ) {
                    if( (x.EndsWith( "\"\"" ) && !x.EndsWith( "\"\"\"" )) && x != "\"\"" ) {
                        cont = true;
                        cs = x;
                        continue;
                    }
                    resp.Add( x.Substring( 1, x.Length - 2 ) );
                    continue;
                }
                // Start of encapsulation but comma has split it into at least next field
                if( x.StartsWith( "\"" ) && !x.EndsWith( "\"" ) ) {
                    cont = true;
                    cs += x.Substring( 1 );
                    continue;
                }
                // Non encapsulated complete field
                resp.Add( x );
            }
            return resp.ToArray();
        }


        static void ImportRanks( Player player, CommandReader cmd ) {
            string serverName = cmd.Next();
            string fileName = cmd.Next();
            string rankName = cmd.Next();
            bool silent = (cmd.Next() != null);


            // Make sure all parameters are specified
            if( serverName == null || fileName == null || rankName == null ) {
                CdImport.PrintUsage( player );
                return;
            }

            // Check if file exists
            if( !File.Exists( fileName ) ) {
                player.Message( "File not found: {0}", fileName );
                return;
            }

            Rank targetRank = RankManager.FindRank( rankName );
            if( targetRank == null ) {
                player.MessageNoRank( rankName );
                return;
            }

            string[] names;

            switch( serverName.ToLowerInvariant() ) {
                case "mcsharp":
                case "mczall":
                case "mclawl":
                case "mcforge":
                    try {
                        names = File.ReadAllLines( fileName );
                    } catch( Exception ex ) {
                        Logger.Log( LogType.Error,
                                    "Could not open \"{0}\" to import ranks: {1}",
                                    fileName, ex );
                        return;
                    }
                    break;
                default:
                    player.Message( "fCraft does not support importing from {0}", serverName );
                    return;
            }

            if( !cmd.IsConfirmed ) {
                Logger.Log( LogType.UserActivity,
                            "Import: Asked {0} to confirm importing {1} ranks from {2}",
                            player.Name, names.Length, fileName );
                player.Confirm( cmd, "Import {0} player ranks?", names.Length );
                return;
            }

            string reason = "(Import from " + serverName + ")";
            foreach( string name in names ) {
                try {
                    PlayerInfo info = PlayerDB.FindPlayerInfoExact( name ) ??
                                      PlayerDB.CreateNewPlayerInfo( name, RankChangeType.Promoted );
                    try {
                        info.ChangeRank( player, targetRank, reason, !silent, true, false );
                    } catch( PlayerOpException ex ) {
                        player.Message( ex.MessageColored );
                    }
                } catch( PlayerOpException ex ) {
                    Logger.Log( LogType.Warning, "ImportRanks: " + ex.Message );
                    player.Message( ex.MessageColored );
                }
            }

            PlayerDB.Save();
        }


        static readonly CommandDescriptor CdExport = new CommandDescriptor {
            Name = "Export",
            Category = CommandCategory.Maintenance,
            IsHidden = true,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Import },
            Usage = "/Export Bans FileName&S or &H/Export Ranks FileName RankName",
            Handler = ExportHandler
        };

        static void ExportHandler( [NotNull] Player player, [NotNull] CommandReader cmd ) {
            string actionType = cmd.Next();
            string fileName = cmd.Next();
            if( actionType == null || fileName == null ) {
                CdExport.PrintUsage( player );
                return;
            }

            // Make sure the given filename is valid
            if( !Paths.IsValidPath( fileName ) ) {
                player.Message( "Export: Unacceptable filename given: \"{0}\"", fileName );
                return;
            }

            // Make sure that the target file is legit
            if( !Paths.Contains( Paths.WorkingPath, fileName ) ) {
                Logger.Log( LogType.SuspiciousActivity,
                            "Export: Player {0} tried to export to \"{1}\"",
                            player.Name,
                            fileName );
                player.MessageUnsafePath();
                return;
            }

            IEnumerable<PlayerInfo> playerList;
            if( "Ranks".Equals( actionType, StringComparison.OrdinalIgnoreCase ) ) {
                // Read and check the rank name
                string rankName = cmd.Next();
                if( rankName == null ) {
                    CdExport.PrintUsage( player );
                    return;
                }
                Rank rank = RankManager.FindRank( rankName );
                if( rank == null ) {
                    player.MessageNoRank( rankName );
                    return;
                }

                // Get a list of players of given rank
                playerList = PlayerDB.PlayerInfoList.Where( p => p.Rank == rank );

            } else if( "Bans".Equals( actionType, StringComparison.OrdinalIgnoreCase ) ) {
                // Get a list of banned players
                playerList = PlayerDB.PlayerInfoList
                                            .Where( p => p.BanStatus == BanStatus.Banned );

            } else {
                player.Message( "Export: Action must be \"ranks\" or \"bans\"." );
                CdExport.PrintUsage( player );
                return;
            }

            // If file already exists, require confirmation
            if( !cmd.IsConfirmed && File.Exists( fileName ) ) {
                Logger.Log( LogType.UserActivity,
                            "Export: Asked {0} to confirm overwriting \"{1}\"",
                            player.Name,
                            fileName );
                player.Confirm( cmd, "Export: File \"{0}\" already exists. Overwrite?", fileName );
                return;
            }

            // Save the list to file. If file is not writable, explodes!
            int playerCount = 0;
            using( StreamWriter writer = new StreamWriter( fileName ) ) {
                foreach( PlayerInfo info in playerList ) {
                    writer.WriteLine( info.Name );
                    playerCount++;
                }
            }

            // report success
            player.Message( "Export: Written {0} names to \"{1}\"",
                            playerCount, Path.GetFileName( fileName ) );
        }

        #endregion


        static readonly CommandDescriptor CdInfoSwap = new CommandDescriptor {
            Name = "InfoSwap",
            Category = CommandCategory.Maintenance,
            IsConsoleSafe = true,
            IsHidden = true,
            Permissions = new[] { Permission.EditPlayerDB },
            Usage = "/InfoSwap Player1 Player2",
            Help = "Swaps stats of two players in the database. Both players should be offline.",
            Handler = InfoSwapHandler
        };

        static void InfoSwapHandler( Player player, CommandReader cmd ) {
            string p1Name = cmd.Next();
            string p2Name = cmd.Next();
            if( p1Name == null || p2Name == null ) {
                CdInfoSwap.PrintUsage( player );
                return;
            }

            PlayerInfo p1 = PlayerDB.FindPlayerInfoOrPrintMatches( player, p1Name, SearchOptions.IncludeSelf );
            if( p1 == null ) return;
            PlayerInfo p2 = PlayerDB.FindPlayerInfoOrPrintMatches( player, p2Name, SearchOptions.IncludeSelf );
            if( p2 == null ) return;

            if( p1 == p2 ) {
                player.Message( "InfoSwap: Please specify 2 different players." );
                return;
            }

            if( p1.IsOnline || p2.IsOnline ) {
                player.Message( "InfoSwap: Both players must be offline to swap info." );
                return;
            }

            if( !cmd.IsConfirmed ) {
                Logger.Log( LogType.UserActivity,
                            "InfoSwap: Asked {0} to confirm swapping stats of players {1} and {2}",
                            player.Name, p1.Name, p2.Name );
                player.Confirm( cmd, "InfoSwap: Swap stats of players {0}&S and {1}&S?",
                                     p1.ClassyName, p2.ClassyName );
            } else {
                PlayerDB.SwapPlayerInfo( p1, p2 );
                Logger.Log( LogType.UserActivity,
                            "Player {0} swapped stats of players {1} and {2}",
                            player.Name, p1.Name, p2.Name );
                player.Message( "InfoSwap: Stats of {0}&S and {1}&S have been swapped.",
                                p1.ClassyName, p2.ClassyName );
            }
        }
    }
}