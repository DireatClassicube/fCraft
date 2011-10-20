﻿// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using fCraft.Drawing;

namespace fCraft {

    /// <summary> Integer 3D vector, used by Forester. </summary>
    public struct Vector3I : IEquatable<Vector3I>, IComparable<Vector3I>, IComparable<Vector3F> {
        public static readonly Vector3I Zero = new Vector3I( 0, 0, 0 );
        public static readonly Vector3I Up = new Vector3I( 0, 0, 1 );
        public static readonly Vector3I Down = new Vector3I( 0, 0, -1 );

        public int X, Y, Z;
        public int X2 { get { return X * X; } }
        public int Y2 { get { return Y * Y; } }
        public int Z2 { get { return Z * Z; } }

        public Vector3I( int x, int y, int z ) {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3I( Vector3I other ) {
            X = other.X;
            Y = other.Y;
            Z = other.Z;
        }

        public Vector3I( Vector3F other ) {
            X = (int)other.X;
            Y = (int)other.Y;
            Z = (int)other.Z;
        }


        public float Length {
            get {
                return (float)Math.Sqrt( X * X + Y * Y + Z * Z );
            }
        }

        public int LengthSquared {
            get {
                return X * X + Y * Y + Z * Z;
            }
        }


        public int this[int i] {
            get {
                switch( i ) {
                    case 0: return X;
                    case 1: return Y;
                    default: return Z;
                }
            }
            set {
                switch( i ) {
                    case 0: X = value; return;
                    case 1: Y = value; return;
                    default: Z = value; return;
                }
            }
        }

        public int this[Axis i] {
            get {
                switch( i ) {
                    case Axis.X: return X;
                    case Axis.Y: return Y;
                    default: return Z;
                }
            }
            set {
                switch( i ) {
                    case Axis.X: X = value; return;
                    case Axis.Y: Y = value; return;
                    default: Z = value; return;
                }
            }
        }


        #region Operations

        public static Vector3I operator +( Vector3I a, Vector3I b ) {
            return new Vector3I( a.X + b.X, a.Y + b.Y, a.Z + b.Z );
        }

        public static Vector3I operator +( Vector3I a, int scalar ) {
            return new Vector3I( a.X + scalar, a.Y + scalar, a.Z + scalar );
        }

        public static Vector3I operator -( Vector3I a, Vector3I b ) {
            return new Vector3I( a.X - b.X, a.Y - b.Y, a.Z - b.Z );
        }

        public static Vector3I operator -( Vector3I a, int scalar ) {
            return new Vector3I( a.X - scalar, a.Y - scalar, a.Z - scalar );
        }

        public static Vector3I operator *( Vector3I a, double scalar ) {
            return new Vector3I( (int)(a.X * scalar), (int)(a.Y * scalar), (int)(a.Z * scalar) );
        }

        public static Vector3I operator /( Vector3I a, double scalar ) {
            return new Vector3I( (int)(a.X / scalar), (int)(a.Y / scalar), (int)(a.Z / scalar) );
        }

        #endregion


        #region Equality

        public override bool Equals( object obj ) {
            if( obj is Vector3I ) {
                return Equals( (Vector3I)obj );
            } else {
                return base.Equals( obj );
            }
        }

        public bool Equals( Vector3I other ) {
            return (X == other.X) && (Y == other.Y) && (Z == other.Z);
        }


        public static bool operator ==( Vector3I a, Vector3I b ) {
            return a.Equals( b );
        }

        public static bool operator !=( Vector3I a, Vector3I b ) {
            return !a.Equals( b );
        }


        public override int GetHashCode() {
            return X + Z * 1625 + Y * 2642245;
        }

        #endregion


        #region Comparison

        public int CompareTo( Vector3I other ) {
            return Math.Sign( LengthSquared - other.LengthSquared );
        }

        public int CompareTo( Vector3F other ) {
            return Math.Sign( LengthSquared - other.LengthSquared );
        }


        public static bool operator >( Vector3I a, Vector3I b ) {
            return a.LengthSquared > b.LengthSquared;
        }

        public static bool operator <( Vector3I a, Vector3I b ) {
            return a.LengthSquared < b.LengthSquared;
        }

        public static bool operator >=( Vector3I a, Vector3I b ) {
            return a.LengthSquared >= b.LengthSquared;
        }

        public static bool operator <=( Vector3I a, Vector3I b ) {
            return a.LengthSquared <= b.LengthSquared;
        }

        #endregion

        public int Dot( Vector3I b ) {
            return (X * b.X) + (Y * b.Y) + (Z * b.Z);
        }

        public float Dot( Vector3F b ) {
            return (X * b.X) + (Y * b.Y) + (Z * b.Z);
        }

        public Vector3I Cross( Vector3I b ) {
            return new Vector3I( (Y * b.Z) - (Z * b.Y),
                                 (Z * b.X) - (X * b.Z),
                                 (X * b.Y) - (Y * b.X) );
        }

        public Vector3F Cross( Vector3F b ) {
            return new Vector3F( (Y * b.Z) - (Z * b.Y),
                                 (Z * b.X) - (X * b.Z),
                                 (X * b.Y) - (Y * b.X) );
        }

        public Axis GetLargestComponent() {
            int maxVal = Math.Max( Math.Abs( X ), Math.Max( Math.Abs( Y ), Math.Abs( Z ) ) );
            if( maxVal == Math.Abs( X ) ) return Axis.X;
            if( maxVal == Math.Abs( Y ) ) return Axis.Y;
            return Axis.Z;
        }

        public override string ToString() {
            return String.Format( "({0},{1},{2})", X, Y, Z );
        }

        public Position ToPosition() {
            return new Position( X, Y, Z );
        }

        public Vector3I Abs() {
            return new Vector3I( Math.Abs( X ), Math.Abs( Y ), Math.Abs( Z ) );
        }
    }


