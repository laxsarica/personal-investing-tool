using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScreenEdge.Entity.Migrations
{
    /// <inheritdoc />
    public partial class AddScreenerUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TimeFrame",
                table: "Screeners",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Screeners_Symbol_ScreenerName_TimeFrame_RecognizeDate",
                table: "Screeners",
                columns: new[] { "Symbol", "ScreenerName", "TimeFrame", "RecognizeDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Screeners_Symbol_ScreenerName_TimeFrame_RecognizeDate",
                table: "Screeners");

            migrationBuilder.AlterColumn<string>(
                name: "TimeFrame",
                table: "Screeners",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
