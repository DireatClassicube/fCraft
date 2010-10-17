﻿using System;
using System.Collections.Generic;


namespace fCraft {

    enum DrawMode {
        Cuboid,
        CuboidHollow,
        Ellipsoid,
        Replace,
        ReplaceNot
    }


    class CopyInformation {
        public byte[, ,] buffer;
        public int widthX, widthY, height;
    }


    struct ReplaceArgs {
        public bool doExclude;
        public Block[] types;
        public Block replacementBlock;
    }


    struct PasteArgs {
        public bool doInclude, doExclude;
        public Block[] types;
    }


    static class DrawCommands {

        const int MaxUndoCount = 2000000;
        const int DrawStride = 16;


        internal static void Init() {
            string generalDrawingHelp = " Use &H/cancel&S to exit draw mode. " +
                                        "Use &H/undo&S to undo the last draw operation. " +
                                        "Use &H/lock&S to cancel drawing after it started.";

            cdCuboid.help += generalDrawingHelp;
            cdCuboidHollow.help += generalDrawingHelp;
            cdEllipsoid.help += generalDrawingHelp;
            cdReplace.help += generalDrawingHelp;
            cdReplaceNot.help += generalDrawingHelp;
            cdPasteNot.help += generalDrawingHelp;
            cdPaste.help += generalDrawingHelp;

            CommandList.RegisterCommand( cdCuboid );
            CommandList.RegisterCommand( cdCuboidHollow );
            CommandList.RegisterCommand( cdEllipsoid );
            CommandList.RegisterCommand( cdReplace );
            CommandList.RegisterCommand( cdReplaceNot );

            CommandList.RegisterCommand( cdMark );
            CommandList.RegisterCommand( cdCancel );
            CommandList.RegisterCommand( cdUndo );

            CommandList.RegisterCommand( cdCopy );
            CommandList.RegisterCommand( cdCut );
            CommandList.RegisterCommand( cdPasteNot );
            CommandList.RegisterCommand( cdPaste );
            CommandList.RegisterCommand( cdMirror );
            CommandList.RegisterCommand( cdRotate );
        }


        static CommandDescriptor cdCuboid = new CommandDescriptor {
            name = "cuboid",
            aliases = new string[] { "blb", "c", "cub", "z" },
            permissions = new Permission[] { Permission.Draw },
            usage = "/cuboid [BlockName]",
            help = "Allows to fill a rectangular area (cuboid) with blocks. " +
                   "If BlockType is omitted, uses the block that player is holding.",
            handler = Cuboid
        };

        internal static void Cuboid( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.Cuboid );
        }



        static CommandDescriptor cdCuboidHollow = new CommandDescriptor {
            name = "cuboidh",
            aliases = new string[] { "cubh", "bhb", "h" },
            permissions = new Permission[] { Permission.Draw },
            usage = "/cuboidh [BlockName]",
            help = "Allows to box a rectangular area (cuboid) with blocks. " +
                   "If BlockType is omitted, uses the block that player is holding.",
            handler = CuboidHollow
        };

