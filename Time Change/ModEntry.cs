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

            helper.ConsoleCommands.Add("debug_kill", "[Build v6] Instantly kills an NPC for testing.\n\nUsage: debug_kill <name>", this.OnDebugKill);
            helper.ConsoleCommands.Add("debug_revive", "Revives a killed NPC.\n\nUsage: debug_revive <name>", this.OnDebugRevive);
        }

        private void OnDebugRevive(string command, string[] args)
        {
            if (args.Length == 0)
            {
                this.Monitor.Log("Usage: debug_revive <name>", LogLevel.Error);
                return;
            }

            string name = args[0];
            if (this.Data.NpcStates.TryGetValue(name, out var npcData))
            {
                npcData.Alive = true;
                // Reset to Adult or original stage? Defaulting to Adult for safety, or calculating from age.
                // Re-calculating stage based on age is safer.
                npcData.LifeStage = LifeStage.Adult; // Placeholder, LifecycleManager handles updates
                
                if (this.Data.PendingFunerals.Contains(name))
                {
                    this.Data.PendingFunerals.Remove(name);
                }

                this.Monitor.Log($"Revived {name}.", LogLevel.Alert);

                // Unhide the NPC
                // We need to re-add them to a default location (e.g. Town) or their home.
                // Simplest: Add to Town at default coords.
                var town = Game1.getLocationFromName("Town");
                if (town != null)
                {
                    // Check if character already exists (hidden)
                    var existing = Game1.getCharacterFromName(name);
                    if (existing != null)
                    {
                        existing.IsInvisible = false;
                        existing.HideShadow = false;
                        if (existing.currentLocation == null)
                        {
                            town.addCharacter(existing);
                            existing.Position = new Microsoft.Xna.Framework.Vector2(20 * 64, 20 * 64); // Arbitrary spot
                        }
                    }
                    else
                    {
                        // Reload character? This is hard if completely removed.
                        // Stardew usually keeps them in the global list or we rely on DayStart to reload them.
                        this.Monitor.Log("NPC entity might need a day reset to fully reappear.", LogLevel.Warn);
                    }
                }
            }
            else
            {
                this.Monitor.Log($"NPC '{name}' not found in mod data.", LogLevel.Error);
            }
        }

        private void OnDebugKill(string command, string[] args)
        {
            if (args.Length == 0)
            {
                this.Monitor.Log("Usage: debug_kill <name>", LogLevel.Error);
                return;
            }

            string name = args[0];
            if (this.Data.NpcStates.TryGetValue(name, out var npcData))
            {
                npcData.Alive = false;
                npcData.LifeStage = LifeStage.Deceased;
                this.Monitor.Log($"Killed {name}. They should have a funeral pending now.", LogLevel.Alert);
                
                if (!this.Data.PendingFunerals.Contains(name))
                {
                    this.Data.PendingFunerals.Add(name);
                }

                // Hide immediately
                var npc = Game1.getCharacterFromName(name);
                if (npc != null)
                {
                    if (npc.currentLocation != null) npc.currentLocation.characters.Remove(npc);
                    npc.IsInvisible = true;
                    npc.HideShadow = true;
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

            // Hide deceased NPCs
            foreach (var kvp in this.Data.NpcStates)
            {
                if (!kvp.Value.Alive && kvp.Value.LifeStage == Models.LifeStage.Deceased)
                {
                    var npc = Game1.getCharacterFromName(kvp.Key);
                    if (npc != null)
                    {
                        // Effectively remove them from the world
                        if (npc.currentLocation != null)
                        {
                            npc.currentLocation.characters.Remove(npc);
                        }
                        else
                        {
                            // Fallback: Check all locations if current is null/unsynced
                            foreach (var loc in Game1.locations)
                            {
                                if (loc.characters.Contains(npc))
                                {
                                    loc.characters.Remove(npc);
                                    break;
                                }
                            }
                        }
                        // Also try to hide them if removal fails or they respawn (e.g. valid spawn points)
                        npc.IsInvisible = true;
                        npc.HideShadow = true;
                        this.Monitor.Log($"Removed deceased NPC {npc.Name} from the world.", LogLevel.Trace);
                    }
                }
            }

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