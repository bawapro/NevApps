using System.ComponentModel.DataAnnotations.Schema;

namespace NevDBClass.Models
{
    public class ExpenseTracker
    {
        public int Id { get; set; }

        public DateOnly Date { get; set; }

        [Column(TypeName = "TEXT COLLATE NOCASE")]
        public string Category { get; set; } = "";

        [Column(TypeName = "TEXT COLLATE NOCASE")]
        public string Type { get; set; } = "";

        [Column(TypeName = "TEXT COLLATE NOCASE")]
        public string Place { get; set; } = "";
        public decimal Price { get; set; }

        [Column(TypeName = "TEXT COLLATE NOCASE")]
        public string Detail { get; set; } = "";

        public int? AutoPay { get; set; }

        public DateOnly? AutoPayDate { get; set; }

        public string? Frequency { get; set; } // D/W/B/M/Q/H/Y — see Constants.AutoPay

        public int? TripFlag { get; set; }

        [Column(TypeName = "TEXT COLLATE NOCASE")]
        public string? TripDestination { get; set; }

        public int? TripId { get; set; }
    }
}
