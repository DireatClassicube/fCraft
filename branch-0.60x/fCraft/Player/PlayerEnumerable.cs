﻿// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;

namespace fCraft {
    public static class PlayerEnumerable {

        #region Rank Filters

        /// <summary> Filters a collection of players, leaving only those of the given rank. </summary>
        /// <param name="source"> Original collection of players. Will not get modified. </param>
        /// <param name="rank"> Desired rank. </param>
        /// <returns> Filtered collection of players. </returns>
        public static IEnumerable<Player> Ranked( this IEnumerable<Player> source, Rank rank ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( rank == null ) throw new ArgumentNullException( "rank" );
            foreach( Player player in source ) {
                if( player.Info.Rank == rank ) {
                    yield return player;
                }
            }
        }


        /// <summary> Filters a collection of players, leaving only those NOT of the given rank. </summary>
        /// <param name="source"> Original collection of players. Will not get modified. </param>
        /// <param name="rank"> Undesired rank. </param>
        /// <returns> Filtered collection of players. </returns>
        public static IEnumerable<Player> NotRanked( this IEnumerable<Player> source, Rank rank ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( rank == null ) throw new ArgumentNullException( "rank" );
            foreach( Player player in source ) {
                if( player.Info.Rank != rank ) {
                    yield return player;
                }
            }
        }


        /// <summary> Filters a collection of players, leaving only those above the given rank. </summary>
        /// <param name="source"> Original collection of players. Will not get modified. </param>
        /// <param name="minRank"> All ranks above this one will be kept. This and lower ranks will be filtered out. </param>
        /// <returns> Filtered collection of players. </returns>
        public static IEnumerable<Player> RankedAbove( this IEnumerable<Player> source, Rank minRank ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( minRank == null ) throw new ArgumentNullException( "minRank" );
            foreach( Player player in source ) {
                if( player.Info.Rank > minRank ) {
                    yield return player;
                }
            }
        }


        /// <summary> Filters a collection of players, leaving only those of or above the given rank. </summary>
        /// <param name="source"> Original collection of players. Will not get modified. </param>
        /// <param name="minRank"> Minimum desired rank. </param>
        /// <returns> Filtered collection of players. </returns>
        public static IEnumerable<Player> RankedAtLeast( this IEnumerable<Player> source, Rank minRank ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( minRank == null ) throw new ArgumentNullException( "minRank" );
            foreach( Player player in source ) {
                if( player.Info.Rank >= minRank ) {
                    yield return player;
                }
            }
        }


        /// <summary> Filters a collection of players, leaving only those below the given rank. </summary>
        /// <param name="source"> Original collection of players. Will not get modified. </param>
        /// <param name="maxRank"> All ranks below this one will be kept. This and higher ranks will be filtered out. </param>
        /// <returns> Filtered collection of players. </returns>
        public static IEnumerable<Player> RankedBelow( this IEnumerable<Player> source, Rank maxRank ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( maxRank == null ) throw new ArgumentNullException( "maxRank" );
            foreach( Player player in source ) {
                if( player.Info.Rank < maxRank ) {
                    yield return player;
                }
            }
        }


        /// <summary> Filters a collection of players, leaving only those of or below the given rank. </summary>
        /// <param name="source"> Original collection of players. Will not get modified. </param>
        /// <param name="maxRank"> Maximum desired rank. </param>
        /// <returns> Filtered collection of players. </returns>
        public static IEnumerable<Player> RankedAtMost( this IEnumerable<Player> source, Rank maxRank ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( maxRank == null ) throw new ArgumentNullException( "maxRank" );
            foreach( Player player in source ) {
                if( player.Info.Rank <= maxRank ) {
                    yield return player;
                }
            }
        }

        #endregion


        #region Permissions

        /// <summary> Filters a collection of players, leaving only those who have the given permission. </summary>
        /// <param name="source"> Original collection of players. Will not get modified. </param>
        /// <param name="permission"> Permission that players are required to have. </param>
        /// <returns> Filtered collection of players. </returns>
        public static IEnumerable<Player> Can( this IEnumerable<Player> source, Permission permission ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            foreach( Player player in source ) {
                if( player.Can( permission ) ) {
                    yield return player;
                }
            }
        }


        /// <summary> Filters a collection of players, leaving only those who have the given permission,
        /// and with permission limits allowing operation on the given rank. </summary>
        /// <param name="source"> Original collection of players. Will not get modified. </param>
        /// <param name="permission"> Permission that players are required to have. </param>
        /// <param name="affectedRank"> Permission limit will be checked against this rank. </param>
        /// <returns> Filtered collection of players. </returns>
        public static IEnumerable<Player> Can( this IEnumerable<Player> source, Permission permission, Rank affectedRank ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( affectedRank == null ) throw new ArgumentNullException( "affectedRank" );
            foreach( Player player in source ) {
                if( player.Can( permission, affectedRank ) ) {
                    yield return player;
                }
            }
        }


