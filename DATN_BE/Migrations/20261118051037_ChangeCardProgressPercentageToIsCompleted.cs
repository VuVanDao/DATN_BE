using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DATN_BE.Migrations
{
    /// <inheritdoc />
    public partial class ChangeCardProgressPercentageToIsCompleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "Cards",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE \"Cards\" SET \"IsCompleted\" = TRUE WHERE \"ProgressPercentage\" = 100;");

            migrationBuilder.DropColumn(
                name: "ProgressPercentage",
                table: "Cards");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProgressPercentage",
                table: "Cards",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE \"Cards\" SET \"ProgressPercentage\" = 100 WHERE \"IsCompleted\" = TRUE;");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "Cards");
        }
    }
}
