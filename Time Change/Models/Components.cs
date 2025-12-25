using System.Collections.Generic;

namespace Time_Change.Models
{
    public class Health
    {
        public List<string> Conditions { get; set; } = new();
        public List<string> Injuries { get; set; } = new();
        public List<string> Chronic { get; set; } = new();
        
        // Vitality could be here or on the main NPC, keeping it on main for now as per JSON example
    }

    public class Psyche
    {
        public int Stress { get; set; } // 0-100
        public int Resilience { get; set; } // 0-100
        public CopingStyle CopingStyle { get; set; } = CopingStyle.Avoidance;
        public MentalStage Stage { get; set; } = MentalStage.Stable;
    }

    public class RelationshipData
    {
        public List<string> Likes { get; set; } = new();
        public List<string> Loves { get; set; } = new();
        public List<string> Hates { get; set; } = new();
        public string? Partner { get; set; }
        public List<string> Children { get; set; } = new();
    }

    public class Occupation
    {
        public string Current { get; set; } = "Unemployed";
        public bool Inherited { get; set; }
    }

    public class RiskProfile
    {
        public bool SubstanceUse { get; set; }
        public RiskLevel Impulsivity { get; set; }
        public RiskLevel Isolation { get; set; }
    }
}