        /// <summary> Filters a collection of players, leaving only those who do NOT have the given permission. </summary>
        /// <param name="source"> Original collection of players. Will not get modified. </param>
        /// <param name="permission"> Permission that players are required to NOT have. </param>
        /// <returns> Filtered collection of players. </returns>
        public static IEnumerable<Player> Cant( this IEnumerable<Player> source, Permission permission ) {
            foreach( Player player in source ) {
                if( !player.Can( permission ) ) {
                    yield return player;
                }
            }
        }


        /// <summary> Filters a collection of players, leaving only those who do NOT have the given permission,
        /// or with permission limits NOT allowing operation on the given rank. </summary>
        /// <param name="source"> Original collection of players. Will not get modified. </param>
        /// <param name="permission"> Permission that players are required to NOT have. </param>
        /// <param name="affectedRank"> Permission limit will be checked against this rank. </param>
        /// <returns> Filtered collection of players. </returns>
        public static IEnumerable<Player> Cant( this IEnumerable<Player> source, Permission permission, Rank affectedRank ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( affectedRank == null ) throw new ArgumentNullException( "affectedRank" );
            foreach( Player player in source ) {
                if( !player.Can( permission, affectedRank ) ) {
                    yield return player;
                }
            }
        }


        /// <summary> Filters a collection of players, leaving only those who can see the target. </summary>
        /// <param name="source"> Original collection of players. Will not get modified. </param>
        /// <param name="targetPlayer"> Player whose visibility is being tested. </param>
        /// <returns> Filtered collection of players. </returns>
        public static IEnumerable<Player> CanSee( this IEnumerable<Player> source, Player targetPlayer ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( targetPlayer == null ) throw new ArgumentNullException( "targetPlayer" );
            foreach( Player player in source ) {
                if( player != targetPlayer && player.CanSee( targetPlayer ) ) {
                    yield return player;
                }
            }
        }


        /// <summary> Filters a collection of players, leaving only those who can NOT see the target. </summary>
        /// <param name="source"> Original collection of players. Will not get modified. </param>
        /// <param name="targetPlayer"> Player whose visibility is being tested. </param>
        /// <returns> Filtered collection of players. </returns>
        public static IEnumerable<Player> CantSee( this IEnumerable<Player> source, Player targetPlayer ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( targetPlayer == null ) throw new ArgumentNullException( "targetPlayer" );
            foreach( Player player in source ) {
                if( player != targetPlayer && !player.CanSee( targetPlayer ) ) {
                    yield return player;
                }
            }
        }


        /// <summary> Filters a collection of players, leaving only those who can be seen by the given player. </summary>
        /// <param name="source"> Original collection of players. Will not get modified. </param>
        /// <param name="observer"> Player whose vision is being tested. </param>
        /// <returns> Filtered collection of players. </returns>
        public static IEnumerable<Player> CanBeSeen( this IEnumerable<Player> source, Player observer ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( observer == null ) throw new ArgumentNullException( "observer" );
            foreach( Player player in source ) {
                if( player != observer && observer.CanSee( player ) ) {
                    yield return player;
                }
            }
        }


        /// <summary> Filters a collection of players, leaving only those who can NOT be seen by the given player. </summary>
        /// <param name="source"> Original collection of players. Will not get modified. </param>
        /// <param name="observer"> Player whose vision is being tested. </param>
        /// <returns> Filtered collection of players. </returns>
        public static IEnumerable<Player> CantBeSeen( this IEnumerable<Player> source, Player observer ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( observer == null ) throw new ArgumentNullException( "observer" );
            foreach( Player player in source ) {
                if( player != observer && !observer.CanSee( player ) ) {
                    yield return player;
                }
            }
        }

        #endregion


        #region Ignore

        /// <summary> Filters a collection of players, leaving only those who are ignoring the given player. </summary>
        /// <param name="source"> Original collection of players. Will not get modified. </param>
        /// <param name="player"> Player whose ignore standing is being checked. </param>
        /// <returns> Filtered collection of players. </returns>
        public static IEnumerable<Player> Ignoring( this IEnumerable<Player> source, Player player ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( player == null ) throw new ArgumentNullException( "player" );
            foreach( Player otherPlayer in source ) {
                if( otherPlayer.IsIgnoring( player.Info ) ) {
                    yield return otherPlayer;
                }
            }
        }


