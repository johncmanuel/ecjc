using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextSearchToEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Entries",
                type: "tsvector",
                nullable: true)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "TextContent" });

            migrationBuilder.CreateIndex(
                name: "IX_Entries_SearchVector",
                table: "Entries",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Entries_SearchVector",
                table: "Entries");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Entries");
        }
    }
}
