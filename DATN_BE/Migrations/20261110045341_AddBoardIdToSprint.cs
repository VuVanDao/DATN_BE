using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DATN_BE.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardIdToSprint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BoardId",
                table: "Sprints",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(@"
                UPDATE ""Sprints""
                SET ""BoardId"" = (
                    SELECT b.""Id"" 
                    FROM ""Boards"" b 
                    WHERE b.""ProjectId"" = ""Sprints"".""ProjectId"" 
                      AND b.""Type"" = 'Scrum' 
                    ORDER BY b.""CreatedAt"" 
                    LIMIT 1
                )
                WHERE ""BoardId"" = '00000000-0000-0000-0000-000000000000';

                UPDATE ""Sprints""
                SET ""BoardId"" = (
                    SELECT b.""Id"" 
                    FROM ""Boards"" b 
                    WHERE b.""ProjectId"" = ""Sprints"".""ProjectId"" 
                    ORDER BY b.""CreatedAt"" 
                    LIMIT 1
                )
                WHERE ""BoardId"" = '00000000-0000-0000-0000-000000000000';

                DELETE FROM ""Sprints"" WHERE ""BoardId"" = '00000000-0000-0000-0000-000000000000';
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Sprints_BoardId",
                table: "Sprints",
                column: "BoardId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sprints_Boards_BoardId",
                table: "Sprints",
                column: "BoardId",
                principalTable: "Boards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sprints_Boards_BoardId",
                table: "Sprints");

            migrationBuilder.DropIndex(
                name: "IX_Sprints_BoardId",
                table: "Sprints");

            migrationBuilder.DropColumn(
                name: "BoardId",
                table: "Sprints");
        }
    }
}
