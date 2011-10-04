﻿// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft.Drawing {
    public sealed class CutDrawOperation : DrawOperation {
        public override string Name {
            get { return "Cut"; }
        }

        public override string DescriptionWithBrush {
            get {
                var normalBrush = Brush as NormalBrush;
                if( normalBrush != null && normalBrush.Block != Block.Air ) {
                    return String.Format( "{0}/{1}", Name, normalBrush.Block );
                } else {
                    return Name;
                }
            }
        }

        public CutDrawOperation( Player player )
            : base( player ) {
        }


        public override bool Begin( Vector3I[] marks ) {
            if( !base.Begin( marks ) ) return false;

            BlocksTotalEstimate = Bounds.Volume;

            Coords.X = Bounds.XMin;
            Coords.Y = Bounds.YMin;
            Coords.Z = Bounds.ZMin;

            // remember dimensions and orientation
            CopyInformation copyInfo = new CopyInformation {
                Width = marks[1].X - marks[0].X,
                Length = marks[1].Y - marks[0].Y,
                Height = marks[1].Z - marks[0].Z,
                Buffer = new byte[Bounds.Width, Bounds.Length, Bounds.Height]
            };

            for( int x = Bounds.XMin; x <= Bounds.XMax; x++ ) {
                for( int y = Bounds.YMin; y <= Bounds.YMax; y++ ) {
                    for( int z = Bounds.ZMin; z <= Bounds.ZMax; z++ ) {
                        copyInfo.Buffer[x - Bounds.XMin, y - Bounds.YMin, z - Bounds.ZMin] = Map.GetBlockByte( x, y, z );
                    }
                }
            }
            copyInfo.OriginWorld = Player.World.Name;
            copyInfo.CopyTime = DateTime.UtcNow;
            Player.SetCopyInformation( copyInfo );

            Player.Message( "{0} blocks cut into slot #{1}. You can now &H/paste",
                            Bounds.Volume, Player.CopySlot + 1 );
            Player.Message( "Origin at {0} {1}{2} corner.",
                            (copyInfo.Height > 0 ? "bottom" : "top"),
                            (copyInfo.Length > 0 ? "south" : "north"),
                            (copyInfo.Width > 0 ? "east" : "west") );
            return true;
        }


        public override int DrawBatch( int maxBlocksToDraw ) {
            StartBatch();
            int blocksDone = 0;
            for( ; Coords.X <= Bounds.XMax; Coords.X++ ) {
                for( ; Coords.Y <= Bounds.YMax; Coords.Y++ ) {
                    for( ; Coords.Z <= Bounds.ZMax; Coords.Z++ ) {
                        if( DrawOneBlock() ) {
                            blocksDone++;
                            if( blocksDone >= maxBlocksToDraw ) {
                                Coords.Z++;
                                return blocksDone;
                            }
                        }
                    }
                    Coords.Z = Bounds.ZMin;
                }
                Coords.Y = Bounds.YMin;
                if( TimeToEndBatch ) {
                    Coords.X++;
                    return blocksDone;
                }
            }
            IsDone = true;
            return blocksDone;
        }
    }
}