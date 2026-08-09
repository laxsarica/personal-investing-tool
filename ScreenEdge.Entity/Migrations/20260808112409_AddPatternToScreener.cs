using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScreenEdge.Entity.Migrations
{
    /// <inheritdoc />
    public partial class AddPatternToScreener : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Pattern",
                table: "Screeners",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Pattern",
                table: "Screeners");
        }
    }
}
