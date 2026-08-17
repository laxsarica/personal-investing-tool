using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScreenEdge.Entity.Migrations
{
    /// <inheritdoc />
    public partial class AddFinnhubFundamentals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MarketCap",
                table: "DistinctStocks",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StockFundamentals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DistinctStockId = table.Column<long>(type: "bigint", nullable: false),
                    PeRatio = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PbRatio = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DividendYield = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FiftyTwoWeekHigh = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FiftyTwoWeekLow = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Website = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockFundamentals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockFundamentals_DistinctStocks_DistinctStockId",
                        column: x => x.DistinctStockId,
                        principalTable: "DistinctStocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockFundamentals_DistinctStockId",
                table: "StockFundamentals",
                column: "DistinctStockId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockFundamentals");

            migrationBuilder.DropColumn(
                name: "MarketCap",
                table: "DistinctStocks");
        }
    }
}
