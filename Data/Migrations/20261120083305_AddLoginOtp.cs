using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLoginOtp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoginOtps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    PhoneNumber = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RequestIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    LastModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreationDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginOtps", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoginOtps_ExpiresAtUtc",
                table: "LoginOtps",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_LoginOtps_PhoneNumber_CreationDateTime",
                table: "LoginOtps",
                columns: new[] { "PhoneNumber", "CreationDateTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoginOtps");
        }
    }
}