        /// <summary> Filters a collection of players, leaving only those who are NOT ignoring the given player. </summary>
        /// <param name="source"> Original collection of players. Will not get modified. </param>
        /// <param name="player"> Player whose ignore standing is being checked. </param>
        /// <returns> Filtered collection of players. </returns>
        public static IEnumerable<Player> NotIgnoring( this IEnumerable<Player> source, Player player ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( player == null ) throw new ArgumentNullException( "player" );
            foreach( Player otherPlayer in source ) {
                if( !otherPlayer.IsIgnoring( player.Info ) ) {
                    yield return otherPlayer;
                }
            }
        }


        /// <summary> Filters a collection of players, leaving only those who are ignoring the given player. </summary>
        /// <param name="source"> Original collection of players. Will not get modified. </param>
        /// <param name="playerInfo"> Player whose ignore standing is being checked. </param>
        /// <returns> Filtered collection of players. </returns>
        public static IEnumerable<Player> Ignoring( this IEnumerable<Player> source, PlayerInfo playerInfo ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( playerInfo == null ) throw new ArgumentNullException( "playerInfo" );
            foreach( Player otherPlayer in source ) {
                if( otherPlayer.IsIgnoring( playerInfo ) ) {
                    yield return otherPlayer;
                }
            }
        }


        /// <summary> Filters a collection of players, leaving only those who are NOT ignoring the given player. </summary>
        /// <param name="source"> Original collection of players. Will not get modified. </param>
        /// <param name="playerInfo"> Player whose ignore standing is being checked. </param>
        /// <returns> Filtered collection of players. </returns>
        public static IEnumerable<Player> NotIgnoring( this IEnumerable<Player> source, PlayerInfo playerInfo ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( playerInfo == null ) throw new ArgumentNullException( "playerInfo" );
            foreach( Player otherPlayer in source ) {
                if( !otherPlayer.IsIgnoring( playerInfo ) ) {
                    yield return otherPlayer;
                }
            }
        }


        /// <summary> Filters a collection of players, leaving only those who are ignored by the given player. </summary>
        /// <param name="source"> Original collection of players. Will not get modified. </param>
        /// <param name="ignorer"> Player whose disposition is being checked. </param>
        /// <returns> Filtered collection of players. </returns>
        public static IEnumerable<Player> IgnoredBy( this IEnumerable<Player> source, Player ignorer ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( ignorer == null ) throw new ArgumentNullException( "ignorer" );
            foreach( Player otherPlayer in source ) {
                if( ignorer.IsIgnoring( otherPlayer.Info ) ) {
                    yield return otherPlayer;
                }
            }
        }


        /// <summary> Filters a collection of players, leaving only those who are NOT ignored by the given player. </summary>
        /// <param name="source"> Original collection of players. Will not get modified. </param>
        /// <param name="ignorer"> Player whose disposition is being checked. </param>
        /// <returns> Filtered collection of players. </returns>
        public static IEnumerable<Player> NotIgnoredBy( this IEnumerable<Player> source, Player ignorer ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( ignorer == null ) throw new ArgumentNullException( "ignorer" );
            foreach( Player otherPlayer in source ) {
                if( !ignorer.IsIgnoring( otherPlayer.Info ) ) {
                    yield return otherPlayer;
                }
            }
        }

        #endregion


        #region Worlds

        /// <summary> Filters a collection of players, leaving only those who are currently located on the given world. </summary>
        /// <param name="source"> Original collection of players. Will not get modified. </param>
        /// <param name="world"> World that players are desired to be on. </param>
        /// <returns> Filtered collection of players. </returns>
        public static IEnumerable<Player> InWorld( this IEnumerable<Player> source, World world ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( world == null ) throw new ArgumentNullException( "world" );
            foreach( Player player in source ) {
                if( player.World == world ) {
                    yield return player;
                }
            }
        }


        /// <summary> Filters a collection of players, leaving only those who are currently NOT located on the given world. </summary>
        /// <param name="source"> Original collection of players. Will not get modified. </param>
        /// <param name="world"> World that players are desired to NOT be on. </param>
        /// <returns> Filtered collection of players. </returns>
        public static IEnumerable<Player> NotInWorld( this IEnumerable<Player> source, World world ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( world == null ) throw new ArgumentNullException( "world" );
            foreach( Player player in source ) {
                if( player.World != world ) {
                    yield return player;
                }
            }
        }

        #endregion


