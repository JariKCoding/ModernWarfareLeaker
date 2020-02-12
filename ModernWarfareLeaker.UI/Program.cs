using System;
using ModernWarfareLeaker.Library;

namespace ModernWarfareLeaker.UI
{
    class Program
    {
        /// <summary>
        /// Active Instance
        /// </summary>
        private static LeakerInstance Instance = new LeakerInstance();
        
        static void Main(string[] args)
        {
            Console.WriteLine("ModernWarfareLeaker by JariK");

            var status = Instance.LoadGame();

            foreach (var asset in Instance.Assets)
            {
                if (asset.AssetPool.Name != "weapon")
                {
                    asset.AssetPool.Export(asset, Instance);
                }
            }
            Console.WriteLine(status);
        }
    }
}