using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DartSmartNet.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureGameOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "option_cricket_mode",
                table: "game_sessions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "option_in_mode",
                table: "game_sessions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "option_out_mode",
                table: "game_sessions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "game_event_logs",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    event_data = table.Column<string>(type: "jsonb", nullable: false),
                    player_username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_event_logs", x => x.event_id);
                    table.ForeignKey(
                        name: "FK_game_event_logs_game_sessions_game_id",
                        column: x => x.game_id,
                        principalTable: "game_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "game_profiles",
                columns: table => new
                {
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    game_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    starting_score = table.Column<int>(type: "integer", nullable: false),
                    out_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    in_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    extension_settings = table.Column<string>(type: "jsonb", nullable: true),
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_profiles", x => x.profile_id);
                    table.ForeignKey(
                        name: "FK_game_profiles_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_game_event_logs_game_id",
                table: "game_event_logs",
                column: "game_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_event_logs_game_timestamp",
                table: "game_event_logs",
                columns: new[] { "game_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_game_event_logs_timestamp",
                table: "game_event_logs",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_game_profiles_is_public",
                table: "game_profiles",
                column: "is_public");

            migrationBuilder.CreateIndex(
                name: "ix_game_profiles_owner_id",
                table: "game_profiles",
                column: "owner_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "game_event_logs");

            migrationBuilder.DropTable(
                name: "game_profiles");

            migrationBuilder.DropColumn(
                name: "option_cricket_mode",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "option_in_mode",
                table: "game_sessions");

            migrationBuilder.DropColumn(
                name: "option_out_mode",
                table: "game_sessions");
        }
    }
}
