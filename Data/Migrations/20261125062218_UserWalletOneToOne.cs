using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class UserWalletOneToOne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserWallets_UserId",
                table: "UserWallets");

            migrationBuilder.CreateIndex(
                name: "IX_UserWallets_UserId",
                table: "UserWallets",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserWallets_UserId",
                table: "UserWallets");

            migrationBuilder.CreateIndex(
                name: "IX_UserWallets_UserId",
                table: "UserWallets",
                column: "UserId");
        }
    }
}
