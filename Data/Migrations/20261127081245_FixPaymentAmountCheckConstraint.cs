using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class FixPaymentAmountCheckConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_Amount_EqualsParts",
                table: "Payments");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_Amount_EqualsParts",
                table: "Payments",
                sql: "[WalletAmount] <= [Amount] AND [GatewayAmount] = [Amount] - [WalletAmount]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_Amount_EqualsParts",
                table: "Payments");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_Amount_EqualsParts",
                table: "Payments",
                sql: "[WalletAmount] <= 9223372036854775807 - [GatewayAmount] AND [Amount] = [WalletAmount] + [GatewayAmount]");
        }
    }
}
