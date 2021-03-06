using System;
using System.Collections.Generic;
using fNbt;
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    /// <summary> Schematic conversion implementation, for exporting fCraft maps to MCEdit and WorldEdit
    /// with converted modern materials. </summary>
    internal class MapModernSchematic : MapSchematic {
        const int ModernWoolBlockID = 35,
                  ModernDiamondBlockID = 57,
                  ModernEmeraldBlockID = 133,
                  ModernHardenedClayBlockID = 159,
                  MagentaStainedClayData = 2,
                  LimeStainedClayData = 5;

        static readonly Dictionary<Block, byte> ModernWoolColorMapping = new Dictionary<Block, byte> {
            { Block.Red, 14 },
            { Block.Orange, 1 },
            { Block.Yellow, 4 },
            { Block.Lime, 5 },
            { Block.Green, 0 }, // replace with lime hardened clay
            { Block.Teal, 0 }, // replace with emerald block
            { Block.Aqua, 0 }, // replace with diamond block
            { Block.Cyan, 9 },
            { Block.Blue, 3 },
            { Block.Indigo, 10 },
            { Block.Violet, 2 },
            { Block.Magenta, 0 }, // replace with magenta hardened clay
            { Block.Pink, 6 },
            { Block.Black, 7 },
            { Block.Gray, 8 },
            { Block.White, 0 }
        };

        public override string ServerName {
            get { return "ModernSchematic"; }
        }

        public override MapFormat Format {
            get { return MapFormat.ModernSchematic; }
        }


        protected override void DoConversion([NotNull] NbtCompound rootTag) {
            if (rootTag == null) throw new ArgumentNullException("rootTag");
            byte[] blocksIDs = rootTag["Blocks"].ByteArrayValue;
            byte[] blockData = rootTag["Data"].ByteArrayValue;
            rootTag.Get<NbtString>("Materials").Value = "Alpha";
            for (int i = 0; i < blocksIDs.Length; i++) {
                Block block = (Block)blocksIDs[i];
                if (block >= Block.Red && block <= Block.White) {
                    // Convert wool colors
                    switch (block) {
                        case Block.Teal:
                            blocksIDs[i] = ModernEmeraldBlockID;
                            break;
                        case Block.Aqua:
                            blocksIDs[i] = ModernDiamondBlockID;
                            break;
                        case Block.Magenta:
                            blocksIDs[i] = ModernHardenedClayBlockID;
                            blockData[i] = MagentaStainedClayData;
                            break;
                        case Block.Green:
                            blocksIDs[i] = ModernHardenedClayBlockID;
                            blockData[i] = LimeStainedClayData;
                            break;
                        default:
                            blocksIDs[i] = ModernWoolBlockID;
                            blockData[i] = ModernWoolColorMapping[block];
                            break;
                    }
                }
            }
        }
    }
}
