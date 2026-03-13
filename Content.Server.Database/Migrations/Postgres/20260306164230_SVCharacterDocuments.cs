using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class SVCharacterDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "test_profiles",
                columns: table => new
                {
                    player_i_d = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    profile_id = table.Column<int>(type: "integer", nullable: false),
                    character_doc = table.Column<JsonDocument>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_profiles", x => x.player_i_d);
                    table.ForeignKey(
                        name: "FK_test_profiles_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sv_character_document_entries",
                columns: table => new
                {
                    doc_i_d = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    doc_title = table.Column<string>(type: "text", nullable: false),
                    doc_author = table.Column<string>(type: "text", nullable: false),
                    doc_content = table.Column<string>(type: "text", nullable: false),
                    doc_stamps = table.Column<string>(type: "text", nullable: false),
                    test_profile_i_d = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sv_character_document_entries", x => x.doc_i_d);
                    table.ForeignKey(
                        name: "FK_sv_character_document_entries_test_profiles_test_profile_i_d",
                        column: x => x.test_profile_i_d,
                        principalTable: "test_profiles",
                        principalColumn: "player_i_d",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sv_character_document_entries_doc_i_d",
                table: "sv_character_document_entries",
                column: "doc_i_d");

            migrationBuilder.CreateIndex(
                name: "IX_sv_character_document_entries_test_profile_i_d",
                table: "sv_character_document_entries",
                column: "test_profile_i_d");

            migrationBuilder.CreateIndex(
                name: "IX_test_profiles_profile_id",
                table: "test_profiles",
                column: "profile_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sv_character_document_entries");

            migrationBuilder.DropTable(
                name: "test_profiles");
        }
    }
}
