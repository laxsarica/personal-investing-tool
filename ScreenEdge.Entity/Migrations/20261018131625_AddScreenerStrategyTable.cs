using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ScreenEdge.Entity.Migrations
{
    /// <inheritdoc />
    public partial class AddScreenerStrategyTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScreenerStrategies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StrategyName = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScreenerStrategies", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ScreenerStrategies",
                columns: new[] { "Id", "Description", "DisplayName", "IsEnabled", "StrategyName", "UpdatedAt" },
                values: new object[,]
                {
                    { 1L, "ZeroLag crossover screener — detects momentum shift using zero-lag EMA signals.", "ZeroLag", true, "NOLAG", new DateTime(2026, 10, 18, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2L, "EMA-50 crossover screener — detects price crossing above EMA-50.", "EMA 50", false, "EMAFIFTY", new DateTime(2026, 10, 18, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3L, "Support/Resistance breakout screener — detects price breaking out above key resistance levels.", "Support/Resistance Breakout", true, "SUPPORTRESISTANCE", new DateTime(2026, 10, 18, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4L, "RSI-Weighted Moving Average screener — detects bullish RSI-WMA crossovers.", "RSI Weighted MA", false, "RSIWMA", new DateTime(2026, 10, 18, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5L, "RSI Triple Timeframe — monthly/weekly uptrend with daily pullback recovery.", "RSI Triple Timeframe", false, "RSITTF", new DateTime(2026, 10, 18, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6L, "RSI oversold zone with bullish candlestick pattern confirmation.", "RSI Full + Candlestick", false, "RSIFULL", new DateTime(2026, 10, 18, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7L, "ATR-based uptrend detection bot — identifies buy signals from trend reversals.", "Uptrend Bot", false, "UPTRENDBOT", new DateTime(2026, 10, 18, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8L, "Long-term wealth creation indicator — identifies stocks in sustained uptrends.", "Wealth Creation", false, "WEALTHCREATION", new DateTime(2026, 10, 18, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9L, "RSI + SMA + Candlestick composite screener — detects buy signals from multi-indicator confluence.", "RSI SMA Candle", true, "RSISMACANDLE", new DateTime(2026, 10, 18, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScreenerStrategies_StrategyName",
                table: "ScreenerStrategies",
                column: "StrategyName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScreenerStrategies");
        }
    }
}
