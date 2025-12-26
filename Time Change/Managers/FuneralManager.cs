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
            // Invalidate the event cache so that if there is a new pending funeral (different from yesterday),
            // the game re-requests the asset and we can generate the new script.
            this.Helper.GameContent.InvalidateCache($"Data/Events/{MapManager.CemeteryLocationName}");
            this.Helper.GameContent.InvalidateCache("Data/mail");

            // Ensure mail is sent for all pending funerals
            if (this.Data != null)
            {
                foreach (var name in this.Data.PendingFunerals)
                {
                    string mailKey = $"SeasonsOfTime_Death_{name}";
                    // Check if mail already received or in mailbox
                    if (!Game1.player.mailReceived.Contains(mailKey) && !Game1.player.mailbox.Contains(mailKey))
                    {
                        Game1.addMailForTomorrow(mailKey);
                        this.Monitor.Log($"Queued death notification letter for {name}", LogLevel.Info);
                    }
                }
            }
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

        

                                                string script = $"moonlightJellies/10 10/farmer 10 15 0 Lewis 10 12 2/pause 1000/speak Lewis \"We are gathered here today to say goodbye to our friend, {deceasedName}.\"/pause 500/message \"The town stands in silence.\"/pause 1000/end";

        

                    

        

                                                events[eventId.ToString()] = script;

        

                                            }

        

                                        }

        

                                        

        

                                        return events;

        

                                    }, AssetLoadPriority.Medium);

        

                                }

        

                    

        

                                // Inject Mail Data

        

                                if (e.Name.IsEquivalentTo("Data/mail"))

        

                                {

        

                                    e.Edit(editor =>

        

                                    {

        

                                        var mail = editor.AsDictionary<string, string>().Data;

        

                                        foreach (var name in this.Data.PendingFunerals)

        

                                        {

        

                                            string mailKey = $"SeasonsOfTime_Death_{name}";

        

                                            if (!mail.ContainsKey(mailKey))

        

                                            {

        

                                                mail[mailKey] = $"Dear @,^^It is with heavy hearts that we announce the passing of {name}.^^A memorial service will be held at the Cemetery.^Please join us to pay your respects.^^   - Mayor Lewis";

        

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