    /// <summary>
    /// Floating-point (float) 3D vector, used by Forester
    /// </summary>
    public struct Vector3F : IEquatable<Vector3F>, IComparable<Vector3I>, IComparable<Vector3F> {
        public static readonly Vector3F Zero = new Vector3F( 0, 0, 0 );
        public static readonly Vector3F Up = new Vector3F( 0, 0, 1 );
        public static readonly Vector3F Down = new Vector3F( 0, 0, -1 );

        public float X, Y, Z;
        public float X2 { get { return X * X; } }
        public float Y2 { get { return Y * Y; } }
        public float Z2 { get { return Z * Z; } }


        public Vector3F( float x, float y, float z ) {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3F( Vector3F other ) {
            X = other.X;
            Y = other.Y;
            Z = other.Z;
        }

        public Vector3F( Vector3I other ) {
            X = other.X;
            Y = other.Y;
            Z = other.Z;
        }


        public float Length {
            get {
                return (float)Math.Sqrt( X * X + Y * Y + Z * Z );
            }
        }

        public float LengthSquared {
            get {
                return X * X + Y * Y + Z * Z;
            }
        }


        public float this[int i] {
            get {
                switch( i ) {
                    case 0: return X;
                    case 1: return Y;
                    default: return Z;
                }
            }
            set {
                switch( i ) {
                    case 0: X = value; return;
                    case 1: Y = value; return;
                    default: Z = value; return;
                }
            }
        }


        public float this[Axis i] {
            get {
                switch( i ) {
                    case Axis.X: return X;
                    case Axis.Y: return Y;
                    default: return Z;
                }
            }
            set {
                switch( i ) {
                    case Axis.X: X = value; return;
                    case Axis.Y: Y = value; return;
                    default: Z = value; return;
                }
            }
        }


        #region Operators

        public static Vector3F operator +( Vector3F a, Vector3F b ) {
            return new Vector3F( a.X + b.X, a.Y + b.Y, a.Z + b.Z );
        }

        public static Vector3F operator +( Vector3F a, float scalar ) {
            return new Vector3F( a.X + scalar, a.Y + scalar, a.Z + scalar );
        }

        public static Vector3F operator -( Vector3F a, Vector3F b ) {
            return new Vector3F( a.X - b.X, a.Y - b.Y, a.Z - b.Z );
        }

        public static Vector3F operator -( Vector3F a, float scalar ) {
            return new Vector3F( a.X - scalar, a.Y - scalar, a.Z - scalar );
        }

        public static Vector3F operator *( Vector3F a, float scalar ) {
            return new Vector3F( a.X * scalar, a.Y * scalar, a.Z * scalar );
        }

        public static Vector3F operator /( Vector3F a, double scalar ) {
            return new Vector3F( (float)(a.X / scalar), (float)(a.Y / scalar), (float)(a.Z / scalar) );
        }


        public static Vector3F operator +( Vector3I a, Vector3F b ) {
            return new Vector3F( a.X + b.X, a.Y + b.Y, a.Z + b.Z );
        }

        public static Vector3F operator +( Vector3F a, Vector3I b ) {
            return new Vector3F( a.X + b.X, a.Y + b.Y, a.Z + b.Z );
        }

        public static Vector3F operator -( Vector3I a, Vector3F b ) {
            return new Vector3F( a.X - b.X, a.Y - b.Y, a.Z - b.Z );
        }

        public static Vector3F operator -( Vector3F a, Vector3I b ) {
            return new Vector3F( a.X - b.X, a.Y - b.Y, a.Z - b.Z );
        }

        #endregion


        #region Equality

        public override bool Equals( object obj ) {
            if( obj is Vector3F ) {
                return Equals( (Vector3F)obj );
            } else {
                return base.Equals( obj );
            }
        }

        public bool Equals( Vector3F other ) {
            return (X == other.X) && (Y == other.Y) && (Z == other.Z);
        }


        public static bool operator ==( Vector3F a, Vector3F b ) {
            return a.Equals( b );
        }

        public static bool operator !=( Vector3F a, Vector3F b ) {
            return !a.Equals( b );
        }


        public override int GetHashCode() {
            return (int)(X + Y * 1625 + Z * 2642245);
        }

        #endregion


        #region Comparison

        public int CompareTo( Vector3I other ) {
            return Math.Sign( LengthSquared - LengthSquared );
        }

        public int CompareTo( Vector3F other ) {
            return Math.Sign( LengthSquared - LengthSquared );
        }


        public static bool operator >( Vector3F a, Vector3F b ) {
            return (a.X * a.X + a.Y * a.Y + a.Z * a.Z) > (b.X * b.X + b.Y * b.Y + b.Z * b.Z);
        }

        public static bool operator <( Vector3F a, Vector3F b ) {
            return (a.X * a.X + a.Y * a.Y + a.Z * a.Z) < (b.X * b.X + b.Y * b.Y + b.Z * b.Z);
        }

        public static bool operator >=( Vector3F a, Vector3F b ) {
            return (a.X * a.X + a.Y * a.Y + a.Z * a.Z) >= (b.X * b.X + b.Y * b.Y + b.Z * b.Z);
        }

        public static bool operator <=( Vector3F a, Vector3F b ) {
            return (a.X * a.X + a.Y * a.Y + a.Z * a.Z) <= (b.X * b.X + b.Y * b.Y + b.Z * b.Z);
        }

        #endregion
        
        public float Dot(Vector3I b) {
            return ( X * b.X ) + ( Y * b.Y ) + ( Z * b.Z );
        }

        public float Dot(Vector3F b) {
            return ( X * b.X ) + ( Y * b.Y ) + ( Z * b.Z );
        }

        public Vector3F Cross( Vector3I b ) {
            return new Vector3F( (Y * b.Z) - (Z * b.Y),
                                 (Z * b.X) - (X * b.Z),
                                 (X * b.Y) - (Y * b.X) );
        }

        public Vector3F Cross( Vector3F b ) {
            return new Vector3F( (Y * b.Z) - (Z * b.Y),
                                 (Z * b.X) - (X * b.Z),
                                 (X * b.Y) - (Y * b.X) );
        }

        public override string ToString() {
            return String.Format( "({0},{1},{2})", X, Y, Z );
        }
    }
}