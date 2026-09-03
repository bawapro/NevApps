using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NevDBClass.Models;

namespace NevDBClass.Data
{
    public class SqLiteDbContext : DbContext
    {
        public DbSet<ExpenseTracker> ExpenseTrackers { get; set; } = null!;
        public DbSet<MileageTracker> MileageTrackers { get; set; } = null!;
        public DbSet<Setting> Settings { get; set; } = null!;
        public DbSet<Reminder> Reminders { get; set; } = null!;

        // Constructor that accepts DbContextOptions and passes them to the base DbContext constructor.
        public SqLiteDbContext(DbContextOptions<SqLiteDbContext> options) : base(options)
        {
        }

        // This method is used to configure the model (entities) and their relationships.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure DateOnly properties to be stored as TEXT in SQLite
            modelBuilder.Entity<ExpenseTracker>()
                .Property(x => x.Date)
                .HasConversion(
                    v => v.ToString("yyyy-MM-dd"),
                    v => DateOnly.Parse(v)
                )
                .HasColumnType("TEXT");

            modelBuilder.Entity<MileageTracker>()
                .Property(x => x.Date)
                .HasConversion(
                    v => v.ToString("yyyy-MM-dd"),
                    v => DateOnly.Parse(v)
                )
                .HasColumnType("TEXT");

            modelBuilder.Entity<Reminder>()
                .Property(x => x.Date)
                .HasConversion(
                    v => v.ToString("yyyy-MM-dd"),
                    v => DateOnly.Parse(v)
                )
                .HasColumnType("TEXT");

            //Create Unique Indexes to prevent duplicate entries
            modelBuilder.Entity<ExpenseTracker>()
                .HasIndex(m => new { m.Date, m.Category, m.Type, m.Place, m.Detail, m.Price })
                .IsUnique();

            modelBuilder.Entity<MileageTracker>()
                .HasIndex(m => new { m.Date, m.Vehicle, m.OdoStart, m.OdoEnd, m.Start, m.End })
                .IsUnique();

            modelBuilder.Entity<Reminder>()
                .Property(r => r.Type)
                .HasConversion<int>();
            modelBuilder.Entity<Reminder>()
                .HasIndex(m => new { m.Name, m.Type, m.Date })
                .IsUnique();

        }
    }

    /// <summary>
    /// Used by EF Core tools when adding migrations. Not used at runtime.
    /// </summary>
    public class SqLiteDbContextFactory : IDesignTimeDbContextFactory<SqLiteDbContext>
    {
        public SqLiteDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SqLiteDbContext>();

            // Temporary DB path – only used for generating migrations (never in your app)
            var tempPath = Path.Combine(Path.GetTempPath(), "nev_migration_temp.db3");
            optionsBuilder.UseSqlite($"Filename={tempPath}");

            return new SqLiteDbContext(optionsBuilder.Options);
        }
    }
}