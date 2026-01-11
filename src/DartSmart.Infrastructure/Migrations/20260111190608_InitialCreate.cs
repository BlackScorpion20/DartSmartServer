using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DartSmart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "games",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_type = table.Column<string>(type: "text", nullable: false),
                    start_score = table.Column<int>(type: "integer", nullable: false),
                    in_mode = table.Column<string>(type: "text", nullable: false),
                    out_mode = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    winner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    current_player_index = table.Column<int>(type: "integer", nullable: false),
                    current_round = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_games", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "players",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total_games = table.Column<int>(type: "integer", nullable: false),
                    wins = table.Column<int>(type: "integer", nullable: false),
                    best_3_dart_score = table.Column<int>(type: "integer", nullable: false),
                    count_180s = table.Column<int>(type: "integer", nullable: false),
                    highest_checkout = table.Column<int>(type: "integer", nullable: false),
                    total_darts = table.Column<int>(type: "integer", nullable: false),
                    total_points = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_players", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dart_throws",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    segment = table.Column<int>(type: "integer", nullable: false),
                    multiplier = table.Column<int>(type: "integer", nullable: false),
                    points = table.Column<int>(type: "integer", nullable: false),
                    round = table.Column<int>(type: "integer", nullable: false),
                    dart_number = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_bust = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dart_throws", x => x.id);
                    table.ForeignKey(
                        name: "FK_dart_throws_games_game_id",
                        column: x => x.game_id,
                        principalTable: "games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "game_players",
                columns: table => new
                {
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_score = table.Column<int>(type: "integer", nullable: false),
                    turn_order = table.Column<int>(type: "integer", nullable: false),
                    darts_thrown = table.Column<int>(type: "integer", nullable: false),
                    legs_won = table.Column<int>(type: "integer", nullable: false),
                    sets_won = table.Column<int>(type: "integer", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_players", x => new { x.game_id, x.player_id });
                    table.ForeignKey(
                        name: "FK_game_players_games_game_id",
                        column: x => x.game_id,
                        principalTable: "games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dart_throws_game_id",
                table: "dart_throws",
                column: "game_id");

            migrationBuilder.CreateIndex(
                name: "IX_dart_throws_player_id",
                table: "dart_throws",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "IX_dart_throws_timestamp",
                table: "dart_throws",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_players_email",
                table: "players",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_players_username",
                table: "players",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dart_throws");

            migrationBuilder.DropTable(
                name: "game_players");

            migrationBuilder.DropTable(
                name: "players");

            migrationBuilder.DropTable(
                name: "games");
        }
    }
}
