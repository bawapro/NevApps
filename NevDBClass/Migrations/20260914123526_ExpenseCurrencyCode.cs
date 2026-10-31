using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NevDBClass.Migrations
{
    /// <inheritdoc />
    public partial class ExpenseCurrencyCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExpenseTrackers_Date_Category_Type_Place_Detail_Price",
                table: "ExpenseTrackers");

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "ExpenseTrackers",
                type: "TEXT COLLATE NOCASE",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE "ExpenseTrackers"
                SET "CurrencyCode" = COALESCE(
                    NULLIF((SELECT "SettingValue" FROM "Settings" WHERE "SettingKey" = 'currency' LIMIT 1), ''),
                    ''
                )
                WHERE "CurrencyCode" IS NULL OR TRIM("CurrencyCode") = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseTrackers_Date_Category_Type_Place_Detail_Price_CurrencyCode",
                table: "ExpenseTrackers",
                columns: new[] { "Date", "Category", "Type", "Place", "Detail", "Price", "CurrencyCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExpenseTrackers_Date_Category_Type_Place_Detail_Price_CurrencyCode",
                table: "ExpenseTrackers");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "ExpenseTrackers");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseTrackers_Date_Category_Type_Place_Detail_Price",
                table: "ExpenseTrackers",
                columns: new[] { "Date", "Category", "Type", "Place", "Detail", "Price" },
                unique: true);
        }
    }
}
