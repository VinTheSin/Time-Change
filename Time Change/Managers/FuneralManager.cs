using System;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Time_Change.Models;

namespace Time_Change.Managers
{
    public class FuneralManager
    {
        private readonly IModHelper Helper;
        private readonly IMonitor Monitor;
        private ModData? Data;

        // Base ID for funeral events to avoid collisions
        private const int EventIdBase = 7000000;

        public FuneralManager(IModHelper helper, IMonitor monitor)
        {
            this.Helper = helper;
            this.Monitor = monitor;

            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.GameLoop.DayEnding += OnDayEnding;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
        }

        public void SetData(ModData data)
        {
            this.Data = data;
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            // Invalidate the event cache
            this.Helper.GameContent.InvalidateCache($"Data/Events/{MapManager.CemeteryLocationName}");
            this.Helper.GameContent.InvalidateCache("Data/mail");
            
            // Force load to trigger injection logging check
            // We just ask for it, which triggers OnAssetRequested if invalidated
            // This ensures our logs appear if it works.
            this.Helper.GameContent.Load<System.Collections.Generic.Dictionary<string, string>>("Data/mail");

            // Ensure mail is sent for all pending funerals
            if (this.Data != null)

            foreach (var kvp in this.Data.NpcStates)
            {
                var npc = kvp.Value;
                if (!npc.Alive && npc.LifeStage == LifeStage.Deceased && npc.DeathDateTotal >= 0)
                {
                    int daysSinceDeath = Game1.Date.TotalDays - npc.DeathDateTotal;

                    // Day 1: Send Mail from Priest
                    if (daysSinceDeath >= 1) // Send if missed too?
                    {
                        string mailKey = $"SeasonsOfTime_Death_{npc.Id}";
                        // If not received and not in mailbox
                        if (!Game1.player.mailReceived.Contains(mailKey) && !Game1.player.mailbox.Contains(mailKey))
                        {
                            Game1.addMailForTomorrow(mailKey);
                            this.Monitor.Log($"Queued priest letter for {npc.Id} (Died {daysSinceDeath} days ago)", LogLevel.Info);
                        }
                    }

                    // Day 7: Funeral Day
                    if (daysSinceDeath == 7)
                    {
                        this.Monitor.Log($"Today is the funeral for {npc.Id}.", LogLevel.Info);
                        // We don't need to do anything here except ensure the event asset is ready, 
                        // which invalidation handled.
                    }
                }
            }
        }

                        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)

