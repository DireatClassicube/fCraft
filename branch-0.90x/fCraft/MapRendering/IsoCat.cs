// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using fCraft.Properties;
using JetBrains.Annotations;

namespace fCraft.MapRendering {
    /// <summary> Isometric map renderer. Creates a bitmap of the map. </summary>
    public sealed unsafe class IsoCat {
        static readonly byte[] Tiles, ShadowTiles;
        static readonly int TileX, TileY;
        static readonly int MaxTileDim, TileStride;
        const byte BlockTypeCount = (byte)Block.StoneBrick + 1;


        static IsoCat() {
            using (Bitmap tilesBmp = Resources.Tileset) {
                TileX = tilesBmp.Width/BlockTypeCount;
                TileY = tilesBmp.Height;
                TileStride = TileX*TileY*4;
                Tiles = new byte[BlockTypeCount*TileStride];

                MaxTileDim = Math.Max(TileX, TileY);

                for (int i = 0; i < BlockTypeCount; i++) {
                    for (int y = 0; y < TileY; y++) {
                        for (int x = 0; x < TileX; x++) {
                            int p = i*TileStride + (y*TileX + x)*4;
                            Color c = tilesBmp.GetPixel(x + i*TileX, y);
                            Tiles[p] = c.B;
                            Tiles[p + 1] = c.G;
                            Tiles[p + 2] = c.R;
                            Tiles[p + 3] = c.A;
                        }
                    }
                }
            }

            using (Bitmap stilesBmp = Resources.TilesetShadowed) {
                ShadowTiles = new byte[BlockTypeCount*TileStride];

                for (int i = 0; i < BlockTypeCount; i++) {
                    for (int y = 0; y < TileY; y++) {
                        for (int x = 0; x < TileX; x++) {
                            int p = i*TileStride + (y*TileX + x)*4;
                            Color c = stilesBmp.GetPixel(x + i*TileX, y);
                            ShadowTiles[p] = c.B;
                            ShadowTiles[p + 1] = c.G;
                            ShadowTiles[p + 2] = c.R;
                            ShadowTiles[p + 3] = c.A;
                        }
                    }
                }
            }
        }


        [CanBeNull]
        public BoundingBox Chunk { get; set; }

        public int Rotation { get; set; }

        public IsoCatMode Mode { get; set; }

        public bool SeeThroughWater { get; set; }
        public bool SeeThroughLava { get; set; }
        public bool DrawShadows { get; set; }
        public bool Gradient { get; set; }


        public IsoCat() {
            Rotation = 0;
            Mode = IsoCatMode.Normal;
            Chunk = null;
            DrawShadows = true;
            Gradient = true;
        }


        byte* bp, ctp, image;
        int blendDivisor, mh34;
        int x, y, z;
        Map map;
        BitmapData imageData;
        int imageWidth, imageHeight;
        int dimX, dimY, dimX1, dimY1, dimX2, dimY2;
        int offsetX, offsetY;
        int isoOffset, isoX, isoY, isoH;
        int imageStride;


