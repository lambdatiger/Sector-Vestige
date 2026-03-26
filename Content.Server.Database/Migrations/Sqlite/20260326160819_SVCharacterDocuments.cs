using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class SVCharacterDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sv_profiles",
                columns: table => new
                {
                    profile_id = table.Column<int>(nullable: false),
                    player_name = table.Column<string>(nullable: false, defaultValue: ""),
                    character_name = table.Column<string>(nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sv_profiles", x => x.profile_id);
                    table.ForeignKey(
                        name: "FK_sv_profiles_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sv_character_document_entries",
                columns: table => new
                {
                    doc_i_d = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    doc_type = table.Column<int>(nullable: false, defaultValue: 0),
                    doc_date_last_edited = table.Column<DateTime>(nullable: false),
                    doc_title = table.Column<string>(nullable: false),
                    doc_author = table.Column<string>(nullable: false),
                    doc_content = table.Column<string>(nullable: false),
                    doc_stamps = table.Column<string>(nullable: false),
                    profile_id = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sv_character_document_entries", x => x.doc_i_d);
                    table.ForeignKey(
                        name: "FK_sv_character_document_entries_sv_profiles_profile_id",
                        column: x => x.profile_id,
                        principalTable: "sv_profiles",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sv_character_document_entries_doc_i_d",
                table: "sv_character_document_entries",
                column: "doc_i_d");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "sv_character_document_entries");
            migrationBuilder.DropTable(name: "sv_profiles");
        }
    }
}
