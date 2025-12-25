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
            
            // Note: We removed the physical warp in favor of an interaction event.
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            // Only listen for Action (Right-Click/A-Button)
            if (!e.Button.IsActionButton()) return;

            // Must be in SeedShop
            if (Game1.currentLocation?.Name != "SeedShop") return;

            // Get the tile the player is interacting with
            Vector2 grabbedTile = e.Cursor.GrabTile;
            
            // Check if it matches Yoba Shrine coordinates
            // The Shrine is roughly at 35,17 and 36,17
            // We'll check a small box area to be safe
            bool isShrine = (grabbedTile.X >= 35 && grabbedTile.X <= 37) && (grabbedTile.Y >= 16 && grabbedTile.Y <= 17);

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
