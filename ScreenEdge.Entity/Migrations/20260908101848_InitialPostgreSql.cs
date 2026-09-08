using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ScreenEdge.Entity.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DistinctStocks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Symbol = table.Column<string>(type: "text", nullable: false),
                    CompanyName = table.Column<string>(type: "text", nullable: false),
                    Exchange = table.Column<string>(type: "text", nullable: false),
                    TotalTradingDays = table.Column<int>(type: "integer", nullable: false),
                    MarketCapCategory = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistinctStocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Screeners",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Symbol = table.Column<string>(type: "text", nullable: false),
                    ScreenerName = table.Column<string>(type: "text", nullable: false),
                    TimeFrame = table.Column<string>(type: "text", nullable: false),
                    RecognizeDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Rsi = table.Column<double>(type: "double precision", nullable: false),
                    RsiWeekly = table.Column<double>(type: "double precision", nullable: false),
                    RsiMonthly = table.Column<double>(type: "double precision", nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: false),
                    RecognizedPrice = table.Column<double>(type: "double precision", nullable: false),
                    Pattern = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Screeners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TickerHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Symbol = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Open = table.Column<decimal>(type: "numeric", nullable: false),
                    High = table.Column<decimal>(type: "numeric", nullable: false),
                    Low = table.Column<decimal>(type: "numeric", nullable: false),
                    Close = table.Column<decimal>(type: "numeric", nullable: false),
                    Volume = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TickerHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockFundamentals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DistinctStockId = table.Column<long>(type: "bigint", nullable: false),
                    PeRatio = table.Column<decimal>(type: "numeric", nullable: true),
                    PbRatio = table.Column<decimal>(type: "numeric", nullable: true),
                    DividendYield = table.Column<decimal>(type: "numeric", nullable: true),
                    FiftyTwoWeekHigh = table.Column<decimal>(type: "numeric", nullable: true),
                    FiftyTwoWeekLow = table.Column<decimal>(type: "numeric", nullable: true),
                    Industry = table.Column<string>(type: "text", nullable: false),
                    Website = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
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
                name: "IX_DistinctStocks_Symbol",
                table: "DistinctStocks",
                column: "Symbol",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Screeners_RecognizeDate",
                table: "Screeners",
                column: "RecognizeDate");

            migrationBuilder.CreateIndex(
                name: "IX_Screeners_ScreenerName",
                table: "Screeners",
                column: "ScreenerName");

            migrationBuilder.CreateIndex(
                name: "IX_Screeners_Symbol",
                table: "Screeners",
                column: "Symbol");

            migrationBuilder.CreateIndex(
                name: "IX_Screeners_Symbol_ScreenerName_TimeFrame_RecognizeDate",
                table: "Screeners",
                columns: new[] { "Symbol", "ScreenerName", "TimeFrame", "RecognizeDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockFundamentals_DistinctStockId",
                table: "StockFundamentals",
                column: "DistinctStockId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TickerHistories_Symbol",
                table: "TickerHistories",
                column: "Symbol");

            migrationBuilder.CreateIndex(
                name: "IX_TickerHistories_Symbol_Date",
                table: "TickerHistories",
                columns: new[] { "Symbol", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Screeners");

            migrationBuilder.DropTable(
                name: "StockFundamentals");

            migrationBuilder.DropTable(
                name: "TickerHistories");

            migrationBuilder.DropTable(
                name: "DistinctStocks");
        }
    }
}
