using System;
using System.Collections.Generic;
using StardewModdingAPI;
using Time_Change.Models;

namespace Time_Change.Managers
{
    public class LifecycleManager
    {
        private readonly IMonitor Monitor;
        private readonly ModConfig Config;
        private readonly PsychologyManager Psychology;

        public LifecycleManager(IMonitor monitor, ModConfig config)
        {
            this.Monitor = monitor;
            this.Config = config;
            this.Psychology = new PsychologyManager(monitor);
        }

        public void ProcessYearlyUpdate(ModData data)
        {
            this.Monitor.Log($"Processing yearly update for Year {data.CurrentYear + 1}...", LogLevel.Info);

            data.PlayerAge++;

            foreach (var kvp in data.NpcStates)
            {
                var npcData = kvp.Value;
                if (!npcData.Alive) continue;

                npcData.Age++;
                UpdateLifeStage(npcData);
                CheckForDeath(npcData, data);
                
                if (npcData.Alive) // Only process psyche if they survived the death check
                {
                    this.Psychology.ProcessYearlyPsychology(npcData);
                }
            }
        }

        private void CheckForDeath(NPCData npc, ModData data)
        {
            // Simple probabilistic death model for old age
            double deathChance = 0.0;
            
            // Adjust death thresholds based on Elder age start? 
            // For now keeping absolute age checks, but could be relative to AdultMaxAge.
            if (npc.Age >= 100) deathChance = 0.50; // 50% chance each year after 100
            else if (npc.Age >= 90) deathChance = 0.20;
            else if (npc.Age >= 80) deathChance = 0.05;
            else if (npc.Age >= 70) deathChance = 0.01;

            if (deathChance > 0)
            {
                Random rnd = new Random();
                if (rnd.NextDouble() < deathChance)
                {
                    KillNPC(npc, "Old Age", data);
                }
            }
        }

        private void KillNPC(NPCData npc, string cause, ModData data)
        {
            npc.Alive = false;
            npc.LifeStage = LifeStage.Deceased;
            this.Monitor.Log($"NPC DEATH: {npc.Id} has died of {cause} at age {npc.Age}.", LogLevel.Alert);
            
            // Queue funeral
            if (!data.PendingFunerals.Contains(npc.Id))
            {
                data.PendingFunerals.Add(npc.Id);
            }
        }

        private void UpdateLifeStage(NPCData npc)
        {
            var oldStage = npc.LifeStage;
            
            if (npc.Age < this.Config.ChildMaxAge) npc.LifeStage = LifeStage.Child;
            else if (npc.Age < this.Config.TeenMaxAge) npc.LifeStage = LifeStage.Teen;
            else if (npc.Age < this.Config.AdultMaxAge) npc.LifeStage = LifeStage.Adult;
            else npc.LifeStage = LifeStage.Elder;

            if (oldStage != npc.LifeStage)
            {
                this.Monitor.Log($"{npc.Id} transitioned from {oldStage} to {npc.LifeStage}", LogLevel.Info);
            }
        }
    }
}
