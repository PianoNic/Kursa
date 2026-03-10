using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kursa.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudySessionEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudySessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    WorkMinutes = table.Column<int>(type: "integer", nullable: false),
                    BreakMinutes = table.Column<int>(type: "integer", nullable: false),
                    CompletedPomodoros = table.Column<int>(type: "integer", nullable: false),
                    TotalDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    CardsReviewed = table.Column<int>(type: "integer", nullable: false),
                    QuizQuestionsAnswered = table.Column<int>(type: "integer", nullable: false),
                    QuizCorrectAnswers = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudySessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudySessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudySessions_UserId",
                table: "StudySessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudySessions");
        }
    }
}