        [NotNull]
        [Pure]
        public IsoCatResult Draw([NotNull] Map mapToDraw) {
            if (mapToDraw == null) throw new ArgumentNullException("mapToDraw");
            isCancelled = false;
            map = mapToDraw;

            if (Mode == IsoCatMode.Chunk && Chunk == null) {
                throw new InvalidOperationException(
                    "When IsoCatMode is set to \"Chunk\", chunk boundaries must be set before calling Draw()");
            }

            x = y = z = 0;
            dimX = map.Width;
            dimY = map.Length;
            offsetY = Math.Max(0, map.Width - map.Length);
            offsetX = Math.Max(0, map.Length - map.Width);
            dimX2 = dimX/2 - 1;
            dimY2 = dimY/2 - 1;
            dimX1 = dimX - 1;
            dimY1 = dimY - 1;

            blendDivisor = 255*map.Height;

            imageWidth = TileX*Math.Max(dimX, dimY) + TileY/2*map.Height + TileX*2;
            imageHeight = TileY/2*map.Height + MaxTileDim/2*Math.Max(Math.Max(dimX, dimY), map.Height) +
                          TileY*2;

            short[][] shadows;
            if (DrawShadows) {
                shadows = map.ComputeHeightmap();
            } else {
                shadows = new short[map.Width][];
                for (int i = 0; i < map.Width; i++) {
                    shadows[i] = new short[map.Length];
                }
            }

            Bitmap imageBmp = null;
            try {
                imageBmp = new Bitmap(imageWidth, imageHeight, PixelFormat.Format32bppArgb);
                imageData = imageBmp.LockBits(new Rectangle(0, 0, imageBmp.Width, imageBmp.Height),
                                              ImageLockMode.ReadWrite,
                                              PixelFormat.Format32bppArgb);

                image = (byte*)imageData.Scan0;
                imageStride = imageData.Stride;

                isoOffset = (map.Height*TileY/2*imageStride + imageStride/2 + TileX*2);
                isoX = (TileX/4*imageStride + TileX*2);
                isoY = (TileY/4*imageStride - TileY*2);
                isoH = (-TileY/2*imageStride);

                mh34 = map.Height*3/4;

                fixed (byte* bpx = map.Blocks,
                    tp = Tiles,
                    stp = ShadowTiles) {
                    bp = bpx;
                    while (z < map.Height) {
                        byte block = GetBlock(x, y, z);
                        if (block != 0 && block < BlockTypeCount) {
                            switch (Rotation) {
                                case 0:
                                    ctp = (z >= shadows[x][y] ? tp : stp);
                                    break;
                                case 1:
                                    ctp = (z >= shadows[dimX1 - y][x] ? tp : stp);
                                    break;
                                case 2:
                                    ctp = (z >= shadows[dimX1 - x][dimY1 - y] ? tp : stp);
                                    break;
                                case 3:
                                    ctp = (z >= shadows[y][dimY1 - x] ? tp : stp);
                                    break;
                            }

                            int blockRight = (x != (Rotation == 1 || Rotation == 3 ? dimY1 : dimX1))
                                                 ? GetBlock(x + 1, y, z)
                                                 : 0;
                            int blockLeft = (y != (Rotation == 1 || Rotation == 3 ? dimX1 : dimY1))
                                                ? GetBlock(x, y + 1, z)
                                                : 0;
                            int blockUp = (z != map.Height - 1)
                                              ? GetBlock(x, y, z + 1)
                                              : 0;

                            if (blockUp == 0 || blockLeft == 0 || blockRight == 0 || // air
                                (block != 8 && block != 9 || !SeeThroughWater) &&
                                (blockUp == 8 || blockLeft == 8 || blockRight == 8 || blockUp == 9 || blockLeft == 9 ||
                                 blockRight == 9) || // water
                                (block != 10 && block != 11 || !SeeThroughLava) &&
                                (blockUp == 10 || blockLeft == 10 || blockRight == 10 || blockUp == 11 ||
                                 blockLeft == 11 || blockRight == 11) || // lava
                                block != 20 && (blockUp == 20 || blockLeft == 20 || blockRight == 20) || // glass
                                blockUp == 18 || blockLeft == 18 || blockRight == 18 || // foliage
                                blockLeft == 44 || blockRight == 44 || // step
                                blockUp == 37 || blockLeft == 37 || blockRight == 37 || // flower
                                blockUp == 38 || blockLeft == 38 || blockRight == 38 || // flower
                                blockUp == 6 || blockLeft == 6 || blockRight == 6 || // sapling
                                blockUp == 39 || blockLeft == 39 || blockRight == 39 ||
                                blockUp == 40 || blockLeft == 40 || blockRight == 40) // mushroom
                                BlendTile(block);
                        }

                        x++;
                        if (x == (Rotation == 1 || Rotation == 3 ? dimY : dimX)) {
                            y++;
                            x = 0;
                        }
                        if (y == (Rotation == 1 || Rotation == 3 ? dimX : dimY)) {
                            z++;
                            y = 0;
                            if (z%8 == 0) {
                                if (isCancelled) return CancelledResult;
                                ReportProgress(z/(float)map.Height);
                            }
                        }
                    }
                }

                Rectangle cropRectangle = CalculateCropBounds(imageBmp);
                if (isCancelled) {
                    return CancelledResult;
                } else {
                    return new IsoCatResult(false, imageBmp, cropRectangle);
                }
            } finally {
                if (imageBmp != null) {
                    imageBmp.UnlockBits(imageData);
                    if (isCancelled) {
                        imageBmp.Dispose();
                    }
                }
            }
        }


