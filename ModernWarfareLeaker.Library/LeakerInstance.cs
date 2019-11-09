using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using PhilLibX.IO;

namespace ModernWarfareLeaker.Library
{
    public class LeakerInstance
    {
        /// <summary>
        /// Gets or Sets the List of Supported Games
        /// </summary>
        public List<IGame> Games { get; set; }

        /// <summary>
        /// Gets or Sets the current Game
        /// </summary>
        public IGame Game { get; set; }
        
        /// <summary>
        /// Gets or Sets the current Process Reader
        /// </summary>
        public ProcessReader Reader { get; set; }
        
        /// <summary>
        /// Gets or Sets the loaded Assets
        /// </summary>
        public List<GameAsset> Assets { get; set; }
        
        /// <summary>
        /// Gets the Export Path
        /// </summary>
        public string ExportFolder { get { return Path.Combine("exported_files", Game.Name); } }
        
        /// <summary>
        /// Gets the Sound Path
        /// </summary>
        public string SoundFolder { get { return Path.Combine(ExportFolder, "sound"); } }
        
        /// <summary>
        /// Gets the Sound Zone Path
        /// </summary>
        public string SoundZoneFolder { get { return Path.Combine(SoundFolder, "zone"); } }

        public LeakerInstance()
        {
            Games = new List<IGame>();
            var gameType = typeof(IGame);
            var games = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(p => gameType.IsAssignableFrom(p));

            foreach (var game in games)
                if (!game.IsInterface)
                    Games.Add((IGame)Activator.CreateInstance(game));
        }
        
        /// <summary>
        /// Checks to ensure game is running and hasn't changed
        /// </summary>
        public LeakerStatus ValidateGame()
        {
            if (Reader != null && Game != null)
            {
                Process[] processes = Process.GetProcessesByName(Game.ProcessNames[Game.ProcessIndex]);

                if (processes.Length == 0)
                    return LeakerStatus.GameClosed;
                if (processes[0].Id != Reader.ActiveProcess.Id)
                    return LeakerStatus.MemoryChanged;

                return LeakerStatus.Success;
            }

            return LeakerStatus.GameClosed;
        }
        
        /// <summary>
        /// Gets Asset Pools for the given Game
        /// </summary>
        public static List<IAssetPool> GetAssetPools(IGame game)
        {
            var poolType = typeof(IAssetPool);
            var results = new List<IAssetPool>();

            var pools = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(p => poolType.IsAssignableFrom(p));

            foreach (var pool in pools)
                if (!pool.IsInterface)
                    if (pool.DeclaringType is Type gameType)
                        if (gameType == game.GetType())
                            results.Add((IAssetPool)Activator.CreateInstance(pool));

            return results;
        }

        public LeakerStatus LoadGame()
        {
            Process[] processes = Process.GetProcesses();
            
            var status = LeakerStatus.FailedToFindGame;

            foreach (var process in processes)
            {
                foreach (var game in Games)
                {
                    for (int i = 0; i < game.ProcessNames.Length; i++)
                    {
                        if (process.ProcessName == game.ProcessNames[i])
                        {
                            Game = (IGame)game.Clone();
                            Game.ProcessIndex = i;
                            Reader = new ProcessReader(process);

                            if (Game.ValidateAddresses(this))
                            {
                                Game.AssetPools = GetAssetPools(Game);

                                Assets = new List<GameAsset>();

                                foreach (var assetPool in Game.AssetPools)
                                {
                                    Assets.AddRange(assetPool.Load(this));
                                }
                                
                                status = LeakerStatus.Success;
                            }
                            else
                            {
                                status = LeakerStatus.UnsupportedBinary;
                            }

                            break;
                        }
                    }
                }
            }

            return status;
        }
    }
}