using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class SVModelcs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sv_character_document_entries_test_profiles_test_profile_i_d",
                table: "sv_character_document_entries");

            migrationBuilder.RenameColumn(
                name: "test_profile_i_d",
                table: "sv_character_document_entries",
                newName: "svprofile_i_d");

            migrationBuilder.RenameIndex(
                name: "IX_sv_character_document_entries_test_profile_i_d",
                table: "sv_character_document_entries",
                newName: "IX_sv_character_document_entries_svprofile_i_d");

            migrationBuilder.AddColumn<DateTime>(
                name: "doc_date_last_edited",
                table: "sv_character_document_entries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddForeignKey(
                name: "FK_sv_character_document_entries_test_profiles_svprofile_i_d",
                table: "sv_character_document_entries",
                column: "svprofile_i_d",
                principalTable: "test_profiles",
                principalColumn: "player_i_d",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sv_character_document_entries_test_profiles_svprofile_i_d",
                table: "sv_character_document_entries");

            migrationBuilder.DropColumn(
                name: "doc_date_last_edited",
                table: "sv_character_document_entries");

            migrationBuilder.RenameColumn(
                name: "svprofile_i_d",
                table: "sv_character_document_entries",
                newName: "test_profile_i_d");

            migrationBuilder.RenameIndex(
                name: "IX_sv_character_document_entries_svprofile_i_d",
                table: "sv_character_document_entries",
                newName: "IX_sv_character_document_entries_test_profile_i_d");

            migrationBuilder.AddForeignKey(
                name: "FK_sv_character_document_entries_test_profiles_test_profile_i_d",
                table: "sv_character_document_entries",
                column: "test_profile_i_d",
                principalTable: "test_profiles",
                principalColumn: "player_i_d",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