        Rectangle CalculateCropBounds([NotNull] Bitmap imageBmp) {
            if (imageBmp == null) throw new ArgumentNullException("imageBmp");
            int xMin = 0,
                xMax = imageWidth - 1,
                yMin = 0,
                yMax = imageHeight - 1;
            bool cont = true;
            int offset;

            // find left bound (xMin)
            for (x = 0; cont && x < imageWidth; x++) {
                offset = x*4 + 3;
                for (y = 0; y < imageHeight; y++) {
                    if (image[offset] > 0) {
                        xMin = x;
                        cont = false;
                        break;
                    }
                    offset += imageStride;
                }
            }

            if (isCancelled) return default(Rectangle);

            // find top bound (yMin)
            cont = true;
            for (y = 0; cont && y < imageHeight; y++) {
                offset = imageStride*y + xMin*4 + 3;
                for (x = xMin; x < imageWidth; x++) {
                    if (image[offset] > 0) {
                        yMin = y;
                        cont = false;
                        break;
                    }
                    offset += 4;
                }
            }

            if (isCancelled) return default(Rectangle);

            // find right bound (xMax)
            cont = true;
            for (x = imageWidth - 1; cont && x >= xMin; x--) {
                offset = x*4 + 3 + yMin*imageStride;
                for (y = yMin; y < imageHeight; y++) {
                    if (image[offset] > 0) {
                        xMax = x + 1;
                        cont = false;
                        break;
                    }
                    offset += imageStride;
                }
            }

            if (isCancelled) return default(Rectangle);

            // find bottom bound (yMax)
            cont = true;
            for (y = imageHeight - 1; cont && y >= yMin; y--) {
                offset = imageStride*y + 3 + xMin*4;
                for (x = xMin; x < xMax; x++) {
                    if (image[offset] > 0) {
                        yMax = y + 1;
                        cont = false;
                        break;
                    }
                    offset += 4;
                }
            }

            return new Rectangle(Math.Max(0, xMin - 2),
                                 Math.Max(0, yMin - 2),
                                 Math.Min(imageBmp.Width, xMax - xMin + 4),
                                 Math.Min(imageBmp.Height, yMax - yMin + 4));
        }


        void BlendTile(byte block) {
            int pos = (x + (Rotation == 1 || Rotation == 3 ? offsetY : offsetX))*isoX +
                      (y + (Rotation == 1 || Rotation == 3 ? offsetX : offsetY))*isoY +
                      z*isoH + isoOffset;
            int tileOffset = block*TileStride;
            BlendPixel(pos, tileOffset);
            BlendPixel(pos + 4, tileOffset + 4);
            BlendPixel(pos + 8, tileOffset + 8);
            BlendPixel(pos + 12, tileOffset + 12);
            pos += imageStride;
            BlendPixel(pos, tileOffset + 16);
            BlendPixel(pos + 4, tileOffset + 20);
            BlendPixel(pos + 8, tileOffset + 24);
            BlendPixel(pos + 12, tileOffset + 28);
            pos += imageStride;
            BlendPixel(pos, tileOffset + 32);
            BlendPixel(pos + 4, tileOffset + 36);
            BlendPixel(pos + 8, tileOffset + 40);
            BlendPixel(pos + 12, tileOffset + 44);
            pos += imageStride;
            //BlendPixel( pos, tileOffset + 48 ); // bottom left block, always blank in current tileset
            BlendPixel(pos + 4, tileOffset + 52);
            BlendPixel(pos + 8, tileOffset + 56);
            //BlendPixel( pos + 12, tileOffset + 60 ); // bottom right block, always blank in current tileset
        }


