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
        private Managers.MapManager? Maps;

        public override void Entry(IModHelper helper)
        {
            this.Monitor.Log("Time Change mod loaded. Initializing Seasons of Time...", LogLevel.Info);

            this.Config = this.Helper.ReadConfig<ModConfig>();
            
            this.Lifecycle = new Managers.LifecycleManager(this.Monitor, this.Config);
            this.Assets = new Managers.AssetManager(this.Helper, this.Monitor);
            this.Funerals = new Managers.FuneralManager(this.Helper, this.Monitor);
            this.Maps = new Managers.MapManager(this.Helper, this.Monitor);

            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.Saving += OnSaving;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.DayEnding += OnDayEnding;

            helper.ConsoleCommands.Add("debug_kill", "[Build v4] Instantly kills an NPC for testing.\n\nUsage: debug_kill <name>", this.OnDebugKill);
        }

        private void OnDebugKill(string command, string[] args)
        {
            if (args.Length == 0)
            {
                this.Monitor.Log("Usage: debug_kill <name>", LogLevel.Error);
                return;
            }

            string name = args[0];
            if (this.Data.NpcStates.TryGetValue(name, out var npc))
            {
                npc.Alive = false;
                npc.LifeStage = LifeStage.Deceased;
                this.Monitor.Log($"Killed {name}. They should have a funeral pending now.", LogLevel.Alert);
                
                if (!this.Data.PendingFunerals.Contains(name))
                {
                    this.Data.PendingFunerals.Add(name);
                }
            }
            else
            {
                this.Monitor.Log($"NPC '{name}' not found in mod data.", LogLevel.Error);
            }
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            this.Data = this.Helper.Data.ReadSaveData<ModData>(DataKey) ?? new ModData();
            
            this.Monitor.Log("Save loaded. Data will be verified on day start.", LogLevel.Debug);

            this.Assets?.SetData(this.Data);
            this.Funerals?.SetData(this.Data);
        }

        private void OnSaving(object? sender, SavingEventArgs e)
        {
            this.Helper.Data.WriteSaveData(DataKey, this.Data);
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            // Ensure data is populated for all current NPCs
            InitializeDefaultData();

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
            
            // Safely iterate over all existing characters in the world
            // This avoids loading "Data/NPCDispositions" which causes crashes in 1.6
            foreach (var npc in Utility.getAllCharacters())
            {
                // Filter out monsters, pets, horses, etc. by checking if they are villagers
                if (npc.isVillager() && !this.Data.NpcStates.ContainsKey(npc.Name))
                {
                    this.Data.NpcStates[npc.Name] = new NPCData(npc.Name, 25);
                    this.Monitor.Log($"Initialized new NPC: {npc.Name}", LogLevel.Trace);
                }
            }
        }
    }
}