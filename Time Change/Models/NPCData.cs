using System;

namespace Time_Change.Models
{
    public class NPCData
    {
        public string Id { get; set; } = string.Empty;
        public int BirthYear { get; set; }
        public int Age { get; set; }
        public LifeStage LifeStage { get; set; } = LifeStage.Adult;
        public int Vitality { get; set; } // 0-100
        public bool Alive { get; set; } = true;

        // Components
        public Health Health { get; set; } = new();
        public Psyche Psyche { get; set; } = new();
        public RelationshipData Relationships { get; set; } = new();
        public Occupation Occupation { get; set; } = new();
        public RiskProfile RiskProfile { get; set; } = new();

        public NPCData() { }

        public NPCData(string id, int age)
        {
            Id = id;
            Age = age;
            // Basic initialization logic could go here
            BirthYear = 1 - age; // Approximate based on Y1 start
        }
    }
}
