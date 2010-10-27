﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.IO;

namespace fCraft {
    public static class AutoRank {
        const string AutoRankFile = "autorank.xml";
        static List<Criterion> criteria = new List<Criterion>();


        public static void Add( Criterion criterion ) {
            criteria.Add( criterion );
        }


        public static Rank Check( PlayerInfo info ) {
            foreach( Criterion c in criteria ) {
                if( c.FromRank == info.rank && !info.banned && c.Condition.Eval( info ) ) {
                    return c.ToRank;
                }
            }
            return null;
        }


        public static void Init() {
            criteria.Clear();
            if( File.Exists( AutoRankFile ) ) {
                try {
                    XDocument doc = XDocument.Load( AutoRankFile );
                    foreach( XElement el in doc.Root.Elements( "Criterion" ) ) {
                        try {
                            Add( new Criterion( el ) );
                        } catch( Exception ex ) {
                            Logger.Log( "AutoRank.Init: Could not parse an AutoRank criterion: {0}", LogType.Error, ex );
                        }
                    }
                    if( criteria.Count == 0 ) {
                        Logger.Log( "AutoRank.Init: No criteria loaded.", LogType.Warning );
                    }
                } catch( Exception ex ) {
                    Logger.Log( "AutoRank.Init: Could not parse the AutoRank file: {0}", LogType.Error, ex );
                }
            } else {
                Logger.Log( "AutoRank.Init: autorank.xml not found. No criteria loaded.", LogType.Warning );
            }
        }
    }


    public class Criterion {
        public CriterionType Type { get; set; }
        public Rank FromRank { get; set; }
        public Rank ToRank { get; set; }
        public Condition Condition { get; set; }

        public Criterion() { }

        public Criterion( CriterionType _type, Rank _fromRank, Rank _toRank, Condition _condition ) {
            Type = _type;
            FromRank = _fromRank;
            ToRank = _toRank;
            Condition = _condition;
        }

        public Criterion( XElement el ) {
            Type = (CriterionType)Enum.Parse( typeof( CriterionType ), el.Attribute( "type" ).Value, true );
            FromRank = RankList.ParseRank( el.Attribute( "fromRank" ).Value );
            ToRank = RankList.ParseRank( el.Attribute( "toRank" ).Value );
            if( el.Elements().Count() == 1 ) {
                Condition = Condition.Parse( el.Elements().First() );
            } else if( el.Elements().Count() > 1 ) {
                ConditionAND cand = new ConditionAND();
                foreach( XElement cond in el.Elements() ) {
                    cand.Add( Condition.Parse( cond ) );
                }
                Condition = cand;
            } else {
                throw new FormatException( "At least one condition required." );
            }
        }

        public XElement Serialize() {
            XElement el = new XElement( "Criterion" );
            el.Add( new XAttribute( "type", Type ) );
            el.Add( new XAttribute( "fromRank", FromRank ) );
            el.Add( new XAttribute( "toRank", ToRank ) );
            if( Condition != null ) {
                el.Add( Condition.Serialize() );
            }
            return el;
        }
    }


    #region Conditions

    // Base class for all conditions
    public abstract class Condition {
        public Condition() { }
        public abstract bool Eval( PlayerInfo info );

        public static Condition Parse( XElement el ) {
            if( el.Name == "AND" ) {
                return new ConditionAND( el );
            } else if( el.Name == "OR" ) {
                return new ConditionOR( el );
            } else if( el.Name == "NOR" ) {
                return new ConditionNOR( el );
            } else if( el.Name == "NAND" ) {
                return new ConditionNAND( el );
            } else if( el.Name == "ConditionIntRange" ) {
                return new ConditionIntRange( el );
            } else if( el.Name == "ConditionRankChangeType" ) {
                return new ConditionRankChangeType( el );
            } else if( el.Name == "ConditionPreviousRank" ) {
                return new ConditionPreviousRank( el );
            } else {
                return null;
            }
        }

        public abstract XElement Serialize();
    }

    // range checks on countable PlayerInfo fields
    public sealed class ConditionIntRange : Condition {
        public ConditionField Field;
        public ConditionScopeType Scope = ConditionScopeType.Total;
        public ComparisonOperation Comparison = ComparisonOperation.eq;
        public int Value;

        public ConditionIntRange() { }

        public ConditionIntRange( XElement el ) {
            Field = (ConditionField)Enum.Parse( typeof( ConditionField ), el.Attribute( "field" ).Value, true );
            Value = Int32.Parse( el.Attribute( "val" ).Value );
            if( el.Attribute( "op" ) != null ) {
                Comparison = (ComparisonOperation)Enum.Parse( typeof( ComparisonOperation ), el.Attribute( "op" ).Value, true );
            }
            if( el.Attribute( "scope" ) != null ) {
                Scope = (ConditionScopeType)Enum.Parse( typeof( ConditionScopeType ), el.Attribute( "scope" ).Value, true );
            }
        }

