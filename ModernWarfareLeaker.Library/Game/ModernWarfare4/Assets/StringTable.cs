using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ModernWarfareLeaker.Library
{
    public partial class ModernWarfare4
    {
        public class StringTable : IAssetPool
        {
            #region AssetStructures
            /// <summary>
            /// String Table Asset Structure
            /// </summary>
            private struct StringTableAsset
            {
                /// <summary>
                /// String Table Cell Structure
                /// </summary>
                public long NamePointer;
                public int ColumnCount;
                public int RowCount;
                public int Unk;
                public long CellsPointer;
                public long IndicesPointer;
                public long StringsPtr;
            }
            #endregion
            public string Name => "stringtable";
            public int Index => (int) AssetPool.stringtable;
            public int AssetSize { get; set; }
            public int AssetCount { get; set; }
            public long StartAddress { get; set; }
            public long EndAddress { get { return StartAddress + (AssetCount * AssetSize); } set => throw new NotImplementedException(); }
            public List<GameAsset> Load(LeakerInstance instance)
            {
                var results = new List<GameAsset>();

                var poolInfo = instance.Reader.ReadStruct<AssetPoolInfo>(instance.Game.BaseAddress + instance.Game.AssetPoolsAddresses[instance.Game.ProcessIndex] + (Index * 24));
                
                StartAddress = poolInfo.PoolPtr;
                AssetSize = poolInfo.AssetSize;
                AssetCount = poolInfo.PoolSize;

                for (int i = 0; i < AssetCount; i++)
                {
                    var header = instance.Reader.ReadStruct<StringTableAsset>(StartAddress + (i * AssetSize));

                    if (IsNullAsset(header.NamePointer))
                        continue;

                    results.Add(new GameAsset()
                    {
                        Name = instance.Reader.ReadNullTerminatedString(header.NamePointer),
                        HeaderAddress = StartAddress + (i * AssetSize),
                        AssetPool = this,
                        Type = Name,
                        Information = string.Format("Rows: {0} - Columns: {1}", header.RowCount, header.ColumnCount)
                    });
                }
                
                return results;
            }

            public LeakerStatus Export(GameAsset asset, LeakerInstance instance)
            {
                var header = instance.Reader.ReadStruct<StringTableAsset>(asset.HeaderAddress);
                
                if (asset.Name != instance.Reader.ReadNullTerminatedString(header.NamePointer))
                    return LeakerStatus.MemoryChanged;
                
                string path = Path.Combine("exported_files", instance.Game.Name, asset.Name);
                
                // Create path
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                // Output result
                var result = new StringBuilder();
                
                // Loop through rows
                int index = 0;
                for (int x = 0; x < header.RowCount; x++)
                {
                    // Loop through columns for this row
                    for (int y = 0; y < header.ColumnCount; y++)
                    {
                        int stringIndex = instance.Reader.ReadInt16(header.CellsPointer + (2 * index));
                        string str = instance.Reader.ReadNullTerminatedString(instance.Reader.ReadInt64(header.StringsPtr + (8 * stringIndex)));
                        result.Append(str + ",");
                        index++;
                    }
                    // Create new line
                    result.AppendLine();
                }
                // Write result
                File.WriteAllText(path, result.ToString());
                
                return LeakerStatus.Success;
            }

            public bool IsNullAsset(GameAsset asset)
            {
                return IsNullAsset(asset.NameLocation);
            }

            public bool IsNullAsset(long nameAddress)
            {
                return nameAddress >= StartAddress && nameAddress <= AssetCount * AssetSize + StartAddress || nameAddress == 0;
            }
        }
    }
}