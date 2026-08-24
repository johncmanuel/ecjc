using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class MovePenaltyToGroupUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "accumulatedPenaltyCents",
                table: "user");

            migrationBuilder.AddColumn<int>(
                name: "AccumulatedPenaltyCents",
                table: "GroupUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccumulatedPenaltyCents",
                table: "GroupUsers");

            migrationBuilder.AddColumn<int>(
                name: "accumulatedPenaltyCents",
                table: "user",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
