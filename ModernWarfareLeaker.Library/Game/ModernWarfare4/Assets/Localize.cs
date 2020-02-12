using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ModernWarfareLeaker.Library
{
    public partial class ModernWarfare4
    {
        public class Localize : IAssetPool
        {
            #region AssetStructures
            /// <summary>
            /// TTF Asset Structure
            /// </summary>
            private struct LocalizeAsset
            {
                public long NamePointer;
                public long RawDataPtr;
            }
            #endregion
            public override string Name => "localize";
            public override int Index => (int) AssetPool.localize;
            public override long EndAddress { get { return StartAddress + (AssetCount * AssetSize); } set => throw new NotImplementedException(); }
            
            public override List<GameAsset> Load(LeakerInstance instance)
            {
                var results = new List<GameAsset>();

                var poolInfo = instance.Reader.ReadStruct<AssetPoolInfo>(instance.Game.BaseAddress + instance.Game.AssetPoolsAddresses[instance.Game.ProcessIndex] + (Index * 24));
                
                StartAddress = poolInfo.PoolPtr;
                AssetSize = poolInfo.AssetSize;
                AssetCount = poolInfo.PoolSize;
                
                Dictionary<string, int> dictionary = new Dictionary<string, int>();
                
                for (int i = 0; i < AssetCount; i++)
                {
                    var header = instance.Reader.ReadStruct<LocalizeAsset>(StartAddress + (i * AssetSize));

                    if (IsNullAsset(header.NamePointer))
                        continue;
                    // Not optimized at all but idc
                    string name = instance.Reader.ReadNullTerminatedString(header.NamePointer);
                    int idx = name.IndexOf('/');
                    string fileName = name.Substring(0, idx);

                    if (dictionary.TryGetValue(fileName, out int value))
                    {
                        dictionary.Remove(fileName);
                        dictionary.Add(fileName, value + 1);
                    }
                    else
                    {
                        dictionary.Add(fileName, 0);
                    }
                }

                foreach (var file in dictionary)
                {
                    results.Add(new GameAsset()
                    {
                        Name = file.Key,
                        HeaderAddress = StartAddress,
                        AssetPool = this,
                        Type = Name,
                        Information = string.Format("Strings: 0x{0:X}", file.Value)
                    });
                }
                
                return results;
            }

            public override LeakerStatus Export(GameAsset asset, LeakerInstance instance)
            {
                string path = Path.Combine("exported_files", instance.Game.Name, "localizedstrings", asset.Name + ".str");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var result = new StringBuilder();
                result.AppendLine("VERSION				\"1\"");
                result.AppendLine("CONFIG				\"C:\\projects\\cod\\t7\\bin\\StringEd.cfg\"");
                result.AppendLine("FILENOTES		    \"Dumped via ModernWarfareLeaker by JariK\"");
                result.AppendLine();
                
                var result2 = new StringBuilder();
                
                for (int i = 0; i < AssetCount; i++)
                {
                    var header = instance.Reader.ReadStruct<LocalizeAsset>(StartAddress + (i * AssetSize));

                    if (IsNullAsset(header.NamePointer))
                        continue;
                    
                    string name = instance.Reader.ReadNullTerminatedString(header.NamePointer);
                    int idx = name.IndexOf('/');
                    string fileName = name.Substring(0, idx);

                    if (asset.Name == fileName)
                    {
                        result.AppendLine(String.Format("REFERENCE            {0}", name.Substring(idx + 1)));
                        result.AppendLine(String.Format("LANG_ENGLISH         \"{0}\"", instance.Reader.ReadNullTerminatedString(header.RawDataPtr)));
                        result.AppendLine();
                        result2.AppendLine(name + "," + instance.Reader.ReadNullTerminatedString(header.RawDataPtr));
                    }
                }
                result.AppendLine();
                result.AppendLine("ENDMARKER");
                // Write result
                File.WriteAllText(path, result.ToString());
                //File.WriteAllText(path + "2", result2.ToString());

                return LeakerStatus.Success;
            }
        }
    }
}