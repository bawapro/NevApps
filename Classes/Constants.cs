namespace NevApps.Classes
{
    public static class Constants
    {
        public const int MaxBackups = 5;
        public const string DbName = "NevDB.db3";
        public const string BackupFolderName = "backups";

        public const string OnLoadErrorMessage =
            "Error while loading the contents. If this persists, try again later.";

        public static readonly string[] EvIndicators = new[]
        {
            "elec", // general EV hint ("electric", "ev")

            // Ford
            "mach-e",
            "mustang mach-e",
            "light",

            // Tesla
            "tesla",
            "cybertruck",

            // Nissan
            "leaf",
            "ariya",

            // Chevrolet
            "bolt",

            // Volkswagen ID series
            "id",

            // Hyundai
            "ioniq",

            // Kia
            "ev",

            // BMW i-series
            "i3", "i4", "i5",
            "i7", "ix", "ix3",

            // Mercedes EQ series
            "eq",

            // Audi
            "e-tron",

            // Porsche
            "taycan",

            // Volvo / Polestar
            "recharge",
            "polestar",

            // Lucid
            "lucid",

            // Rivian
            "r1t", "r1s",
            "r2", "r3",

            // Jaguar
            "i-pace",

            // Mazda
            "mx-30",

            // Subaru
            "solterra",

            // Toyota
            "bz4x",

            // Honda
            "prologue",
            "honda e",
            "e:ny1",

            // Other EV brands
            "fisker",
            "vinfast",
            "atto",
            "byd",
            "smart",

            // Zero Motorcycles (USA)
            "zero s", "zero sr",
            "zero ds", "zero sr/f",
            "zero sr/s",

            // LiveWire (Harley-Davidson EV brand)
            "livewire",
            "livewire one",
            "s2 del mar",

            // Energica (Italy)
            "energica",
            "ego", "eva ribelle", "esseesse9",

            // SONDORS Metacycle
            "metacycle",

            // BMW Electric Motorcycle
            "ce 04", "ce 02",

            // Honda Electric Bikes
            "em1 e:",

            // Yamaha Electric
            "e01", "ne01", "neox",

            // KTM Electric
            "freeride e",

            // Ducati Electric (V21L MotoE)
            "v21l"
        };

        public static class AutoPay
        {
            public const string Daily = "D";
            public const string Weekly = "W";
            public const string BiWeekly = "B";
            public const string Monthly = "M";
            public const string Quarterly = "Q";
            public const string HalfYearly = "H";
            public const string Yearly = "Y";

            public static readonly Dictionary<string, string> Frequencies = new()
            {
                { Daily, "Daily" },
                { Weekly, "Weekly" },
                { BiWeekly, "Bi-weekly" },
                { Monthly, "Monthly" },
                { Quarterly, "Quarterly" },
                { HalfYearly, "Half-yearly" },
                { Yearly, "Yearly" }
            };
        }

        public static class Conversions
        {
            public const decimal KmToMiles = 0.621371m;
            public const decimal MilesToKm = 1 / KmToMiles;
            public const decimal LitresToUsGallons = 0.264172m;
            public const decimal UsGallonsToLitres = 1 / LitresToUsGallons;
        }

        public static class Categories
        {
            public const string Bank = "Bank";
            public const string Car = "Car";
            public const string Food = "Food";
            public const string Medical = "Medical";
            public const string Payment = "Payment";
            public const string Shopping = "Shopping";
            public const string Travel = "Travel";

            public static readonly List<string> AllCategories = new()
            {
                Bank, Car, Food, Medical, Payment, Shopping, Travel
            };
        }
    }
}