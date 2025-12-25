namespace Time_Change
{
    public class ModConfig
    {
        // The age at which a character stops being a Child and becomes a Teen
        public int ChildMaxAge { get; set; } = 13;

        // The age at which a character stops being a Teen and becomes an Adult
        public int TeenMaxAge { get; set; } = 20;

        // The age at which a character stops being an Adult and becomes an Elder
        public int AdultMaxAge { get; set; } = 65;
    }
}