        // inspired by http://www.devmaster.net/wiki/Alpha_blending
        void BlendPixel(int imageOffset, int tileOffset) {
            int sourceAlpha;
            if (ctp[tileOffset + 3] == 0) return;

            byte tA = ctp[tileOffset + 3];

            // Get final alpha channel.
            int finalAlpha = tA + ((255 - tA)*image[imageOffset + 3])/255;

            // Get percentage (out of 256) of source alpha compared to final alpha
            if (finalAlpha == 0) {
                sourceAlpha = 0;
            } else {
                sourceAlpha = tA*255/finalAlpha;
            }

            // Destination percentage is just the additive inverse.
            int destAlpha = 255 - sourceAlpha;

            if (Gradient) {
                // Apply shading
                if (z < (map.Height >> 1)) {
                    int shadow = (z >> 1) + mh34;
                    image[imageOffset] =
                        (byte)
                        ((ctp[tileOffset]*sourceAlpha*shadow + image[imageOffset]*destAlpha*map.Height)/blendDivisor);
                    image[imageOffset + 1] =
                        (byte)
                        ((ctp[tileOffset + 1]*sourceAlpha*shadow + image[imageOffset + 1]*destAlpha*map.Height)/
                         blendDivisor);
                    image[imageOffset + 2] =
                        (byte)
                        ((ctp[tileOffset + 2]*sourceAlpha*shadow + image[imageOffset + 2]*destAlpha*map.Height)/
                         blendDivisor);
                } else {
                    int shadow = (z - (map.Height >> 1))*48;
                    image[imageOffset] =
                        (byte)Math.Min(255, (ctp[tileOffset]*sourceAlpha + shadow + image[imageOffset]*destAlpha)/255);
                    image[imageOffset + 1] =
                        (byte)
                        Math.Min(255, (ctp[tileOffset + 1]*sourceAlpha + shadow + image[imageOffset + 1]*destAlpha)/255);
                    image[imageOffset + 2] =
                        (byte)
                        Math.Min(255, (ctp[tileOffset + 2]*sourceAlpha + shadow + image[imageOffset + 2]*destAlpha)/255);
                }
            } else {
                image[imageOffset] =
                    (byte)Math.Min(255, (ctp[tileOffset]*sourceAlpha + image[imageOffset]*destAlpha)/255);
                image[imageOffset + 1] =
                    (byte)Math.Min(255, (ctp[tileOffset + 1]*sourceAlpha + image[imageOffset + 1]*destAlpha)/255);
                image[imageOffset + 2] =
                    (byte)Math.Min(255, (ctp[tileOffset + 2]*sourceAlpha + image[imageOffset + 2]*destAlpha)/255);
            }
            image[imageOffset + 3] = (byte)finalAlpha;
        }


        byte GetBlock(int xx, int yy, int zz) {
            int realX, realY;
            switch (Rotation) {
                case 0:
                    realX = xx;
                    realY = yy;
                    break;
                case 1:
                    realX = dimX1 - yy;
                    realY = xx;
                    break;
                case 2:
                    realX = dimX1 - xx;
                    realY = dimY1 - yy;
                    break;
                default:
                    realX = yy;
                    realY = dimY1 - xx;
                    break;
            }
            int pos = (zz*dimY + realY)*dimX + realX;

            switch (Mode) {
                case IsoCatMode.Normal:
                    return bp[pos];

                case IsoCatMode.Peeled:
                    if (xx == (Rotation == 1 || Rotation == 3 ? dimY1 : dimX1) ||
                        yy == (Rotation == 1 || Rotation == 3 ? dimX1 : dimY1) ||
                        zz == map.Height - 1) {
                        return 0;
                    } else {
                        return bp[pos];
                    }

                case IsoCatMode.Cut:
                    if (xx > (Rotation == 1 || Rotation == 3 ? dimY2 : dimX2) &&
                        yy > (Rotation == 1 || Rotation == 3 ? dimX2 : dimY2)) {
                        return 0;
                    } else {
                        return bp[pos];
                    }

                case IsoCatMode.Chunk:
                    if (Chunk.Contains(realX, realY, zz)) {
                        return 0;
                    } else {
                        return bp[pos];
                    }

                default:
                    throw new InvalidOperationException("Unrecognized IsoCatMode");
            }
        }

        #region Progress Reporting and Cancellation

        public event ProgressChangedEventHandler ProgressChanged;


        void ReportProgress(float progress) {
            var handler = ProgressChanged;
            if (handler != null) {
                handler(this, new ProgressChangedEventArgs((int)Math.Round(100*progress), "Drawing"));
            }
        }


        public void CancelAsync() {
            isCancelled = true;
        }


        volatile bool isCancelled;

        static readonly IsoCatResult CancelledResult = new IsoCatResult(true, null, default(Rectangle));

        #endregion
    }
}
