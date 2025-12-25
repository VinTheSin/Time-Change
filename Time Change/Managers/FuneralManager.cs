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
        }

        public void SetData(ModData data)
        {
            this.Data = data;
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (this.Data == null || this.Data.PendingFunerals.Count == 0) return;

            // Target the Town events
            if (e.Name.IsEquivalentTo("Data/Events/Town"))
            {
                e.Edit(editor =>
                {
                    var events = editor.AsDictionary<string, string>().Data;
                    
                    // Grab the first pending funeral
                    string deceasedName = this.Data.PendingFunerals[0];
                    int eventId = GetFuneralEventId(deceasedName);

                    // Don't reinject if already seen (double check, though DayEnding handles removal)
                    if (Game1.player.eventsSeen.Contains(eventId.ToString()))
                    {
                        return;
                    }

                    this.Monitor.Log($"Injecting funeral event for {deceasedName} (ID: {eventId})", LogLevel.Info);

                    // Basic Event Script
                    // Preconditions: None (-1) or generic
                    // Music: moonlightJellies (sad/slow)
                    // Viewport: 18 90 (Graveyard approx coords in standard Town map)
                    // Attendees: Lewis at head, others gathered
                    
                    string script = $"moonlightJellies/18 90/farmer 18 94 0 Lewis 18 88 2/pause 1000/speak Lewis \"We are gathered here today to say goodbye to our friend, {deceasedName}.\"/pause 500/message \"The town stands in silence.\"/pause 1000/end";

                    // Key: "Condition/EventID" -> Stardew uses just "EventID" key usually, condition inside?
                    // No, Data/Events key is "ConditionID/Command". 
                    // Actually, key is "Condition" if implicit ID, or "ID Condition" usually?
                    // Standard format: "ID/Condition": "Script"
                    // Example: "60367/f Lewis 2500": "..."
                    
                    // We will use a simple key: "EventID"
                    events[eventId.ToString()] = script;
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
            // Simple deterministic hash
            int hash = Math.Abs(npcName.GetHashCode()) % 100000;
            return EventIdBase + hash;
        }
    }
}
