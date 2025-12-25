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
            helper.Events.Input.ButtonPressed += OnButtonPressed;
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
                var location = new GameLocation($"Maps/{CemeteryLocationName}", CemeteryLocationName);
                Game1.locations.Add(location);
                this.Monitor.Log($"Added custom location: {CemeteryLocationName}", LogLevel.Info);
            }
            
            var seedShop = Game1.getLocationFromName("SeedShop");
            if (seedShop != null)
            {
                // Yoba Shrine area. Moving to 36, 18 to be accessible (in front of altar).
                int warpX = 36;
                int warpY = 18;
                
                // Add warp from SeedShop -> Cemetery
                seedShop.warps.Add(new Warp(warpX, warpY, CemeteryLocationName, 10, 18, false));
                
                // Add warp from Cemetery -> SeedShop
                var cemetery = Game1.getLocationFromName(CemeteryLocationName);
                if (cemetery != null)
                {
                    cemetery.warps.Add(new Warp(10, 19, "SeedShop", warpX, warpY + 1, false));
                }

                // Make it visible: Place a "Sign of the Vessel" (Gold Statue)
                var markerPos = new Microsoft.Xna.Framework.Vector2(warpX, warpY);
                
                // FORCE replace any existing object there (in case of old invisible ones)
                if (seedShop.objects.ContainsKey(markerPos))
                {
                    seedShop.objects.Remove(markerPos);
                }

                var marker = new StardewValley.Object(markerPos, "37", false);
                seedShop.objects.Add(markerPos, marker);
                this.Monitor.Log($"Placed Golden Marker at {warpX},{warpY} in SeedShop.", LogLevel.Info);
            }
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            // Only listen for Action (Right-Click/A-Button)
            if (!e.Button.IsActionButton()) return;

            // Must be in SeedShop
            if (Game1.currentLocation?.Name != "SeedShop") return;

            // Get the tile the player is interacting with
            Vector2 grabbedTile = e.Cursor.GrabTile;
            
            // Check if it matches Yoba Shrine coordinates (New coord 36, 18)
            // Allow clicking the marker itself or adjacent
            bool isShrine = (Math.Abs(grabbedTile.X - 36) <= 1) && (Math.Abs(grabbedTile.Y - 18) <= 1);

            if (isShrine)
            {
                this.Monitor.Log("Interacted with Yoba Shrine - Warping to Cemetery...", LogLevel.Info);
                
                // Warp the player
                Game1.warpFarmer(CemeteryLocationName, 10, 18, 0);
                
                // Suppress default interaction (the message)
                this.Helper.Input.Suppress(e.Button);
            }
        }
    }
}
