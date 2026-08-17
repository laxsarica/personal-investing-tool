using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScreenEdge.Entity.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMarketCap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MarketCap",
                table: "DistinctStocks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MarketCap",
                table: "DistinctStocks",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