        public ConditionIntRange( ConditionField _field, ComparisonOperation _comparison, int _value ) {
            this.Field = _field;
            this.Comparison = _comparison;
            this.Value = _value;
        }

        public override bool Eval( PlayerInfo info ) {
            long givenValue;
            switch( Field ) {
                case ConditionField.TimeSinceFirstLogin:
                    givenValue = (int)DateTime.Now.Subtract( info.firstLoginDate ).TotalSeconds;
                    break;
                case ConditionField.TimeSinceLastLogin:
                    givenValue = (int)DateTime.Now.Subtract( info.lastLoginDate ).TotalSeconds;
                    break;
                case ConditionField.LastSeen:
                    givenValue = (int)DateTime.Now.Subtract( info.lastSeen ).TotalSeconds;
                    break;
                case ConditionField.BlocksBuilt:
                    givenValue = info.blocksBuilt;
                    break;
                case ConditionField.BlocksDeleted:
                    givenValue = info.blocksDeleted;
                    break;
                case ConditionField.BlocksChanged:
                    givenValue = info.blocksBuilt + info.blocksDeleted;
                    break;
                case ConditionField.BlocksDrawn:
                    givenValue = info.blocksDrawn;
                    break;
                case ConditionField.TimesVisited:
                    givenValue = info.timesVisited;
                    break;
                case ConditionField.MessagesWritten:
                    givenValue = info.linesWritten;
                    break;
                case ConditionField.TimesKicked:
                    givenValue = info.timesKicked;
                    break;
                case ConditionField.TotalTime:
                    givenValue = (int)info.totalTime.TotalSeconds;
                    break;
                case ConditionField.TimeSinceRankChange:
                    givenValue = (int)DateTime.Now.Subtract( info.rankChangeDate ).TotalSeconds;
                    break;
                case ConditionField.TimeSinceLastKick:
                    givenValue = (int)DateTime.Now.Subtract( info.lastKickDate ).TotalSeconds;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch( this.Comparison ) {
                case ComparisonOperation.lt:
                    return (givenValue < Value);
                case ComparisonOperation.lte:
                    return (givenValue <= Value);
                case ComparisonOperation.gte:
                    return (givenValue >= Value);
                case ComparisonOperation.gt:
                    return (givenValue > Value);
                case ComparisonOperation.eq:
                    return (givenValue == Value);
                case ComparisonOperation.neq:
                    return (givenValue != Value);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override XElement Serialize() {
            XElement el = new XElement( "ConditionIntRange" );
            el.Add( new XAttribute( "field", Field.ToString() ) );
            el.Add( new XAttribute( "val", Value.ToString() ) );
            el.Add( new XAttribute( "op", Comparison.ToString() ) );
            el.Add( new XAttribute( "scope", Scope.ToString() ) );
            return el;
        }
    }


    // check RankStatus
    public sealed class ConditionRankChangeType : Condition {
        public RankChangeType Type;

        public ConditionRankChangeType( RankChangeType _type ) {
            this.Type = _type;
        }

        public ConditionRankChangeType( XElement el ) {
            Type = (RankChangeType)Enum.Parse( typeof( RankChangeType ), el.Attribute( "val" ).Value, true );
        }

        public override bool Eval( PlayerInfo info ) {
            return (info.rankChangeType & Type) == Type;
        }

        public override XElement Serialize() {
            XElement el = new XElement( "ConditionRankChangeType" );
            el.Add( new XAttribute( "val", this.Type.ToString() ) );
            return el;
        }
    }


    // check previous Rank
    public sealed class ConditionPreviousRank : Condition {
        public Rank Rank;
        public ComparisonOperation Comparison;

        public ConditionPreviousRank( Rank _rank, ComparisonOperation _comparison ) {
            this.Rank = _rank;
            this.Comparison = _comparison;
        }

        public ConditionPreviousRank( XElement el ) {
            Rank = RankList.ParseRank( el.Attribute( "val" ).Value );
            Comparison = (ComparisonOperation)Enum.Parse( typeof( ComparisonOperation ), el.Attribute( "op" ).Value, true );
        }

        public override bool Eval( PlayerInfo info ) {
            Rank prevRank = info.previousRank;
            if( prevRank == null ) {
                prevRank = info.rank;
            }
            switch( this.Comparison ) {
                case ComparisonOperation.lt:
                    return (info.previousRank < this.Rank);
                case ComparisonOperation.lte:
                    return (info.previousRank <= this.Rank);
                case ComparisonOperation.gte:
                    return (info.previousRank >= this.Rank);
                case ComparisonOperation.gt:
                    return (info.previousRank > this.Rank);
                case ComparisonOperation.eq:
                    return (info.previousRank == this.Rank);
                case ComparisonOperation.neq:
                    return (info.previousRank != this.Rank);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override XElement Serialize() {
            XElement el = new XElement( "ConditionPreviousRank" );
            el.Add( new XAttribute( "val", Rank.ToString() ) );
            el.Add( new XAttribute( "op", Comparison.ToString() ) );
            return el;
        }
    }

    #endregion


    #region Condition Sets

    // base class for condition combinations
    public class ConditionSet : Condition {
        protected ConditionSet() {
            Conditions = new List<Condition>();
        }

        public List<Condition> Conditions {
            get;
            private set;
        }

        protected ConditionSet( IEnumerable<Condition> _conditions ) {
            Conditions = _conditions.ToList();
        }

        protected ConditionSet( XElement el )
            : this() {
            foreach( XElement cel in el.Elements() ) {
                Add( Condition.Parse( cel ) );
            }
        }

        public override bool Eval( PlayerInfo info ) {
            throw new NotImplementedException();
        }

        public void Add( Condition cond ) {
            Conditions.Add( cond );
        }

        public override XElement Serialize() {
            throw new NotImplementedException();
        }
    }

    // Logical AND
    public sealed class ConditionAND : ConditionSet {
        public ConditionAND() { }
        public ConditionAND( IEnumerable<Condition> conditions ) : base( conditions ) { }
        public ConditionAND( XElement el ) : base( el ) { }

        public override bool Eval( PlayerInfo info ) {
            if( Conditions == null ) return true;
            for( int i = 0; i < Conditions.Count; i++ ) {
                if( !Conditions[i].Eval( info ) ) return false;
            }
            return true;
        }

        public override XElement Serialize() {
            XElement el = new XElement( "AND" );
            foreach( Condition cond in Conditions ) {
                el.Add( cond.Serialize() );
            }
            return el;
        }
    }

    // Logical NAND
    public sealed class ConditionNAND : ConditionSet {
        public ConditionNAND() { }
        public ConditionNAND( IEnumerable<Condition> conditions ) : base( conditions ) { }
        public ConditionNAND( XElement el ) : base( el ) { }

        public override bool Eval( PlayerInfo info ) {
            if( Conditions == null ) return true;
            for( int i = 0; i < Conditions.Count; i++ ) {
                if( !Conditions[i].Eval( info ) ) return true;
            }
            return false;
        }

        public override XElement Serialize() {
            XElement el = new XElement( "NAND" );
            foreach( Condition cond in Conditions ) {
                el.Add( cond.Serialize() );
            }
            return el;
        }
    }

    // Logical OR
    public sealed class ConditionOR : ConditionSet {
        public ConditionOR() { }
        public ConditionOR( IEnumerable<Condition> conditions ) : base( conditions ) { }
        public ConditionOR( XElement el ) : base( el ) { }

        public override bool Eval( PlayerInfo info ) {
            if( Conditions == null ) return true;
            for( int i = 0; i < Conditions.Count; i++ ) {
                if( Conditions[i].Eval( info ) ) return true;
            }
            return false;
        }

        public override XElement Serialize() {
            XElement el = new XElement( "OR" );
            foreach( Condition cond in Conditions ) {
                el.Add( cond.Serialize() );
            }
            return el;
        }
    }

    // Logical NOR
    public sealed class ConditionNOR : ConditionSet {
        public ConditionNOR() { }
        public ConditionNOR( IEnumerable<Condition> conditions ) : base( conditions ) { }
        public ConditionNOR( XElement el ) : base( el ) { }

        public override bool Eval( PlayerInfo info ) {
            if( Conditions == null ) return true;
            for( int i = 0; i < Conditions.Count; i++ ) {
                if( Conditions[i].Eval( info ) ) return false;
            }
            return true;
        }

        public override XElement Serialize() {
            XElement el = new XElement( "NOR" );
            foreach( Condition cond in Conditions ) {
                el.Add( cond.Serialize() );
            }
            return el;
        }
    }

    #endregion


    #region Enums

    public enum ComparisonOperation {
        lt,
        lte,
        gte,
        gt,
        eq,
        neq
    }

    public enum ConditionField {
        TimeSinceFirstLogin,
        TimeSinceLastLogin,
        LastSeen,
        TotalTime,
        BlocksBuilt,
        BlocksDeleted,
        BlocksChanged,
        BlocksDrawn,
        TimesVisited,
        MessagesWritten,
        TimesKicked,
        TimeSinceRankChange,
        TimeSinceLastKick
    }

    public enum ConditionScopeType {
        Total,
        SinceRankChange,
        SinceKick,
        TimeSpan
    }

    public enum CriterionType {
        Required,
        Suggested,
        Automatic
    }

    public enum RankChangeType {
        Default = 0,
        Promoted = 1,
        Demoted = 2,
        AutoPromoted = 3,
        AutoDemoted = 4
    }

    #endregion
}