        #region Personal Inclusion / Exclusion
        /// <summary> Adds players to the given set.
        /// ]If the given sequence of players already contains player, no duplicate is added.
        /// Precisely speaking, produces the set union of a given collection of players and a given player. </summary>
        /// <param name="source"> Original set of players. Will not get modified. </param>
        /// <param name="includedPlayer"> Player to add to the set. </param>
        /// <returns> A set that contains all players in the input sequence, plus the given player. </returns>
        public static IEnumerable<Player> Union( this IEnumerable<Player> source, Player includedPlayer ) {
            bool found = false;
            foreach( Player player in source ) {
                yield return player;
                if( player == includedPlayer ) {
                    found = true;
                }
            }
            if( !found ) {
                yield return includedPlayer;
            }
        }

        
        /// <summary> Removes player from the given set.
        /// Precisely speaking, produces the set difference between the given collection of players and a given player. </summary>
        /// <param name="source"> Original set of players. Will not get modified. </param>
        /// <param name="excludedPlayer"> Player to remove from the set. </param>
        /// <returns> A set that contains all players in the input sequence, minus the given player. </returns>
        public static IEnumerable<Player> Except( this IEnumerable<Player> source, Player excludedPlayer ) {
            foreach( Player player in source ) {
                if( player != excludedPlayer ) {
                    yield return player;
                }
            }
        }

        #endregion


        #region Messaging

        /// <summary> Broadcasts a message. </summary>
        /// <param name="source"> List of players who will receive the message. </param>
        /// <param name="message"> String/message to send. </param>
        /// <returns> Number of players who received the message. </returns>
        public static int Message( this IEnumerable<Player> source, string message ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( message == null ) throw new ArgumentNullException( "message" );
            int i = 0;
            foreach( Packet packet in LineWrapper.Wrap( message ) ) {
                foreach( Player player in source ) {
                    player.Send( packet );
                    i++;
                }
            }
            return i;
        }


        /// <summary> Formats and broadcasts a message. </summary>
        /// <param name="source"> List of players who will receive the message. </param>
        /// <param name="message"> String/message to send. </param>
        /// <param name="formatArgs"> Format parameters. Same semantics as String.Format </param>
        /// <returns> Number of players who received the message. </returns>
        public static int Message( this IEnumerable<Player> source, string message, params object[] formatArgs ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( message == null ) throw new ArgumentNullException( "message" );
            if( formatArgs == null ) throw new ArgumentNullException( "formatArgs" );
            int i = 0;
            foreach( Packet packet in LineWrapper.Wrap( String.Format( message, formatArgs ) ) ) {
                foreach( Player player in source ) {
                    player.Send( packet );
                    i++;
                }
            }
            return i;
        }


        /// <summary> Broadcasts a message, prefixing wrapped lines. </summary>
        /// <param name="source"> List of players who will receive the message. </param>
        /// <param name="prefix"> Prefix to prepend to prepend to each line after the 1st,
        /// if any line-wrapping occurs. Does NOT get prepended to first line. </param>
        /// <param name="message"> String/message to send. </param>
        /// <returns> Number of players who received the message. </returns>
        public static int MessagePrefixed( this IEnumerable<Player> source, string prefix, string message ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( prefix == null ) throw new ArgumentNullException( "prefix" );
            if( message == null ) throw new ArgumentNullException( "message" );
            int i = 0;
            foreach( Packet packet in LineWrapper.WrapPrefixed( prefix, message ) ) {
                foreach( Player player in source ) {
                    player.Send( packet );
                    i++;
                }
            }
            return i;
        }


        /// <summary> Formats and broadcasts a message, prefixing wrapped lines. </summary>
        /// <param name="source"> List of players who will receive the message. </param>
        /// <param name="prefix"> Prefix to prepend to prepend to each line after the 1st,
        /// if any line-wrapping occurs. Does NOT get prepended to first line. </param>
        /// <param name="message"> String/message to send. </param>
        /// <param name="formatArgs"> Format parameters. Same semantics as String.Format </param>
        /// <returns> Number of players who received the message. </returns>
        public static int MessagePrefixed( this IEnumerable<Player> source, string prefix, string message, params object[] formatArgs ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            if( message == null ) throw new ArgumentNullException( "message" );
            if( prefix == null ) throw new ArgumentNullException( "prefix" );
            if( formatArgs == null ) throw new ArgumentNullException( "formatArgs" );
            int i = 0;
            foreach( Packet packet in LineWrapper.WrapPrefixed( prefix, String.Format( message, formatArgs ) ) ) {
                foreach( Player player in source ) {
                    player.Send( packet );
                    i++;
                }
            }
            return i;
        }

        #endregion


        #region Packet Sending

        public static int Send( this IEnumerable<Player> source, Packet packet ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            int i = 0;
            foreach( Player player in source ) {
                player.Send( packet );
                i++;
            }
            return i;
        }

        public static int SendLowPriority( this IEnumerable<Player> source, Packet packet ) {
            if( source == null ) throw new ArgumentNullException( "source" );
            int i = 0;
            foreach( Player player in source ) {
                player.SendLowPriority( packet );
                i++;
            }
            return i;
        }

        #endregion
    }
}