using System;
using StardewModdingAPI;
using Time_Change.Models;

namespace Time_Change.Managers
{
    public class PsychologyManager
    {
        private readonly IMonitor Monitor;
        private readonly Random Random;

        public PsychologyManager(IMonitor monitor)
        {
            this.Monitor = monitor;
            this.Random = new Random();
        }

        public void ProcessYearlyPsychology(NPCData npc)
        {
            // 1. Calculate Stress Delta
            int stressChange = CalculateStressFactors(npc);
            
            // 2. Apply Coping Mechanisms
            stressChange = ApplyCoping(npc, stressChange);

            // 3. Update Stress
            npc.Psyche.Stress = Math.Clamp(npc.Psyche.Stress + stressChange, 0, 100);

            // 4. Check for Stage Transition
            UpdateMentalStage(npc);
        }

        private int CalculateStressFactors(NPCData npc)
        {
            int delta = 0;

            // Base stress from life stage
            if (npc.LifeStage == LifeStage.Adult) delta += 5;
            if (npc.LifeStage == LifeStage.Elder) delta += 10; // Health worries, etc.

            // Health factors
            delta += npc.Health.Chronic.Count * 10;
            delta += npc.Health.Conditions.Count * 5;

            // Risk Profile
            if (npc.RiskProfile.Isolation == RiskLevel.High) delta += 15;
            if (npc.RiskProfile.Isolation == RiskLevel.Extreme) delta += 25;

            // Random variance (representing unseen daily struggles)
            delta += this.Random.Next(-10, 20);

            return delta;
        }

        private int ApplyCoping(NPCData npc, int stressChange)
        {
            // Resilience buffers stress increases
            if (stressChange > 0)
            {
                // Higher resilience reduces stress gain
                // Resilience 100 -> 50% reduction
                double reductionFactor = npc.Psyche.Resilience / 200.0; // Max 0.5 reduction
                stressChange = (int)(stressChange * (1.0 - reductionFactor));
            }
            
            // Coping styles
            switch (npc.Psyche.CopingStyle)
            {
                case CopingStyle.Avoidance:
                    // Ignores small stress, but crashes hard on big stress?
                    // For now simple implementation
                    break;
                case CopingStyle.SelfDestruction:
                    // Increases stress but might have other side effects (health)
                    stressChange += 5; 
                    break;
                case CopingStyle.SupportSeeking:
                    // Reduces stress if relationships are good (not impl yet)
                    stressChange -= 5;
                    break;
            }

            return stressChange;
        }

        private void UpdateMentalStage(NPCData npc)
        {
            var oldStage = npc.Psyche.Stage;
            int stress = npc.Psyche.Stress;

            // Thresholds
            if (stress < 30) npc.Psyche.Stage = MentalStage.Stable;
            else if (stress < 50) npc.Psyche.Stage = MentalStage.Withdrawal;
            else if (stress < 70) npc.Psyche.Stage = MentalStage.Dysfunction;
            else if (stress < 90) npc.Psyche.Stage = MentalStage.Instability;
            else npc.Psyche.Stage = MentalStage.Crisis;

            if (oldStage != npc.Psyche.Stage)
            {
                this.Monitor.Log($"PSYCHE CHANGE: {npc.Id} moved from {oldStage} to {npc.Psyche.Stage} (Stress: {stress})", LogLevel.Warn);
            }
        }
    }
}
