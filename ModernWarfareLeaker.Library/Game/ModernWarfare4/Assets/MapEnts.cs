using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ModernWarfareLeaker.Library
{
    public partial class ModernWarfare4
    {
        public class MapEnts : IAssetPool
        {
            #region AssetStructures
            /// <summary>
            /// TTF Asset Structure
            /// </summary>
            private struct MapEntsAsset
            {
                public long NamePointer;
                public long RawDataPtr;
            }
            #endregion
            public override string Name => "mapents";
            public override int Index => (int) AssetPool.map_ents;
            
            public override long EndAddress { get { return StartAddress + (AssetCount * AssetSize); } set => throw new NotImplementedException(); }
            public override List<GameAsset> Load(LeakerInstance instance)
            {
                var results = new List<GameAsset>();

                var poolInfo = instance.Reader.ReadStruct<AssetPoolInfo>(instance.Game.BaseAddress + instance.Game.AssetPoolsAddresses[instance.Game.ProcessIndex] + (Index * 24));
                
                StartAddress = poolInfo.PoolPtr;
                AssetSize = poolInfo.AssetSize;
                AssetCount = poolInfo.PoolSize;

                for (int i = 0; i < AssetCount; i++)
                {
                    var header = instance.Reader.ReadStruct<MapEntsAsset>(StartAddress + (i * AssetSize));

                    if (IsNullAsset(header.NamePointer))
                        continue;
                    
                    results.Add(new GameAsset()
                    {
                        Name = instance.Reader.ReadNullTerminatedString(header.NamePointer),
                        HeaderAddress = StartAddress + (i * AssetSize),
                        AssetPool = this,
                        Type = Name,
                        Information = "N/A"
                    });
                }
                
                return results;
            }

            public override LeakerStatus Export(GameAsset asset, LeakerInstance instance)
            {
                var header = instance.Reader.ReadStruct<MapEntsAsset>(asset.HeaderAddress);
                
                if (asset.Name != instance.Reader.ReadNullTerminatedString(header.NamePointer))
                    return LeakerStatus.MemoryChanged;
                
                string path = Path.Combine("exported_files", instance.Game.Name, asset.Name);
                
                // Create path
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                
                File.WriteAllText(path, instance.Reader.ReadNullTerminatedString(header.RawDataPtr));
                
                return LeakerStatus.Success;
            }
        }
    }
}