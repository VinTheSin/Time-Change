using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Time_Change.Models;

namespace Time_Change
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        public ModData Data { get; private set; } = new();
        public ModConfig Config { get; private set; } = new();
        
        private const string DataKey = "seasons-of-time-data";
        private Managers.LifecycleManager? Lifecycle;
        private Managers.AssetManager? Assets;
        private Managers.FuneralManager? Funerals;

        public override void Entry(IModHelper helper)
        {
            this.Monitor.Log("Time Change mod loaded. Initializing Seasons of Time...", LogLevel.Info);

            this.Config = this.Helper.ReadConfig<ModConfig>();
            
            this.Lifecycle = new Managers.LifecycleManager(this.Monitor, this.Config);
            this.Assets = new Managers.AssetManager(this.Helper, this.Monitor);
            this.Funerals = new Managers.FuneralManager(this.Helper, this.Monitor);

            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.Saving += OnSaving;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.DayEnding += OnDayEnding;
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            this.Data = this.Helper.Data.ReadSaveData<ModData>(DataKey) ?? new ModData();
            
            if (this.Data.NpcStates.Count == 0)
            {
                this.Monitor.Log("No existing data found. Initializing default NPC states...", LogLevel.Info);
                InitializeDefaultData();
            }

            this.Assets?.SetData(this.Data);
            this.Funerals?.SetData(this.Data);
        }

        private void OnSaving(object? sender, SavingEventArgs e)
        {
            this.Helper.Data.WriteSaveData(DataKey, this.Data);
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            // Check for new year
            if (Game1.dayOfMonth == 1 && Game1.currentSeason == "spring")
            {
                if (Game1.year > this.Data.CurrentYear)
                {
                    this.Monitor.Log($"New Year detected! Transitioning from Year {this.Data.CurrentYear} to {Game1.year}.", LogLevel.Info);
                    if (this.Lifecycle != null)
                    {
                        this.Lifecycle.ProcessYearlyUpdate(this.Data);
                    }
                    this.Data.CurrentYear = Game1.year;
                }
            }
        }

        private void OnDayEnding(object? sender, DayEndingEventArgs e)
        {
            // Logic for day end
        }

        private void InitializeDefaultData()
        {
            this.Data.CurrentYear = Game1.year;
            
            // Iterate over all characters in the game to initialize them
            // Using Utility.getAllCharacters() might be too broad (includes monsters etc in some versions), 
            // but checking Game1.content for NPC dispositions is safer for "townies".
            
            var npcDispositions = Game1.content.Load<System.Collections.Generic.Dictionary<string, string>>("Data/NPCDispositions");
            
            foreach (var npcName in npcDispositions.Keys)
            {
                // Basic default: Everyone starts as Adult (age 20-30 range random?) or fixed.
                // For now, let's say everyone is 25.
                this.Data.NpcStates[npcName] = new NPCData(npcName, 25);
            }
            
            this.Monitor.Log($"Initialized {this.Data.NpcStates.Count} NPCs.", LogLevel.Info);
        }
    }
}