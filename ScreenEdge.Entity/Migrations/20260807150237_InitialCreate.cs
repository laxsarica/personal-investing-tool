using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScreenEdge.Entity.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DistinctStocks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Exchange = table.Column<string>(type: "nvarchar(max)", nullable: false)
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
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ScreenerName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TimeFrame = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecognizeDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Rsi = table.Column<double>(type: "float", nullable: false),
                    RsiWeekly = table.Column<double>(type: "float", nullable: false),
                    RsiMonthly = table.Column<double>(type: "float", nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: false),
                    RecognizedPrice = table.Column<double>(type: "float", nullable: false)
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
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Open = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    High = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Low = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Close = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Volume = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TickerHistories", x => x.Id);
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
                name: "DistinctStocks");

            migrationBuilder.DropTable(
                name: "Screeners");

            migrationBuilder.DropTable(
                name: "TickerHistories");
        }
    }
}
