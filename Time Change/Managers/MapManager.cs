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

            // Add a warp to the SeedShop (Pierre's), specifically the Yoba Shrine area.
            // This is requested to be "in Pierres shop at the church".
            
            var seedShop = Game1.getLocationFromName("SeedShop");
            if (seedShop != null)
            {
                // Yoba Shrine is on the far right. Altar is roughly at 36, 17.
                // We'll place the warp behind/at the altar.
                int warpX = 36;
                int warpY = 16;
                
                // Add warp from SeedShop -> Cemetery
                seedShop.warps.Add(new Warp(warpX, warpY, CemeteryLocationName, 10, 18, false));
                
                // Add warp from Cemetery -> SeedShop
                var cemetery = Game1.getLocationFromName(CemeteryLocationName);
                if (cemetery != null)
                {
                    cemetery.warps.Add(new Warp(10, 19, "SeedShop", warpX, warpY + 1, false));
                }

                // Make it visible: Place a "Sign of the Vessel" (Gold Statue) or similar marker
                // Object ID 37 is the gold statue thing (Sign of the Vessel) which looks mystical.
                var markerPos = new Microsoft.Xna.Framework.Vector2(warpX, warpY);
                if (!seedShop.objects.ContainsKey(markerPos))
                {
                    // Using "37" (Sign of the Vessel) as a placeholder marker
                    // In 1.6, use ItemRegistry.Create or the correct constructor with string ID
                    // Simple constructor: new Object(Vector2, string itemId, ...)
                    var marker = new StardewValley.Object(markerPos, "37", false);
                    seedShop.objects.Add(markerPos, marker);
                    this.Monitor.Log("Placed marker for Cemetery entrance in SeedShop.", LogLevel.Info);
                }
            }
        }
    }
}
