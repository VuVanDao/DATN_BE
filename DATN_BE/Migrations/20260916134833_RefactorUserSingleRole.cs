using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DATN_BE.Migrations
{
    /// <inheritdoc />
    public partial class RefactorUserSingleRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Thêm cột RoleId (tạm thời nullable để backfill data an toàn)
            migrationBuilder.AddColumn<Guid>(
                name: "RoleId",
                table: "Users",
                type: "uuid",
                nullable: true);

            // 2. Chuyển dữ liệu role từ UserRoles sang Users nếu có
            migrationBuilder.Sql(@"
                UPDATE ""Users"" u
                SET ""RoleId"" = ur.""RoleId""
                FROM ""UserRoles"" ur
                WHERE u.""Id"" = ur.""UserId"";
            ");

            // 3. Đảm bảo Role 'User' tồn tại và gán cho các user chưa có RoleId
            migrationBuilder.Sql(@"
                INSERT INTO ""Roles"" (""Id"", ""Name"", ""Scope"", ""Description"", ""IsSystemRole"", ""CreatedAt"", ""UpdatedAt"")
                SELECT gen_random_uuid(), 'User', 'Global', 'Người dùng hệ thống', true, NOW(), NOW()
                WHERE NOT EXISTS (SELECT 1 FROM ""Roles"" WHERE ""Name"" = 'User');

                UPDATE ""Users""
                SET ""RoleId"" = (SELECT ""Id"" FROM ""Roles"" WHERE ""Name"" = 'User' LIMIT 1)
                WHERE ""RoleId"" IS NULL;
            ");

            // 4. Đổi cột RoleId thành NOT NULL
            migrationBuilder.AlterColumn<Guid>(
                name: "RoleId",
                table: "Users",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // 5. Xóa bảng trung gian UserRoles
            migrationBuilder.DropTable(
                name: "UserRoles");

            // 6. Tạo Index và Khóa ngoại Foreign Key
            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_RoleId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "Users");

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");
        }
    }
}