                        {

                            if (this.Data == null) return;

                

                                        // Target the Cemetery events

                

                                        if (e.Name.IsEquivalentTo($"Data/Events/{MapManager.CemeteryLocationName}"))

                

                                        {

                

                                            e.LoadFrom(() =>

                

                                            {

                

                                                var events = new System.Collections.Generic.Dictionary<string, string>();

                

                                                int currentTotalDays = Game1.Date.TotalDays;

                

                            

                

                                                foreach (var kvp in this.Data.NpcStates)

                

                                                {

                

                                                    var npc = kvp.Value;

                

                                                    if (!npc.Alive && npc.LifeStage == LifeStage.Deceased && npc.DeathDateTotal >= 0)

                

                                                    {

                

                                                        int targetDate = npc.DeathDateTotal + 7;

                

                                                        this.Monitor.Log($"Checking funeral for {npc.Id}: Died Day {npc.DeathDateTotal}, Today {currentTotalDays}, Target {targetDate}", LogLevel.Trace);

                

                            

                

                                                        // Only generate event if TODAY is the funeral day (Death + 7)

                

                                                        if (currentTotalDays == targetDate)

                

                                                        {

                

                                                            int eventId = GetFuneralEventId(npc.Id);

                

                                                            if (!Game1.player.eventsSeen.Contains(eventId.ToString()))

                

                                                            {

                

                                                                this.Monitor.Log($"Injecting funeral event for {npc.Id} (Today is funeral day)", LogLevel.Info);

                

                                                                string script = $"moonlightJellies/10 10/farmer 10 15 0 Lewis 10 12 2/pause 1000/speak Lewis \"We are gathered here today to say goodbye to our friend, {npc.Id}.\"/pause 500/message \"The town stands in silence.\"/pause 1000/end";

                

                                                                events[eventId.ToString()] = script;

                

                                                                break; 

                

                                                            }

                

                                                        }

                

                                                    }

                

                                                }

                

                                                return events;

                

                                            }, AssetLoadPriority.Medium);

                

                                        }

                

                                                                // Inject Mail Data

                

                                                                if (e.Name.IsEquivalentTo("Data/mail"))

                

                                                                {

                

                                                                    this.Monitor.Log("Game requested Data/mail - Attempting injection...", LogLevel.Trace);

                

                                                                    e.Edit(editor =>

                

                                                                    {

                

                                                                        var mail = editor.AsDictionary<string, string>().Data;

                

                                                                        

                

                                                                        // Debug injection

                

                                                                        mail["SeasonsOfTime_Test"] = "This is a test letter from the mod.^^If you see this, injection works.";

                

                                                    

                

                                                                        foreach (var kvp in this.Data.NpcStates)

                

                                                                        {

                

                                                                            var npc = kvp.Value;

                

                                                                            // Inject mail for ANY dead NPC, just in case

                

                                                                            if (!npc.Alive && npc.LifeStage == LifeStage.Deceased)

                

                                                                            {

                

                                                                                string mailKey = $"SeasonsOfTime_Death_{npc.Id}";

                

                                                                                

                

                                                                                // Calculate Funeral Date (default to Day 7 if missing date)

                

                                                                                int deathDate = npc.DeathDateTotal >= 0 ? npc.DeathDateTotal : 1;

                

                                                                                int funeralTotal = deathDate + 7;

                

                                                                                

                

                                                                                int year = 1 + (funeralTotal / (28 * 4));

                

                                                                                int seasonIndex = (funeralTotal % (28 * 4)) / 28;

                

                                                                                int day = 1 + (funeralTotal % 28);

                

                                                                                string season = Utility.getSeasonNameFromNumber(seasonIndex);

                

                                                                                

                

                                                                                string text = $"Dear @,^^It is with heavy hearts that we announce the passing of {npc.Id}.^They passed away because of {npc.CauseOfDeath}.^^A memorial service will be held at the Cemetery on {season} {day}, Year {year}.^Please join us to pay your respects.^^   - The Priest";

                

                                                                                

                

                                                                                mail[mailKey] = text;

                

                                                                                this.Monitor.Log($"[Mail Injection] Key: {mailKey} | Target: {npc.Id}", LogLevel.Trace);

                

                                                                            }

                

                                                                        }

                

                                                                    });

                

                                                                }

                

                                    }

        private void OnDayEnding(object? sender, DayEndingEventArgs e)
        {
            if (this.Data == null) return;

            // Clean up seen funerals
            for (int i = this.Data.PendingFunerals.Count - 1; i >= 0; i--)
            {
                string name = this.Data.PendingFunerals[i];
                int id = GetFuneralEventId(name);

                if (Game1.player.eventsSeen.Contains(id.ToString()))
                {
                    this.Monitor.Log($"Funeral for {name} has been seen. Removing from queue.", LogLevel.Info);
                    this.Data.PendingFunerals.RemoveAt(i);
                }
            }
        }

        private int GetFuneralEventId(string npcName)
        {
            // Deterministic hash for string to ensure ID is stable across sessions
            // (GetHashCode is randomized in modern .NET)
            unchecked
            {
                int hash = 23;
                foreach (char c in npcName)
                {
                    hash = hash * 31 + c;
                }
                return EventIdBase + (Math.Abs(hash) % 100000);
            }
        }
    }
}