        internal static void CuboidHollow( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.CuboidHollow );
        }



        static CommandDescriptor cdEllipsoid = new CommandDescriptor {
            name = "ellipsoid",
            aliases = new string[] { "e", "ell", "spheroid" },
            permissions = new Permission[] { Permission.Draw },
            usage = "/ellipsoid [BlockName]",
            help = "Allows to fill a sphere-like area (ellipsoid) with blocks. " +
                   "If BlockType is omitted, uses the block that player is holding.",
            handler = Ellipsoid
        };

        internal static void Ellipsoid( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.Ellipsoid );
        }



        static CommandDescriptor cdReplace = new CommandDescriptor {
            name = "replace",
            aliases = new string[] { "r" },
            permissions = new Permission[] { Permission.Draw },
            usage = "/replace (BlockToReplace [AnotherOne]) ReplacementBlock",
            help = "Replaces all blocks of specified type(s) in an area.",
            handler = Replace
        };

        internal static void Replace( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.Replace );
        }


        static CommandDescriptor cdReplaceNot = new CommandDescriptor {
            name = "replacenot",
            aliases = new string[] { "rn" },
            permissions = new Permission[] { Permission.Draw },
            usage = "/replacenot (ExcludedBlock [AnotherOne]) ReplacementBlock",
            help = "Replaces all blocks EXCEPT specified type(s) in an area.",
            handler = ReplaceNot
        };

        internal static void ReplaceNot( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.ReplaceNot );
        }



        internal static void Draw( Player player, Command cmd, DrawMode mode ) {
            string blockName = cmd.Next();
            Block block = Block.Undefined;

            Permission permission = Permission.Build;

            // if a type is specified in chat, try to parse it
            if( blockName != null ) {
                block = Map.GetBlockByName( blockName );

                switch( block ) {
                    case Block.Admincrete:
                        permission = Permission.PlaceAdmincrete; break;
                    case Block.Air:
                        permission = Permission.Delete; break;
                    case Block.Water:
                    case Block.StillWater:
                        permission = Permission.PlaceWater; break;
                    case Block.Lava:
                    case Block.StillLava:
                        permission = Permission.PlaceLava; break;
                    case Block.Undefined:
                        player.MessageNow( "{0}: Unrecognized block name: {1}",
                                           mode, blockName );
                        return;
                }
            } // else { use the last-used-block }

            // ReplaceNot does not need permission (since the block is EXCLUDED)
            if( !player.Can( permission ) && mode != DrawMode.ReplaceNot ) {
                player.MessageNow( "{0}: You are not allowed to draw with this block ({1})",
                                   mode, blockName );
                return;
            }

            player.selectionArgs = (byte)block;
            switch( mode ) {
                case DrawMode.Cuboid:
                    player.selectionCallback = CuboidCallback;
                    break;

                case DrawMode.CuboidHollow:
                    player.selectionCallback = CuboidHollowCallback;
                    break;

                case DrawMode.Ellipsoid:
                    player.selectionCallback = EllipsoidCallback;
                    break;

                case DrawMode.Replace:
                case DrawMode.ReplaceNot:

                    List<Block> affectedTypes = new List<Block>();
                    affectedTypes.Add( block );
                    Block affectedType;
                    while( cmd.NextBlockType( out affectedType ) ) {
                        if( affectedType != Block.Undefined ) {
                            affectedTypes.Add( affectedType );
                        } else {
                            player.MessageNow( "{0}: Unrecognized block type.", mode );
                            return;
                        }
                    }

                    if( affectedTypes.Count > 1 ) {
                        Block replacementType = affectedTypes[affectedTypes.Count - 1];
                        affectedTypes.RemoveAt( affectedTypes.Count - 1 );
                        player.selectionArgs = new ReplaceArgs {
                            doExclude = (mode == DrawMode.ReplaceNot),
                            types = affectedTypes.ToArray(),
                            replacementBlock = replacementType
                        };
                        string affectedString = "";
                        foreach( Block affectedBlock in affectedTypes ) {
                            affectedString += ", " + affectedBlock.ToString();
                        }
                        if( mode == DrawMode.ReplaceNot ) {
                            player.MessageNow( "ReplaceNot: Ready to replace everything EXCEPT ({0}) with {1}", affectedString.Substring( 2 ), replacementType );
                        } else {
                            player.MessageNow( "Replace: Ready to replace ({0}) with {1}", affectedString.Substring( 2 ), replacementType );
                        }
                        player.selectionCallback = ReplaceCallback;
                    } else {
                        if( mode == DrawMode.ReplaceNot ) {
                            cdReplaceNot.PrintUsage( player );
                        } else {
                            cdReplace.PrintUsage( player );
                        }
                        return;
                    }
                    break;
            }
            player.selectionMarksExpected = 2;
            player.selectionMarkCount = 0;
            player.selectionMarks.Clear();
            if( block != Block.Undefined ) {
                player.MessageNow( "{0} ({1}): Click a block or use &H/mark",
                                   mode, block );
            } else {
                player.MessageNow( "{0}: Click a block or use &H/mark",
                   mode, block );
            }
        }



        static CommandDescriptor cdMark = new CommandDescriptor {
            name = "mark",
            aliases = new string[] { "m" },
            help = "When making a selection (for drawing or zoning) use this to make a marker at your position in the world. " +
                   "You can mark in places where making blocks is difficult (e.g. mid-air).",
            handler = Mark
        };

        internal static void Mark( Player player, Command command ) {
            Position pos = new Position( (short)((player.pos.x - 1) / 32), (short)((player.pos.y - 1) / 32), (short)((player.pos.h - 1) / 32) );
            pos.x = (short)Math.Min( player.world.map.widthX - 1, Math.Max( 0, (int)pos.x ) );
            pos.y = (short)Math.Min( player.world.map.widthY - 1, Math.Max( 0, (int)pos.y ) );
            pos.h = (short)Math.Min( player.world.map.height - 1, Math.Max( 0, (int)pos.h ) );

            if( player.selectionMarksExpected > 0 ) {
                player.selectionMarks.Enqueue( pos );
                player.selectionMarkCount++;
                if( player.selectionMarkCount >= player.selectionMarksExpected ) {
                    player.selectionCallback( player, player.selectionMarks.ToArray(), player.selectionArgs );
                    player.selectionMarksExpected = 0;
                } else {
                    player.MessageNow( "Block #{0} marked at ({1},{2},{3}). Place mark #{4}.",
                                       player.selectionMarkCount,
                                       pos.x, pos.y, pos.h,
                                       player.selectionMarkCount + 1 );
                }
            } else {
                player.MessageNow( "Cannot mark - no draw or zone commands initiated." );
            }
        }



        static CommandDescriptor cdCancel = new CommandDescriptor {
            name = "cancel",
            help = "Cancels current selection (for drawing or zoning) operation, for instance if you misclicked on the first block. " +
                   "If you wish to stop a drawing in-progress, use &H/lock&S instead.",
            handler = Cancel
        };

        internal static void Cancel( Player player, Command command ) {
            if( player.selectionMarksExpected > 0 ) {
                player.selectionMarksExpected = 0;
                player.MessageNow( "Selection cancelled." );
            } else {
                player.MessageNow( "There is currently nothing to cancel." );
            }
        }


        #region Undo / Redo
        static CommandDescriptor cdUndo = new CommandDescriptor {
            name = "undo",
            aliases = new string[] { "redo" },
            help = "Selectively removes changes from your last drawing command. " +
                   "Note that commands involving over 2 million blocks cannot be undone due to memory restrictions.",
            handler = Undo
        };

        internal static void Undo( Player player, Command command ) {
            if( !player.Can( Permission.Draw ) ) {
                player.NoAccessMessage( Permission.Draw );
                return;
            }
            if( player.undoBuffer.Count > 0 ) {
                // no need to set player.drawingInProgress here because this is done on the user thread
                Logger.Log( "Player {0} initiated /undo affecting {1} blocks (on world {2})", LogType.UserActivity,
                            player.name,
                            player.undoBuffer.Count,
                            player.world.name );
                player.MessageNow( "Restoring {0} blocks...", player.undoBuffer.Count );
                Queue<BlockUpdate> redoBuffer = new Queue<BlockUpdate>();
                while( player.undoBuffer.Count > 0 ) {
                    BlockUpdate newBlock = player.undoBuffer.Dequeue();
                    BlockUpdate oldBlock = new BlockUpdate( null, newBlock.x, newBlock.y, newBlock.h,
                                                            player.world.map.GetBlock( newBlock.x, newBlock.y, newBlock.h ) );
                    player.world.map.QueueUpdate( newBlock );
                    redoBuffer.Enqueue( oldBlock );
                }
                player.undoBuffer = redoBuffer;
                redoBuffer.TrimExcess();
                player.MessageNow( "Type /undo again to reverse this command." );
                GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );
            } else {
                player.MessageNow( "There is currently nothing to undo." );
            }
        }
        #endregion


        internal static void ReplaceCallback( Player player, Position[] marks, object drawArgs ) {
            ReplaceArgs args = (ReplaceArgs)drawArgs;

            byte[] specialTypes = new byte[args.types.Length];
            for( int i = 0; i < args.types.Length; i++ ) {
                specialTypes[i] = (byte)args.types[i];
            }
            byte replacementBlock = (byte)args.replacementBlock;
            bool doExclude = args.doExclude;

            // find start/end coordinates
            int sx = Math.Min( marks[0].x, marks[1].x );
            int ex = Math.Max( marks[0].x, marks[1].x );
            int sy = Math.Min( marks[0].y, marks[1].y );
            int ey = Math.Max( marks[0].y, marks[1].y );
            int sh = Math.Min( marks[0].h, marks[1].h );
            int eh = Math.Max( marks[0].h, marks[1].h );

            int volume = (ex - sx + 1) * (ey - sy + 1) * (eh - sh + 1);
            if( player.CanDraw( volume ) ) {
                player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                player.info.rank.DrawLimit,
                                volume );
                return;
            }

            player.undoBuffer.Clear();

            bool cannotUndo = false;
            int blocks = 0;
            byte block;
            for( int x = sx; x <= ex; x += DrawStride ) {
                for( int y = sy; y <= ey; y += DrawStride ) {
                    for( int h = sh; h <= eh; h++ ) {
                        for( int y3 = 0; y3 < DrawStride && y + y3 <= ey; y3++ ) {
                            for( int x3 = 0; x3 < DrawStride && x + x3 <= ex; x3++ ) {

                                block = player.world.map.GetBlock( x + x3, y + y3, h );

                                if( args.doExclude ) {
                                    bool skip = false;
                                    for( int i = 0; i < specialTypes.Length; i++ ) {
                                        if( block == specialTypes[i] ) {
                                            skip = true;
                                            break;
                                        }
                                    }
                                    if( skip ) continue;
                                } else {
                                    bool skip = true;
                                    for( int i = 0; i < specialTypes.Length; i++ ) {
                                        if( block == specialTypes[i] ) {
                                            skip = false;
                                            break;
                                        }
                                    }
                                    if( skip ) continue;
                                }

                                if( block == (byte)Block.Admincrete && !player.Can( Permission.DeleteAdmincrete ) ) continue;
                                if( player.CanPlace( x, y, h, replacementBlock ) != CanPlaceResult.Allowed ) continue;
                                player.world.map.QueueUpdate( new BlockUpdate( null, x + x3, y + y3, h, replacementBlock ) );
                                if( blocks < MaxUndoCount ) {
                                    player.undoBuffer.Enqueue( new BlockUpdate( null, x + x3, y + y3, h, block ) );
                                } else if( !cannotUndo ) {
                                    player.undoBuffer.Clear();
                                    player.undoBuffer.TrimExcess();
                                    player.MessageNow( "NOTE: This draw command is too massive to undo." );
                                    cannotUndo = true;
                                    if( player.Can( Permission.ManageWorlds ) ) {
                                        player.MessageNow( "Reminder: You can use &H/wflush&S to accelerate draw commands." );
                                    }
                                }
                                blocks++;

                            }
                        }
                    }
                }
            }

            player.MessageNow( "Replacing {0} blocks... The map is now being updated.", blocks );

            string affectedString = "";
            foreach( Block affectedBlock in specialTypes ) {
                affectedString += ", " + affectedBlock.ToString();
            }
            Logger.Log( "{0} replaced {1} blocks {2} ({3}) with {4} (on world {5})", LogType.UserActivity,
                        player.name, blocks,
                        (doExclude ? "except" : "of"),
                        affectedString.Substring( 2 ), (Block)replacementBlock,
                        player.world.name );

            player.undoBuffer.TrimExcess();
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );
        }


        internal static void CuboidCallback( Player player, Position[] marks, object tag ) {
            byte drawBlock = (byte)tag;
            if( drawBlock == (byte)Block.Undefined ) {
                drawBlock = (byte)player.lastUsedBlockType;
            }

            // find start/end coordinates
            int sx = Math.Min( marks[0].x, marks[1].x );
            int ex = Math.Max( marks[0].x, marks[1].x );
            int sy = Math.Min( marks[0].y, marks[1].y );
            int ey = Math.Max( marks[0].y, marks[1].y );
            int sh = Math.Min( marks[0].h, marks[1].h );
            int eh = Math.Max( marks[0].h, marks[1].h );

            int volume = (ex - sx + 1) * (ey - sy + 1) * (eh - sh + 1);
            if( player.CanDraw( volume ) ) {
                player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                   player.info.rank.DrawLimit,
                                   volume );
                return;
            }

            player.undoBuffer.Clear();

            int blocks = 0;
            bool cannotUndo = false;

            for( int x = sx; x <= ex; x += DrawStride ) {
                for( int y = sy; y <= ey; y += DrawStride ) {
                    for( int h = sh; h <= eh; h++ ) {
                        for( int y3 = 0; y3 < DrawStride && y + y3 <= ey; y3++ ) {
                            for( int x3 = 0; x3 < DrawStride && x + x3 <= ex; x3++ ) {
                                DrawOneBlock( player, drawBlock, x + x3, y + y3, h, ref blocks, ref cannotUndo );
                            }
                        }
                    }
                }
            }
            player.MessageNow( "Drawing {0} blocks... The map is now being updated.", blocks );
            Logger.Log( "{0} drew a cuboid containing {1} blocks of type {2} (on world {3})", LogType.UserActivity,
                        player.name,
                        blocks,
                        (Block)drawBlock,
                        player.world.name );
            player.undoBuffer.TrimExcess();
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );
        }


        internal static void CuboidHollowCallback( Player player, Position[] marks, object tag ) {
            byte drawBlock = (byte)tag;
            if( drawBlock == (byte)Block.Undefined ) {
                drawBlock = (byte)player.lastUsedBlockType;
            }

            // find start/end coordinates
            int sx = Math.Min( marks[0].x, marks[1].x );
            int ex = Math.Max( marks[0].x, marks[1].x );
            int sy = Math.Min( marks[0].y, marks[1].y );
            int ey = Math.Max( marks[0].y, marks[1].y );
            int sh = Math.Min( marks[0].h, marks[1].h );
            int eh = Math.Max( marks[0].h, marks[1].h );

            int volume = (ex - sx + 1) * (ey - sy + 1) * (eh - sh + 1) - (ex - sx - 1) * (ey - sy - 1) * (eh - sh - 1);
            if( player.CanDraw( volume ) ) {
                player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                player.info.rank.DrawLimit,
                                volume );
                return;
            }

            player.undoBuffer.Clear();

            int blocks = 0;
            bool cannotUndo = false;

            for( int x = sx; x <= ex; x++ ) {
                for( int y = sy; y <= ey; y++ ) {
                    DrawOneBlock( player, drawBlock, x, y, sh, ref blocks, ref cannotUndo );
                    DrawOneBlock( player, drawBlock, x, y, eh, ref blocks, ref cannotUndo );
                }
            }
            for( int x = sx; x <= ex; x++ ) {
                for( int h = sh; h <= eh; h++ ) {
                    DrawOneBlock( player, drawBlock, x, sy, h, ref blocks, ref cannotUndo );
                    DrawOneBlock( player, drawBlock, x, ey, h, ref blocks, ref cannotUndo );
                }
            }
            for( int y = sy; y <= ey; y++ ) {
                for( int h = sh; h <= eh; h++ ) {
                    DrawOneBlock( player, drawBlock, sx, y, h, ref blocks, ref cannotUndo );
                    DrawOneBlock( player, drawBlock, ex, y, h, ref blocks, ref cannotUndo );
                }
            }

            player.MessageNow( "Drawing {0} blocks... The map is now being updated.", blocks );
            Logger.Log( "{0} drew a hollow cuboid containing {1} blocks of type {2} (on world {3})", LogType.UserActivity,
                        player.name,
                        blocks,
                        (Block)drawBlock,
                        player.world.name );
            player.undoBuffer.TrimExcess();
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );
        }


        internal static void EllipsoidCallback( Player player, Position[] marks, object tag ) {
            byte drawBlock = (byte)tag;
            if( drawBlock == (byte)Block.Undefined ) {
                drawBlock = (byte)player.lastUsedBlockType;
            }

            // find start/end coordinates
            int sx = Math.Min( marks[0].x, marks[1].x );
            int ex = Math.Max( marks[0].x, marks[1].x );
            int sy = Math.Min( marks[0].y, marks[1].y );
            int ey = Math.Max( marks[0].y, marks[1].y );
            int sh = Math.Min( marks[0].h, marks[1].h );
            int eh = Math.Max( marks[0].h, marks[1].h );

            // find axis lengths
            double rx = (ex - sx + 1) / 2d;
            double ry = (ey - sy + 1) / 2d;
            double rh = (eh - sh + 1) / 2d;

            double rx2 = 1 / (rx * rx);
            double ry2 = 1 / (ry * ry);
            double rh2 = 1 / (rh * rh);

            // find center points
            double cx = (ex + sx) / 2d;
            double cy = (ey + sy) / 2d;
            double ch = (eh + sh) / 2d;


            int volume = (int)(.75d * Math.PI * rx * ry * rh);
            if( player.CanDraw( volume ) ) {
                player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                   player.info.rank.DrawLimit,
                                   volume );
                return;
            }

            player.undoBuffer.Clear();

            int blocks = 0;
            bool cannotUndo = false;

            for( int x = sx; x <= ex; x += DrawStride ) {
                for( int y = sy; y <= ey; y += DrawStride ) {
                    for( int h = sh; h <= eh; h++ ) {
                        for( int y3 = 0; y3 < DrawStride && y + y3 <= ey; y3++ ) {
                            for( int x3 = 0; x3 < DrawStride && x + x3 <= ex; x3++ ) {

                                // get relative coordinates
                                double dx = (x + x3 - cx);
                                double dy = (y + y3 - cy);
                                double dh = (h - ch);

                                // test if it's inside ellipse
                                if( (dx * dx) * rx2 + (dy * dy) * ry2 + (dh * dh) * rh2 <= 1 ) {
                                    DrawOneBlock( player, drawBlock, x + x3, y + y3, h, ref blocks, ref cannotUndo );
                                }
                            }
                        }
                    }
                }
            }
            player.MessageNow( "Drawing {0} blocks... The map is now being updated.", blocks );
            Logger.Log( "{0} drew an ellipsoid containing {1} blocks of type {2} (on world {3})", LogType.UserActivity,
                        player.name,
                        blocks,
                        (Block)drawBlock,
                        player.world.name );
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );
        }


        static void DrawOneBlock( Player player, byte drawBlock, int x, int y, int h, ref int blocks, ref bool cannotUndo ) {
            if( player.CanPlace( x, y, h, drawBlock ) != CanPlaceResult.Allowed ) return;
            byte block = player.world.map.GetBlock( x, y, h );
            if( block == drawBlock ||
                (block == (byte)Block.Admincrete && !player.Can( Permission.DeleteAdmincrete )) ||
                (drawBlock == (byte)Block.Admincrete && !player.Can( Permission.PlaceAdmincrete )) ) return;

            player.world.map.QueueUpdate( new BlockUpdate( null, x, y, h, drawBlock ) );
            if( blocks < MaxUndoCount ) {
                player.undoBuffer.Enqueue( new BlockUpdate( null, x, y, h, block ) );
            } else if( !cannotUndo ) {
                player.undoBuffer.Clear();
                player.undoBuffer.TrimExcess();
                player.MessageNow( "NOTE: This draw command is too massive to undo." );
                if( player.Can( Permission.ManageWorlds ) ) {
                    player.MessageNow( "Reminder: You can use &H/wflush&S to accelerate draw commands." );
                }
                cannotUndo = true;
            }
            blocks++;
        }


        #region Copy and Paste

        static CommandDescriptor cdCopy = new CommandDescriptor {
            name = "copy",
            permissions = new Permission[] { Permission.CopyAndPaste },
            help = "Copy blocks for pasting. " +
                   "Used together with &H/paste&S and &H/pastenot&S commands. " +
                   "Note that pasting starts at the same corner that you started &H/copy&S from.",
            handler = Copy
        };

        internal static void Copy( Player player, Command cmd ) {
            player.SetCallback( 2, CopyCallback, null );
            player.MessageNow( "Copy: Place a block or type /mark to use your location." );
        }

        internal static void CopyCallback( Player player, Position[] marks, object tag ) {
            int sx = Math.Min( marks[0].x, marks[1].x );
            int ex = Math.Max( marks[0].x, marks[1].x );
            int sy = Math.Min( marks[0].y, marks[1].y );
            int ey = Math.Max( marks[0].y, marks[1].y );
            int sh = Math.Min( marks[0].h, marks[1].h );
            int eh = Math.Max( marks[0].h, marks[1].h );

            int volume = (ex - sx + 1) * (ey - sy + 1) * (eh - sh + 1);
            if( player.CanDraw( volume ) ) {
                player.MessageNow( String.Format( "You are only allowed to run commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                               player.info.rank.DrawLimit, volume ) );
                return;
            }

            CopyInformation copyInfo = new CopyInformation();

            // remember dimensions and orientation
            copyInfo.widthX = marks[1].x - marks[0].x;
            copyInfo.widthY = marks[1].y - marks[0].y;
            copyInfo.height = marks[1].h - marks[0].h;

            copyInfo.buffer = new byte[ex - sx + 1, ey - sy + 1, eh - sh + 1];

            for( int x = sx; x <= ex; x++ ) {
                for( int y = sy; y <= ey; y++ ) {
                    for( int h = sh; h <= eh; h++ ) {
                        copyInfo.buffer[x - sx, y - sy, h - sh] = player.world.map.GetBlock( x, y, h );
                    }
                }
            }

            player.copyInformation = copyInfo;
            player.MessageNow( "{0} blocks were copied. You can now &H/paste", volume );
            player.MessageNow( "Origin at {0} {1}{2} corner.",
                               (copyInfo.height > 0 ? "bottom" : "top"),
                               (copyInfo.widthY > 0 ? "south" : "north"),
                               (copyInfo.widthX > 0 ? "west" : "east") );

            Logger.Log( "{0} copied {1} blocks from {2}.", LogType.UserActivity,
                        player.name, volume, player.world.name );
        }



        static CommandDescriptor cdCut = new CommandDescriptor {
            name = "cut",
            permissions = new Permission[] { Permission.CopyAndPaste },
            help = "Copies and removes blocks for pasting. Unless a different block type is specified, the area is filled with air. " +
                   "Used together with &H/paste&S and &H/pastenot&S commands. " +
                   "Note that pasting starts at the same corner that you started &H/cut&S from.",
            usage = "/cut [FillBlock]",
            handler = Cut
        };

        internal static void Cut( Player player, Command cmd ) {
            Block fillBlock = Block.Air;
            if( cmd.NextBlockType( out fillBlock ) ) {
                if( fillBlock == Block.Undefined ) {
                    cmd.Rewind();
                    player.Message( "Cut: Unknown block type \"{0}\"", cmd.Next() );
                    return;
                }
            } else {
                fillBlock = Block.Air;
            }
            player.SetCallback( 2, CutCallback, fillBlock );
            player.MessageNow( "Cut: Place a block or type /mark to use your location." );
        }

        internal static void CutCallback( Player player, Position[] marks, object tag ) {
            int sx = Math.Min( marks[0].x, marks[1].x );
            int ex = Math.Max( marks[0].x, marks[1].x );
            int sy = Math.Min( marks[0].y, marks[1].y );
            int ey = Math.Max( marks[0].y, marks[1].y );
            int sh = Math.Min( marks[0].h, marks[1].h );
            int eh = Math.Max( marks[0].h, marks[1].h );

            byte fillType = (byte)tag;

            int volume = (ex - sx + 1) * (ey - sy + 1) * (eh - sh + 1);
            if( player.CanDraw( volume ) ) {
                player.MessageNow( String.Format( "You are only allowed to run commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                               player.info.rank.DrawLimit, volume ) );
                return;
            }

            CopyInformation copyInfo = new CopyInformation();

            // remember dimensions and orientation
            copyInfo.widthX = marks[1].x - marks[0].x;
            copyInfo.widthY = marks[1].y - marks[0].y;
            copyInfo.height = marks[1].h - marks[0].h;

            copyInfo.buffer = new byte[ex - sx + 1, ey - sy + 1, eh - sh + 1];

            player.undoBuffer.Clear();
            int blocks = 0;
            bool cannotUndo = false;

            for( int x = sx; x <= ex; x++ ) {
                for( int y = sy; y <= ey; y++ ) {
                    for( int h = sh; h <= eh; h++ ) {
                        copyInfo.buffer[x - sx, y - sy, h - sh] = player.world.map.GetBlock( x, y, h );
                        DrawOneBlock( player, fillType, x, y, h, ref blocks, ref cannotUndo );
                    }
                }
            }

            player.copyInformation = copyInfo;
            player.MessageNow( "{0} blocks were cut. You can now &H/paste", volume );
            player.MessageNow( "Origin at {0} {1}{2} corner.",
                               (copyInfo.height > 0 ? "bottom" : "top"),
                               (copyInfo.widthY > 0 ? "south" : "north"),
                               (copyInfo.widthX > 0 ? "west" : "east") );

            Logger.Log( "{0} cut {1} blocks from {2}, replacing {3} blocks with {4}.", LogType.UserActivity,
                        player.name, volume, player.world.name, blocks, (Block)fillType );

            player.undoBuffer.TrimExcess();
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );
        }



        static CommandDescriptor cdPasteNot = new CommandDescriptor {
            name = "pastenot",
            permissions = new Permission[] { Permission.CopyAndPaste },
            help = "Paste previously copied blocks, excluding specified block type(s). " +
                   "Used together with &H/copy&S command. " +
                   "Note that pasting starts at the same corner that you started &H/copy&S from. ",
            usage = "/pastenot ExcludedBlock [AnotherOne [AndAnother]]",
            handler = PasteNot
        };

        internal static void PasteNot( Player player, Command cmd ) {
            if( player.copyInformation == null ) {
                player.MessageNow( "Nothing to paste! Copy something first." );
                return;
            }

            PasteArgs args;
            List<Block> excludedTypes = new List<Block>();
            Block excludedType;
            while( cmd.NextBlockType( out excludedType ) ) {
                if( excludedType != Block.Undefined ) {
                    excludedTypes.Add( excludedType );
                } else {
                    player.MessageNow( "Paste: Unrecognized block type." );
                    return;
                }
            }

            if( excludedTypes.Count > 0 ) {
                args = new PasteArgs {
                    doExclude = true,
                    types = excludedTypes.ToArray()
                };
                string includedString = "";
                foreach( Block block in excludedTypes ) {
                    includedString += ", " + block.ToString();
                }
                player.MessageNow( "Ready to paste all EXCEPT {0}", includedString.Substring( 2 ) );
            } else {
                player.MessageNow( "PasteNot: Please specify block(s) to exclude." );
                return;
            }

            player.SetCallback( 1, PasteCallback, args );

            player.MessageNow( "PasteNot: Place a block or type /mark to use your location. " );
        }


        static CommandDescriptor cdPaste = new CommandDescriptor {
            name = "paste",
            permissions = new Permission[] { Permission.CopyAndPaste },
            help = "Pastes previously copied blocks. Used together with &H/copy&S command. " +
                   "If one or more optional IncludedBlock parameters are specified, ONLY pastes blocks of specified type(s). " +
                   "Note that pasting starts at the same corner that you started &H/copy&S from.",
            usage = "/paste [IncludedBlock [AnotherOne [AndAnother]]]",
            handler = Paste
        };

        internal static void Paste( Player player, Command cmd ) {
            if( player.copyInformation == null ) {
                player.MessageNow( "Nothing to paste! Copy something first." );
                return;
            }

            List<Block> includedTypes = new List<Block>();
            Block includedType;
            while( cmd.NextBlockType( out includedType ) ) {
                if( includedType != Block.Undefined ) {
                    includedTypes.Add( includedType );
                } else {
                    player.MessageNow( "Paste: Unrecognized block type." );
                    return;
                }
            }

            PasteArgs args;
            if( includedTypes.Count > 0 ) {
                args = new PasteArgs {
                    doInclude = true,
                    types = includedTypes.ToArray()
                };
                string includedString = "";
                foreach( Block block in includedTypes ) {
                    includedString += ", " + block.ToString();
                }
                player.MessageNow( "Ready to paste ONLY {0}", includedString.Substring( 2 ) );
            } else {
                args = new PasteArgs() {
                    types = new Block[0]
                };
                player.MessageNow( "Ready to paste all blocks." );
            }

            player.SetCallback( 1, PasteCallback, args );

            player.MessageNow( "Paste: Place a block or type /mark to use your location. " );
        }


        internal static void PasteCallback( Player player, Position[] marks, object tag ) {
            CopyInformation info = player.copyInformation;

            PasteArgs args = (PasteArgs)tag;
            byte[] specialTypes = new byte[args.types.Length];
            for( int i = 0; i < args.types.Length; i++ ) {
                specialTypes[i] = (byte)args.types[i];
            }
            Map map = player.world.map;

            BoundingBox bounds = new BoundingBox( marks[0], info.widthX, info.widthY, info.height );

            if( bounds.xMin < 0 || bounds.xMax > map.widthX - 1 ) {
                player.MessageNow( "Warning: Not enough room horizontally (X), paste cut off." );
            }
            if( bounds.yMin < 0 || bounds.yMax > map.widthY - 1 ) {
                player.MessageNow( "Warning: Not enough room horizontally (Y), paste cut off." );
            }
            if( bounds.hMin < 0 || bounds.hMax > map.height - 1 ) {
                player.MessageNow( "Warning: Not enough room vertically, paste cut off." );
            }

            player.undoBuffer.Clear();

            int blocks = 0;
            bool cannotUndo = false;
            byte block;

            for( int x = bounds.xMin; x <= bounds.xMax; x += DrawStride ) {
                for( int y = bounds.yMin; y <= bounds.yMax; y += DrawStride ) {
                    for( int h = bounds.hMin; h <= bounds.hMax; h++ ) {
                        for( int y3 = 0; y3 < DrawStride && y + y3 <= bounds.yMax; y3++ ) {
                            for( int x3 = 0; x3 < DrawStride && x + x3 <= bounds.xMax; x3++ ) {
                                block = info.buffer[x + x3 - bounds.xMin, y + y3 - bounds.yMin, h - bounds.hMin];

                                if( args.doInclude ) {
                                    bool skip = true;
                                    for( int i = 0; i < specialTypes.Length; i++ ) {
                                        if( block == specialTypes[i] ) {
                                            skip = false;
                                            break;
                                        }
                                    }
                                    if( skip ) continue;
                                } else if( args.doExclude ) {
                                    bool skip = false;
                                    for( int i = 0; i < specialTypes.Length; i++ ) {
                                        if( block == specialTypes[i] ) {
                                            skip = true;
                                            break;
                                        }
                                    }
                                    if( skip ) continue;
                                }
                                DrawOneBlock( player, block, x + x3, y + y3, h, ref blocks, ref cannotUndo );
                            }
                        }
                    }
                }
            }

            player.MessageNow( "{0} blocks pasted. The map is now being updated...", blocks );

            Logger.Log( "{0} pasted {1} blocks to {2}.", LogType.UserActivity,
                        player.name, blocks, player.world.name );
            player.undoBuffer.TrimExcess();
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );
        }


        static CommandDescriptor cdMirror = new CommandDescriptor {
            name = "mirror",
            aliases = new string[] { "flip" },
            permissions = new Permission[] { Permission.CopyAndPaste },
            help = "Flips copied blocks along specified axis/axes. " +
                   "The axes are: X = horizontal (east-west), Y = horizontal (north-south), Z = vertical. " +
                   "You can mirror more than one axis at a time, e.g. &H/copymirror X Y&S.",
            usage = "/mirror [X] [Y] [Z]",
            handler = Mirror
        };

        internal static void Mirror( Player player, Command cmd ) {
            if( player.copyInformation == null ) {
                player.MessageNow( "Nothing to flip! Copy something first." );
                return;
            }

            bool flipX = false, flipY = false, flipH = false;
            string axis;
            while( (axis = cmd.Next()) != null ) {
                foreach( char c in axis.ToLower() ) {
                    if( c == 'x' ) flipX = true;
                    if( c == 'y' ) flipY = true;
                    if( c == 'z' ) flipH = true;
                }
            }

            if( !flipX && !flipY && !flipH ) {
                cdMirror.PrintUsage( player );
                return;
            }

            byte block;
            byte[, ,] buffer = player.copyInformation.buffer;

            if( flipX ) {
                int left = 0;
                int right = buffer.GetLength( 0 ) - 1;
                while( left < right ) {
                    for( int y = player.copyInformation.buffer.GetLength( 1 ) - 1; y >= 0; y-- ) {
                        for( int h = player.copyInformation.buffer.GetLength( 2 ) - 1; h >= 0; h-- ) {
                            block = buffer[left, y, h];
                            buffer[left, y, h] = buffer[right, y, h];
                            buffer[right, y, h] = block;
                        }
                    }
                    left++;
                    right--;
                }
            }

            if( flipY ) {
                int left = 0;
                int right = buffer.GetLength( 1 ) - 1;
                while( left < right ) {
                    for( int x = player.copyInformation.buffer.GetLength( 0 ) - 1; x >= 0; x-- ) {
                        for( int h = player.copyInformation.buffer.GetLength( 2 ) - 1; h >= 0; h-- ) {
                            block = buffer[x, left, h];
                            buffer[x, left, h] = buffer[x, right, h];
                            buffer[x, right, h] = block;
                        }
                    }
                    left++;
                    right--;
                }
            }

            if( flipH ) {
                int left = 0;
                int right = buffer.GetLength( 2 ) - 1;
                while( left < right ) {
                    for( int x = player.copyInformation.buffer.GetLength( 0 ) - 1; x >= 0; x-- ) {
                        for( int y = player.copyInformation.buffer.GetLength( 1 ) - 1; y >= 0; y-- ) {
                            block = buffer[x, y, left];
                            buffer[x, y, left] = buffer[x, y, right];
                            buffer[x, y, right] = block;
                        }
                    }
                    left++;
                    right--;
                }
            }

            if( flipX ) {
                if( flipY ) {
                    if( flipH ) {
                        player.Message( "Flipped copy along all axes." );
                    } else {
                        player.Message( "Flipped copy along X (east/west) and Y (north/south) axes." );
                    }
                } else {
                    if( flipH ) {
                        player.Message( "Flipped copy along X (east/west) and Z (vertical) axes." );
                    } else {
                        player.Message( "Flipped copy along X (east/west) axis." );
                    }
                }
            } else {
                if( flipY ) {
                    if( flipH ) {
                        player.Message( "Flipped copy along Y (north/south) and Z (vertical) axes." );
                    } else {
                        player.Message( "Flipped copy along Y (north/south) axis." );
                    }
                } else {
                    player.Message( "Flipped copy along Z (vertical) axis." );
                }
            }
        }


        static CommandDescriptor cdRotate = new CommandDescriptor {
            name = "rotate",
            permissions = new Permission[] { Permission.CopyAndPaste },
            help = "Rotates copied blocks around specifies axis/axes. If no axis is given, rotates around Z (vertical).",
            usage = "/rotate (-90|90|180|270) (X|Y|Z)",
            handler = Rotate
        };

        enum RotationAxis {
            X, Y, Z
        }
        internal static void Rotate( Player player, Command cmd ) {
            if( player.copyInformation == null ) {
                player.MessageNow( "Nothing to rotate! Copy something first." );
                return;
            }

            int degrees;
            if( !cmd.NextInt( out degrees ) || (degrees != 90 && degrees != -90 && degrees != 180 && degrees != 270) ) {
                cdRotate.PrintUsage( player );
                return;
            }

            string axisName = cmd.Next();
            RotationAxis axis = RotationAxis.Z;
            if( axisName != null ) {
                switch( axisName.ToLower() ) {
                    case "x":
                        axis = RotationAxis.X;
                        break;
                    case "y":
                        axis = RotationAxis.Y;
                        break;
                    case "z":
                    case "h":
                        axis = RotationAxis.Z;
                        break;
                    default:
                        cdRotate.PrintUsage( player );
                        return;
                }
            }


            // allocate the new buffer
            byte[, ,] oldBuffer = player.copyInformation.buffer;
            byte[, ,] newBuffer;

            if( degrees == 180 ) {
                newBuffer = new byte[oldBuffer.GetLength( 0 ), oldBuffer.GetLength( 1 ), oldBuffer.GetLength( 2 )];

            } else if( axis == RotationAxis.X ) {
                newBuffer = new byte[oldBuffer.GetLength( 0 ), oldBuffer.GetLength( 2 ), oldBuffer.GetLength( 1 )];
                int dimY = player.copyInformation.widthY;
                player.copyInformation.widthY = player.copyInformation.height;
                player.copyInformation.height = dimY;

            } else if( axis == RotationAxis.Y ) {
                newBuffer = new byte[oldBuffer.GetLength( 2 ), oldBuffer.GetLength( 1 ), oldBuffer.GetLength( 0 )];
                int dimX = player.copyInformation.widthX;
                player.copyInformation.widthX = player.copyInformation.height;
                player.copyInformation.height = dimX;

            } else {
                newBuffer = new byte[oldBuffer.GetLength( 1 ), oldBuffer.GetLength( 0 ), oldBuffer.GetLength( 2 )];
                int dimY = player.copyInformation.widthY;
                player.copyInformation.widthY = player.copyInformation.widthX;
                player.copyInformation.widthX = dimY;
            }


            // construct the rotation matrix
            int[,] matrix = new int[,]{
                {1,0,0},
                {0,1,0},
                {0,0,1}
            };

            int a, b;
            switch( axis ) {
                case RotationAxis.X:
                    a = 1;
                    b = 2;
                    break;
                case RotationAxis.Y:
                    a = 0;
                    b = 2;
                    break;
                default:
                    a = 0;
                    b = 1;
                    break;
            }

            switch( degrees ) {
                case 90:
                    matrix[a, a] = 0;
                    matrix[b, b] = 0;
                    matrix[a, b] = -1;
                    matrix[b, a] = 1;
                    break;
                case 180:
                    matrix[a, a] = -1;
                    matrix[b, b] = -1;
                    break;
                case -90:
                case 270:
                    matrix[a, a] = 0;
                    matrix[b, b] = 0;
                    matrix[a, b] = 1;
                    matrix[b, a] = -1;
                    break;
            }

            // apply the rotation matrix
            int nx, ny, nz;
            for( int x = oldBuffer.GetLength( 0 ) - 1; x >= 0; x-- ) {
                for( int y = oldBuffer.GetLength( 1 ) - 1; y >= 0; y-- ) {
                    for( int z = oldBuffer.GetLength( 2 ) - 1; z >= 0; z-- ) {
                        nx = (matrix[0, 0] < 0 ? oldBuffer.GetLength( 0 ) - 1 - x : (matrix[0, 0] > 0 ? x : 0)) +
                             (matrix[0, 1] < 0 ? oldBuffer.GetLength( 1 ) - 1 - y : (matrix[0, 1] > 0 ? y : 0)) +
                             (matrix[0, 2] < 0 ? oldBuffer.GetLength( 2 ) - 1 - z : (matrix[0, 2] > 0 ? z : 0));
                        ny = (matrix[1, 0] < 0 ? oldBuffer.GetLength( 0 ) - 1 - x : (matrix[1, 0] > 0 ? x : 0)) +
                             (matrix[1, 1] < 0 ? oldBuffer.GetLength( 1 ) - 1 - y : (matrix[1, 1] > 0 ? y : 0)) +
                             (matrix[1, 2] < 0 ? oldBuffer.GetLength( 2 ) - 1 - z : (matrix[1, 2] > 0 ? z : 0));
                        nz = (matrix[2, 0] < 0 ? oldBuffer.GetLength( 0 ) - 1 - x : (matrix[2, 0] > 0 ? x : 0)) +
                             (matrix[2, 1] < 0 ? oldBuffer.GetLength( 1 ) - 1 - y : (matrix[2, 1] > 0 ? y : 0)) +
                             (matrix[2, 2] < 0 ? oldBuffer.GetLength( 2 ) - 1 - z : (matrix[2, 2] > 0 ? z : 0));
                        newBuffer[nx, ny, nz] = oldBuffer[x, y, z];
                    }
                }
            }

            player.Message( "Rotated copy by {0} degrees around {1} axis.", degrees, axis );
            player.copyInformation.buffer = newBuffer;
        }

        #endregion
    }
}