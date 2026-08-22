using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DATN_BE.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueConstraintOnPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Columns_BoardId_Position",
                table: "Columns");

            migrationBuilder.DropIndex(
                name: "IX_Cards_ColumnId_Position",
                table: "Cards");

            migrationBuilder.CreateIndex(
                name: "IX_Columns_BoardId_Position",
                table: "Columns",
                columns: new[] { "BoardId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_Cards_ColumnId_Position",
                table: "Cards",
                columns: new[] { "ColumnId", "Position" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Columns_BoardId_Position",
                table: "Columns");

            migrationBuilder.DropIndex(
                name: "IX_Cards_ColumnId_Position",
                table: "Cards");

            migrationBuilder.CreateIndex(
                name: "IX_Columns_BoardId_Position",
                table: "Columns",
                columns: new[] { "BoardId", "Position" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_ColumnId_Position",
                table: "Cards",
                columns: new[] { "ColumnId", "Position" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }
    }
}
