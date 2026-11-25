using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DATN_BE.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDependencyTypeFromCardDependency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DependencyType",
                table: "CardDependencies");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DependencyType",
                table: "CardDependencies",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");
        }
    }
}
