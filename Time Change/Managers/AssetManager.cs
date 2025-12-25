using System.IO;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using Time_Change.Models;

namespace Time_Change.Managers
{
    public class AssetManager
    {
        private readonly IModHelper Helper;
        private readonly IMonitor Monitor;
        
        // Reference to the mod data to look up ages
        private ModData? Data;

        public AssetManager(IModHelper helper, IMonitor monitor)
        {
            this.Helper = helper;
            this.Monitor = monitor;
            
            // Subscribe to asset events
            helper.Events.Content.AssetRequested += OnAssetRequested;
        }

        public void SetData(ModData data)
        {
            this.Data = data;
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (this.Data == null) return;

            // Check if the asset is a Portrait or Character sprite
            if (e.Name.StartsWith("Portraits/") || e.Name.StartsWith("Characters/"))
            {
                // Extract NPC name
                // e.g. "Portraits/Abigail" -> "Abigail"
                string assetName = e.Name.Name; // Normalized path
                string npcName = Path.GetFileName(assetName);

                if (this.Data.NpcStates.TryGetValue(npcName, out var npcData))
                {
                    // Always try to load the variant for the current life stage.
                    // This handles cases like Vincent (Vanilla=Child, needs Custom=Adult)
                    // and Abigail (Vanilla=Adult, needs Custom=Child/Elder).
                    TryLoadVariant(e, npcName, npcData.LifeStage);
                }
            }
        }

        private void TryLoadVariant(AssetRequestedEventArgs e, string npcName, LifeStage stage)
        {
            // Expected local path: "assets/Portraits/Abigail_Child.png"
            string type = e.Name.StartsWith("Portraits") ? "Portraits" : "Characters";
            string variantName = $"{npcName}_{stage}"; // e.g. Abigail_Child
            string localPath = Path.Combine("assets", type, $"{variantName}.png");

            if (File.Exists(Path.Combine(this.Helper.DirectoryPath, localPath)))
            {
                e.LoadFromModFile<Microsoft.Xna.Framework.Graphics.Texture2D>(localPath, AssetLoadPriority.Medium);
                this.Monitor.Log($"Replaced {type} for {npcName} with {stage} variant.", LogLevel.Trace);
            }
        }
    }
}
