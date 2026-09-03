using System.ComponentModel.DataAnnotations.Schema;

namespace NevDBClass.Models
{
    public class MileageTracker
    {
        public int Id { get; set; }

        public DateOnly Date { get; set; }

        [Column(TypeName = "TEXT COLLATE NOCASE")]
        public string Vehicle { get; set; } = "";

        [Column(TypeName = "TEXT COLLATE NOCASE")]
        public string Start { get; set; } = "";

        [Column(TypeName = "TEXT COLLATE NOCASE")]
        public string End { get; set; } = "";

        public int OdoStart { get; set; }

        public int OdoEnd { get; set; }

        public int Distance { get; set; }

        public int Mileage { get; set; }

        [Column(TypeName = "TEXT COLLATE NOCASE")]
        public string? Detail { get; set; }

        [Column(TypeName = "TEXT COLLATE NOCASE")]
        public string? GasStation { get; set; }

        public string? FuelType { get; set; }

        public decimal? FuelPrice { get; set; }

        public decimal? FuelFilled { get; set; }

        public decimal? FuelRate { get; set; }

        public decimal? FuelMileage { get; set; }

        public int? TripFlag { get; set; }

    }
}
