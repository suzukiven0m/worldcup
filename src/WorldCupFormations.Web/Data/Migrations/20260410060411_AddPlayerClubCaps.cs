using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorldCupFormations.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerClubCaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Caps",
                table: "Players",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Club",
                table: "Players",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Caps",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "Club",
                table: "Players");
        }
    }
}
