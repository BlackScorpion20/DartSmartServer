using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DartSmartNet.Server.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePlayerStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BestSessionAverage",
                table: "player_stats",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CurrentLossStreak",
                table: "player_stats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentWinStreak",
                table: "player_stats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HighestScore",
                table: "player_stats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastGameAt",
                table: "player_stats",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LongestLossStreak",
                table: "player_stats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LongestWinStreak",
                table: "player_stats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Total100Plus",
                table: "player_stats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalCheckouts",
                table: "player_stats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalDoubleAttempts",
                table: "player_stats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalDoubleHits",
                table: "player_stats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalFirst9Attempts",
                table: "player_stats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalFirst9Points",
                table: "player_stats",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TotalLegsPlayed",
                table: "player_stats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalLegsWon",
                table: "player_stats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "WorstSessionAverage",
                table: "player_stats",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Tournaments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OrganizerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Format = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    GameType = table.Column<int>(type: "integer", nullable: false),
                    StartingScore = table.Column<int>(type: "integer", nullable: false),
                    LegsToWin = table.Column<int>(type: "integer", nullable: false),
                    SetsToWin = table.Column<int>(type: "integer", nullable: false),
                    MaxParticipants = table.Column<int>(type: "integer", nullable: false),
                    MinParticipants = table.Column<int>(type: "integer", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    JoinCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ScheduledStartAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WinnerId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tournaments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tournaments_users_OrganizerId",
                        column: x => x.OrganizerId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tournaments_users_WinnerId",
                        column: x => x.WinnerId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TournamentParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Seed = table.Column<int>(type: "integer", nullable: true),
                    FinalPlacement = table.Column<int>(type: "integer", nullable: true),
                    IsEliminated = table.Column<bool>(type: "boolean", nullable: false),
                    MatchesWon = table.Column<int>(type: "integer", nullable: false),
                    MatchesLost = table.Column<int>(type: "integer", nullable: false),
                    LegsWon = table.Column<int>(type: "integer", nullable: false),
                    LegsLost = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EliminatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentParticipants_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentParticipants_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TournamentMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Round = table.Column<int>(type: "integer", nullable: false),
                    MatchNumber = table.Column<int>(type: "integer", nullable: false),
                    IsLosersBracket = table.Column<bool>(type: "boolean", nullable: false),
                    Player1Id = table.Column<Guid>(type: "uuid", nullable: true),
                    Player2Id = table.Column<Guid>(type: "uuid", nullable: true),
                    WinnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    GameSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    NextMatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    LoserNextMatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Player1Legs = table.Column<int>(type: "integer", nullable: false),
                    Player2Legs = table.Column<int>(type: "integer", nullable: false),
                    Player1Sets = table.Column<int>(type: "integer", nullable: false),
                    Player2Sets = table.Column<int>(type: "integer", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentMatches_TournamentMatches_LoserNextMatchId",
                        column: x => x.LoserNextMatchId,
                        principalTable: "TournamentMatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentMatches_TournamentMatches_NextMatchId",
                        column: x => x.NextMatchId,
                        principalTable: "TournamentMatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentMatches_TournamentParticipants_Player1Id",
                        column: x => x.Player1Id,
                        principalTable: "TournamentParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentMatches_TournamentParticipants_Player2Id",
                        column: x => x.Player2Id,
                        principalTable: "TournamentParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentMatches_TournamentParticipants_WinnerId",
                        column: x => x.WinnerId,
                        principalTable: "TournamentParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentMatches_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentMatches_game_sessions_GameSessionId",
                        column: x => x.GameSessionId,
                        principalTable: "game_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_GameSessionId",
                table: "TournamentMatches",
                column: "GameSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_LoserNextMatchId",
                table: "TournamentMatches",
                column: "LoserNextMatchId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_NextMatchId",
                table: "TournamentMatches",
                column: "NextMatchId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_Player1Id",
                table: "TournamentMatches",
                column: "Player1Id");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_Player2Id",
                table: "TournamentMatches",
                column: "Player2Id");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_Status",
                table: "TournamentMatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_TournamentId_Round_MatchNumber",
                table: "TournamentMatches",
                columns: new[] { "TournamentId", "Round", "MatchNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_WinnerId",
                table: "TournamentMatches",
                column: "WinnerId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentParticipants_TournamentId_UserId",
                table: "TournamentParticipants",
                columns: new[] { "TournamentId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentParticipants_UserId",
                table: "TournamentParticipants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_IsPublic",
                table: "Tournaments",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_JoinCode",
                table: "Tournaments",
                column: "JoinCode",
                unique: true,
                filter: "\"JoinCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_OrganizerId",
                table: "Tournaments",
                column: "OrganizerId");

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_Status",
                table: "Tournaments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_WinnerId",
                table: "Tournaments",
                column: "WinnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TournamentMatches");

            migrationBuilder.DropTable(
                name: "TournamentParticipants");

            migrationBuilder.DropTable(
                name: "Tournaments");

            migrationBuilder.DropColumn(
                name: "BestSessionAverage",
                table: "player_stats");

            migrationBuilder.DropColumn(
                name: "CurrentLossStreak",
                table: "player_stats");

            migrationBuilder.DropColumn(
                name: "CurrentWinStreak",
                table: "player_stats");

            migrationBuilder.DropColumn(
                name: "HighestScore",
                table: "player_stats");

            migrationBuilder.DropColumn(
                name: "LastGameAt",
                table: "player_stats");

            migrationBuilder.DropColumn(
                name: "LongestLossStreak",
                table: "player_stats");

            migrationBuilder.DropColumn(
                name: "LongestWinStreak",
                table: "player_stats");

            migrationBuilder.DropColumn(
                name: "Total100Plus",
                table: "player_stats");

            migrationBuilder.DropColumn(
                name: "TotalCheckouts",
                table: "player_stats");

            migrationBuilder.DropColumn(
                name: "TotalDoubleAttempts",
                table: "player_stats");

            migrationBuilder.DropColumn(
                name: "TotalDoubleHits",
                table: "player_stats");

            migrationBuilder.DropColumn(
                name: "TotalFirst9Attempts",
                table: "player_stats");

            migrationBuilder.DropColumn(
                name: "TotalFirst9Points",
                table: "player_stats");

            migrationBuilder.DropColumn(
                name: "TotalLegsPlayed",
                table: "player_stats");

            migrationBuilder.DropColumn(
                name: "TotalLegsWon",
                table: "player_stats");

            migrationBuilder.DropColumn(
                name: "WorstSessionAverage",
                table: "player_stats");
        }
    }
}
