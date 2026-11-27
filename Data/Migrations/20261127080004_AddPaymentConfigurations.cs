using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Orders_OrderId",
                table: "Payments");

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentId",
                table: "TransactionHistories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Payments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TrackingCode",
                table: "PaymentAttempts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GatewayRequestId",
                table: "PaymentAttempts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GatewayName",
                table: "PaymentAttempts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ErrorMessage",
                table: "PaymentAttempts",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ErrorCode",
                table: "PaymentAttempts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionHistories_PaymentId",
                table: "TransactionHistories",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionHistories_PaymentId_Type",
                table: "TransactionHistories",
                columns: new[] { "PaymentId", "Type" },
                unique: true,
                filter: "[PaymentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status",
                table: "Payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Type",
                table: "Payments",
                column: "Type");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_Amount_EqualsParts",
                table: "Payments",
                sql: "[WalletAmount] <= 9223372036854775807 - [GatewayAmount] AND [Amount] = [WalletAmount] + [GatewayAmount]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_Amount_NonNegative",
                table: "Payments",
                sql: "[Amount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_AtLeastOneAmount",
                table: "Payments",
                sql: "[WalletAmount] > 0 OR [GatewayAmount] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_GatewayAmount_NonNegative",
                table: "Payments",
                sql: "[GatewayAmount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_Method_Amounts",
                table: "Payments",
                sql: "([Method] = 0 AND [WalletAmount] > 0 AND [GatewayAmount] = 0) OR ([Method] = 1 AND [WalletAmount] = 0 AND [GatewayAmount] > 0) OR ([Method] = 2 AND [WalletAmount] > 0 AND [GatewayAmount] > 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_Type_OrderId",
                table: "Payments",
                sql: "([Type] = 0 AND [OrderId] IS NOT NULL) OR ([Type] = 1 AND [OrderId] IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_WalletAmount_NonNegative",
                table: "Payments",
                sql: "[WalletAmount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_WalletCharge_Method",
                table: "Payments",
                sql: "[Type] <> 1 OR [Method] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_GatewayRequestId",
                table: "PaymentAttempts",
                column: "GatewayRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_Status",
                table: "PaymentAttempts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_TrackingCode",
                table: "PaymentAttempts",
                column: "TrackingCode");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PaymentAttempts_Amount_Positive",
                table: "PaymentAttempts",
                sql: "[Amount] > 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Orders_OrderId",
                table: "Payments",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionHistories_Payments_PaymentId",
                table: "TransactionHistories",
                column: "PaymentId",
                principalTable: "Payments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Orders_OrderId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_TransactionHistories_Payments_PaymentId",
                table: "TransactionHistories");

            migrationBuilder.DropIndex(
                name: "IX_TransactionHistories_PaymentId",
                table: "TransactionHistories");

            migrationBuilder.DropIndex(
                name: "IX_TransactionHistories_PaymentId_Type",
                table: "TransactionHistories");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Status",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Type",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_Amount_EqualsParts",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_Amount_NonNegative",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_AtLeastOneAmount",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_GatewayAmount_NonNegative",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_Method_Amounts",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_Type_OrderId",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_WalletAmount_NonNegative",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_WalletCharge_Method",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_PaymentAttempts_GatewayRequestId",
                table: "PaymentAttempts");

            migrationBuilder.DropIndex(
                name: "IX_PaymentAttempts_Status",
                table: "PaymentAttempts");

            migrationBuilder.DropIndex(
                name: "IX_PaymentAttempts_TrackingCode",
                table: "PaymentAttempts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PaymentAttempts_Amount_Positive",
                table: "PaymentAttempts");

            migrationBuilder.DropColumn(
                name: "PaymentId",
                table: "TransactionHistories");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TrackingCode",
                table: "PaymentAttempts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GatewayRequestId",
                table: "PaymentAttempts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GatewayName",
                table: "PaymentAttempts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "ErrorMessage",
                table: "PaymentAttempts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ErrorCode",
                table: "PaymentAttempts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Orders_OrderId",
                table: "Payments",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id");
        }
    }
}
