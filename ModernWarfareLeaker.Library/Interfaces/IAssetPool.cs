using System.Collections.Generic;

namespace ModernWarfareLeaker.Library
{
    public interface IAssetPool
    {
        /// <summary>
        /// Gets the Asset Pool Name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the Setting Group for this Asset Pool
        /// </summary>
        //string SettingGroup { get; }

        /// <summary>
        /// Gets the Asset Pool Index
        /// </summary>
        int Index { get; }

        /// <summary>
        /// Gets the Asset Header Size
        /// </summary>
        int AssetSize { get; set; }

        /// <summary>
        /// Gets or Sets the number of Asset slots in this pool
        /// </summary>
        int AssetCount { get; set; }

        /// <summary>
        /// Gets or Sets the start Address of this pool
        /// </summary>
        long StartAddress { get; set; }

        /// <summary>
        /// Gets or Sets the end Address of this pool
        /// </summary>
        long EndAddress { get; set; }

        /// <summary>
        /// Loads Assets from the given Asset Pool
        /// </summary>
        List<GameAsset> Load(LeakerInstance instance);

        /// <summary>
        /// Exports the given asset from the game
        /// </summary>
        LeakerStatus Export(GameAsset asset, LeakerInstance instance);

        /// <summary>
        /// Checks if the given asset is null
        /// </summary>
        bool IsNullAsset(GameAsset asset);

        /// <summary>
        /// Checks if the given pointer points to a null slot
        /// </summary>
        bool IsNullAsset(long nameAddress);
    }
}