using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentAttemptConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentAttempts_PaymentId",
                table: "PaymentAttempts");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PaymentAttempts",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationStartedAt",
                table: "PaymentAttempts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_PaymentId",
                table: "PaymentAttempts",
                column: "PaymentId",
                unique: true,
                filter: "[Status] IN (0, 4)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentAttempts_PaymentId",
                table: "PaymentAttempts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PaymentAttempts");

            migrationBuilder.DropColumn(
                name: "VerificationStartedAt",
                table: "PaymentAttempts");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_PaymentId",
                table: "PaymentAttempts",
                column: "PaymentId",
                unique: true,
                filter: "[Status] = 0");
        }
    }
}
