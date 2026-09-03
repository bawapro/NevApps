namespace NevApps.Models
{
    public class Settings
    {
        public class Country
        {
            public string Code { get; set; } = "";         // Alpha-2 e.g. "CA"
            public string Name { get; set; } = "";         // "Canada"
            public string Display => $"{Name} ({Code})";   // In autocomplete
            public string CurrencyCode { get; set; } = "";
            public string CurrencySymbol { get; set; } = "";
            public string Continent { get; set; } = "";
        }
    }
}
