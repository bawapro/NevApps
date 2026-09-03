using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevDBClass.Migrations
{
    /// <inheritdoc />
    public partial class First : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpenseTrackers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT COLLATE NOCASE", nullable: false),
                    Type = table.Column<string>(type: "TEXT COLLATE NOCASE", nullable: false),
                    Place = table.Column<string>(type: "TEXT COLLATE NOCASE", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    Detail = table.Column<string>(type: "TEXT COLLATE NOCASE", nullable: false),
                    AutoPay = table.Column<int>(type: "INTEGER", nullable: true),
                    AutoPayDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Frequency = table.Column<string>(type: "TEXT", nullable: true),
                    TripFlag = table.Column<int>(type: "INTEGER", nullable: true),
                    TripDestination = table.Column<string>(type: "TEXT COLLATE NOCASE", nullable: true),
                    TripId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseTrackers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MileageTrackers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<string>(type: "TEXT", nullable: false),
                    Vehicle = table.Column<string>(type: "TEXT COLLATE NOCASE", nullable: false),
                    Start = table.Column<string>(type: "TEXT COLLATE NOCASE", nullable: false),
                    End = table.Column<string>(type: "TEXT COLLATE NOCASE", nullable: false),
                    OdoStart = table.Column<int>(type: "INTEGER", nullable: false),
                    OdoEnd = table.Column<int>(type: "INTEGER", nullable: false),
                    Distance = table.Column<int>(type: "INTEGER", nullable: false),
                    Mileage = table.Column<int>(type: "INTEGER", nullable: false),
                    Detail = table.Column<string>(type: "TEXT COLLATE NOCASE", nullable: true),
                    GasStation = table.Column<string>(type: "TEXT COLLATE NOCASE", nullable: true),
                    FuelType = table.Column<string>(type: "TEXT", nullable: true),
                    FuelPrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    FuelFilled = table.Column<decimal>(type: "TEXT", nullable: true),
                    FuelRate = table.Column<decimal>(type: "TEXT", nullable: true),
                    FuelMileage = table.Column<decimal>(type: "TEXT", nullable: true),
                    TripFlag = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MileageTrackers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SettingKey = table.Column<string>(type: "TEXT", nullable: false),
                    SettingValue = table.Column<string>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseTrackers_Date_Category_Type_Place_Detail_Price",
                table: "ExpenseTrackers",
                columns: new[] { "Date", "Category", "Type", "Place", "Detail", "Price" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MileageTrackers_Date_Vehicle_OdoStart_OdoEnd_Start_End",
                table: "MileageTrackers",
                columns: new[] { "Date", "Vehicle", "OdoStart", "OdoEnd", "Start", "End" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseTrackers");

            migrationBuilder.DropTable(
                name: "MileageTrackers");

            migrationBuilder.DropTable(
                name: "Settings");
        }
    }
}
