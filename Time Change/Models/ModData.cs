using System.Collections.Generic;

namespace Time_Change.Models
{
    public class ModData
    {
        public Dictionary<string, NPCData> NpcStates { get; set; } = new();
        public int PlayerAge { get; set; } = 20; // Default starting age
        public int CurrentYear { get; set; } = 1;
        
        // Constructor
        public ModData() { }
    }
}
