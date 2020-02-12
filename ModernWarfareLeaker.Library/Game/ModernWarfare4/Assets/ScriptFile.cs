using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace ModernWarfareLeaker.Library
{
    public partial class ModernWarfare4
    {
        public class ScriptFile : IAssetPool
        {
            #region AssetStructures
            /// <summary>
            /// Lua File Asset Structure
            /// </summary>
            private struct ScriptFileAsset
            {
                public long NamePointer;
                public int compressedLength;
                public int len;
                public long byteCodeLen;
                public long buffer;
                public long bytecode;
            }
            #endregion
            public override string Name => "scriptfile";
            public override int Index => (int) AssetPool.scriptfile;
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
                    var header = instance.Reader.ReadStruct<ScriptFileAsset>(StartAddress + (i * AssetSize));

                    if (IsNullAsset(header.NamePointer))
                        continue;

                    if (header.buffer == 0)
                    {
                        continue;
                    }
                    
                    results.Add(new GameAsset()
                    {
                        Name = instance.Reader.ReadNullTerminatedString(header.NamePointer),
                        HeaderAddress = StartAddress + (i * AssetSize),
                        AssetPool = this,
                        Type = Name,
                        Information = string.Format("Size: 0x{0:X}", header.compressedLength)
                    });
                }
                
                return results;
            }
            
            public override LeakerStatus Export(GameAsset asset, LeakerInstance instance)
            {
                var header = instance.Reader.ReadStruct<ScriptFileAsset>(asset.HeaderAddress);
                
                if (asset.Name != instance.Reader.ReadNullTerminatedString(header.NamePointer))
                    return LeakerStatus.MemoryChanged;

                string addedScriptFolder = Path.Combine(asset.Name.Contains("scripts") ? "" : "scripts", asset.Name);
                string path = Path.Combine("exported_files", instance.Game.Name, addedScriptFolder);
                
                // Create path
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                
                MemoryStream DecodedCodeStream = Decode(instance.Reader.ReadBytes(header.buffer + 2, header.compressedLength - 2));
                using (var outputStream = new FileStream(path, FileMode.Create))
                {
                    DecodedCodeStream.CopyTo(outputStream);
                }
                
                return LeakerStatus.Success;
            }
            
            public static MemoryStream Decode(byte[] data)
            {
                MemoryStream output = new MemoryStream();
                MemoryStream input = new MemoryStream(data);

                using (DeflateStream deflateStream = new DeflateStream(input, CompressionMode.Decompress))
                {
                    deflateStream.CopyTo(output);
                }

                output.Flush();
                output.Position = 0;

                return output;
            }
        }
    }
    
}