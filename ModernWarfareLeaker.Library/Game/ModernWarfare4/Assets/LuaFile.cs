using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ModernWarfareLeaker.Library
{
    public partial class ModernWarfare4
    {
        public class LuaFile : IAssetPool
        {
            #region AssetStructures
            /// <summary>
            /// Lua File Asset Structure
            /// </summary>
            private struct LuaFileAsset
            {
                public long NamePointer;
                public int AssetSize;
                public int Unk;
                public long RawDataPtr;
            }
            #endregion
            public string Name => "luafile";
            public int Index => (int) AssetPool.luafile;
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
                    var header = instance.Reader.ReadStruct<LuaFileAsset>(StartAddress + (i * AssetSize));

                    if (IsNullAsset(header.NamePointer))
                        continue;

                    results.Add(new GameAsset()
                    {
                        Name = instance.Reader.ReadNullTerminatedString(header.NamePointer),
                        HeaderAddress = StartAddress + (i * AssetSize),
                        AssetPool = this,
                        Type = Name,
                        Information = string.Format("Size: 0x{0:X}", header.AssetSize)
                    });
                }
                
                return results;
            }

            public LeakerStatus Export(GameAsset asset, LeakerInstance instance)
            {
                var header = instance.Reader.ReadStruct<LuaFileAsset>(asset.HeaderAddress);
                
                if (asset.Name != instance.Reader.ReadNullTerminatedString(header.NamePointer))
                    return LeakerStatus.MemoryChanged;
                
                string path = Path.Combine("exported_files", instance.Game.Name, asset.Name);
                
                // Create path
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                
                byte[] buffer = instance.Reader.ReadBytes(header.RawDataPtr, (int)header.AssetSize);

                File.WriteAllBytes(path, buffer);
                
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