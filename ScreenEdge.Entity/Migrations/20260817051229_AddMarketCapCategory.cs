using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScreenEdge.Entity.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketCapCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MarketCapCategory",
                table: "DistinctStocks",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MarketCapCategory",
                table: "DistinctStocks");
        }
    }
}
