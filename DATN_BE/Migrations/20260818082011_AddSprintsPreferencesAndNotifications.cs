using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DATN_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddSprintsPreferencesAndNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiverId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<int>(type: "integer", maxLength: 50, nullable: true),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Sprints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Goal = table.Column<string>(type: "text", nullable: true),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sprints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sprints_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserBoardPreferences",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BoardId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LastViewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBoardPreferences", x => new { x.UserId, x.BoardId });
                    table.ForeignKey(
                        name: "FK_UserBoardPreferences_Boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "Boards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserBoardPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cards_SprintId",
                table: "Cards",
                column: "SprintId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ReceiverId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "ReceiverId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_SenderId",
                table: "Notifications",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Sprints_ProjectId",
                table: "Sprints",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBoardPreferences_BoardId",
                table: "UserBoardPreferences",
                column: "BoardId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_Sprints_SprintId",
                table: "Cards",
                column: "SprintId",
                principalTable: "Sprints",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_Sprints_SprintId",
                table: "Cards");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Sprints");

            migrationBuilder.DropTable(
                name: "UserBoardPreferences");

            migrationBuilder.DropIndex(
                name: "IX_Cards_SprintId",
                table: "Cards");
        }
    }
}
