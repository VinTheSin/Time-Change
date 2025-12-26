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

                    if (this.Data == null) return; // Allow checking without pending funerals to at least load the empty event file

        

                    // Target the Cemetery events

                    if (e.Name.IsEquivalentTo($"Data/Events/{MapManager.CemeteryLocationName}"))

                    {

                        e.LoadFrom(() =>

                        {

                            var events = new System.Collections.Generic.Dictionary<string, string>();

        

                            if (this.Data.PendingFunerals.Count > 0)

                            {

                                // Grab the first pending funeral

                                string deceasedName = this.Data.PendingFunerals[0];

                                int eventId = GetFuneralEventId(deceasedName);

        

                                // Don't reinject if already seen (double check, though DayEnding handles removal)

                                if (!Game1.player.eventsSeen.Contains(eventId.ToString()))

                                {

                                    this.Monitor.Log($"Injecting funeral event for {deceasedName} (ID: {eventId})", LogLevel.Info);

        

                                    // Basic Event Script

                                    // Preconditions: None (null key prefix?) -> Stardew events usually "ID/Condition"

                                    // If we just use ID, it means "always run if not seen"

                                    

                                    // Script:

                                    // music moonlightJellies

                                    // viewport 10 10

                                    // farmer 10 15 0 (Face Up)

                                    // Lewis 10 12 2 (Face Down)

                                    // speak Lewis "..."

                                    

                                    string script = $"moonlightJellies/10 10/farmer 10 15 0 Lewis 10 12 2/pause 1000/speak Lewis \"We are gathered here today to say goodbye to our friend, {deceasedName}.\"/pause 500/message \"The town stands in silence.\"/pause 1000/end";

        

                                    events[eventId.ToString()] = script;

                                }

                            }

                            

                            return events;

                        }, AssetLoadPriority.Medium);

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
