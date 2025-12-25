using System;
using System.IO;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using xTile;

namespace Time_Change.Managers
{
    public class MapManager
    {
        private readonly IModHelper Helper;
        private readonly IMonitor Monitor;
        
        // The internal name for our custom location
        public const string CemeteryLocationName = "SeasonsOfTime_Cemetery";

        public MapManager(IModHelper helper, IMonitor monitor)
        {
            this.Helper = helper;
            this.Monitor = monitor;

            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            // Load the map file when requested
            if (e.Name.IsEquivalentTo($"Maps/{CemeteryLocationName}"))
            {
                e.LoadFromModFile<Map>("assets/Maps/Cemetery.tmx", AssetLoadPriority.Medium);
            }
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            // Ensure the location exists in the game
            if (Game1.getLocationFromName(CemeteryLocationName) == null)
            {
                // Stardew 1.6+ uses this constructor often, or simply new GameLocation(path, name)
                // Actually, if we loaded the map object manually, we should use:
                // new GameLocation(mapPath, name) where mapPath is the content manager path.
                
                var location = new GameLocation($"Maps/{CemeteryLocationName}", CemeteryLocationName);
                Game1.locations.Add(location);
                this.Monitor.Log($"Added custom location: {CemeteryLocationName}", LogLevel.Info);
            }

            // Add a warp to the Town map so players can access it
            // Assuming coordinates near the existing graveyard in Town (approx 40, 70 is near the river/bridge to beach, 
            // but let's put it north of the blacksmith or similar).
            // Let's place it near the Yoba shrine/Pierre's for now or just generic.
            // Town 70 10 is near the top right. 
            // We'll add a warp at Town 95 15 (East side, path to Joja?). 
            // Better: Behind the graveyard. Town 18 10 is near top left graveyard.
            
            var town = Game1.getLocationFromName("Town");
            if (town != null)
            {
                // X: 18, Y: 10 is roughly top left near the existing graves.
                // We add a tile property or just a warp.
                // Note: Adding a warp via code is safer than editing the map asset for compatibility.
                
                // Add warp from Town -> Cemetery
                // 18, 10 in Town -> 10, 18 in Cemetery
                town.warps.Add(new Warp(18, 10, CemeteryLocationName, 10, 18, false));
                
                // Add warp from Cemetery -> Town
                var cemetery = Game1.getLocationFromName(CemeteryLocationName);
                if (cemetery != null)
                {
                    cemetery.warps.Add(new Warp(10, 19, "Town", 18, 11, false));
                }
            }
        }
    }
}
