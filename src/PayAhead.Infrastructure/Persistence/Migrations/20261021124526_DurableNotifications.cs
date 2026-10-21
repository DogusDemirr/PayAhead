using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayAhead.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DurableNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReminderRules_PaymentPlanId_DaysBefore_TimeOfDay",
                table: "ReminderRules");

            migrationBuilder.DropIndex(
                name: "IX_NotificationDeliveries_ReminderRuleId_DueDate",
                table: "NotificationDeliveries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NotificationDeliveries_Status",
                table: "NotificationDeliveries");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ReminderRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceTokenId",
                table: "NotificationDeliveries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastErrorCode",
                table: "NotificationDeliveries",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LeaseExpiresAtUtc",
                table: "NotificationDeliveries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LeaseOwner",
                table: "NotificationDeliveries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextAttemptAtUtc",
                table: "NotificationDeliveries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderMessageId",
                table: "NotificationDeliveries",
                type: "varchar(128)",
                unicode: false,
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReceiptAttemptCount",
                table: "NotificationDeliveries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TicketAcceptedAtUtc",
                table: "NotificationDeliveries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "NotificationDeliveries",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Legacy deliveries have no device identity; retain audit rows without sending them.
            migrationBuilder.Sql("UPDATE NotificationDeliveries SET UpdatedAtUtc=SYSUTCDATETIME(), Status=CASE WHEN Status=2 THEN 2 ELSE 5 END, LastErrorCode='LegacyUnassigned' WHERE DeviceTokenId IS NULL");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_DeviceTokens_Id_UserId",
                table: "DeviceTokens",
                columns: new[] { "Id", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReminderRules_PaymentPlanId_DaysBefore_TimeOfDay",
                table: "ReminderRules",
                columns: new[] { "PaymentPlanId", "DaysBefore", "TimeOfDay" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_DeviceTokenId_UserId",
                table: "NotificationDeliveries",
                columns: new[] { "DeviceTokenId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_ReminderRuleId_DueDate_DeviceTokenId",
                table: "NotificationDeliveries",
                columns: new[] { "ReminderRuleId", "DueDate", "DeviceTokenId" },
                unique: true,
                filter: "[DeviceTokenId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_Status_NextAttemptAtUtc_LeaseExpiresAtUtc",
                table: "NotificationDeliveries",
                columns: new[] { "Status", "NextAttemptAtUtc", "LeaseExpiresAtUtc" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_NotificationDeliveries_ReceiptAttempts",
                table: "NotificationDeliveries",
                sql: "[ReceiptAttemptCount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_NotificationDeliveries_Status",
                table: "NotificationDeliveries",
                sql: "[Status] BETWEEN 0 AND 5");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationDeliveries_DeviceTokens_DeviceTokenId_UserId",
                table: "NotificationDeliveries",
                columns: new[] { "DeviceTokenId", "UserId" },
                principalTable: "DeviceTokens",
                principalColumns: new[] { "Id", "UserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationDeliveries_DeviceTokens_DeviceTokenId_UserId",
                table: "NotificationDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_ReminderRules_PaymentPlanId_DaysBefore_TimeOfDay",
                table: "ReminderRules");

            migrationBuilder.DropIndex(
                name: "IX_NotificationDeliveries_DeviceTokenId_UserId",
                table: "NotificationDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_NotificationDeliveries_ReminderRuleId_DueDate_DeviceTokenId",
                table: "NotificationDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_NotificationDeliveries_Status_NextAttemptAtUtc_LeaseExpiresAtUtc",
                table: "NotificationDeliveries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NotificationDeliveries_ReceiptAttempts",
                table: "NotificationDeliveries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NotificationDeliveries_Status",
                table: "NotificationDeliveries");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_DeviceTokens_Id_UserId",
                table: "DeviceTokens");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ReminderRules");

            migrationBuilder.DropColumn(
                name: "DeviceTokenId",
                table: "NotificationDeliveries");

            migrationBuilder.DropColumn(
                name: "LastErrorCode",
                table: "NotificationDeliveries");

            migrationBuilder.DropColumn(
                name: "LeaseExpiresAtUtc",
                table: "NotificationDeliveries");

            migrationBuilder.DropColumn(
                name: "LeaseOwner",
                table: "NotificationDeliveries");

            migrationBuilder.DropColumn(
                name: "NextAttemptAtUtc",
                table: "NotificationDeliveries");

            migrationBuilder.DropColumn(
                name: "ProviderMessageId",
                table: "NotificationDeliveries");

            migrationBuilder.DropColumn(
                name: "ReceiptAttemptCount",
                table: "NotificationDeliveries");

            migrationBuilder.DropColumn(
                name: "TicketAcceptedAtUtc",
                table: "NotificationDeliveries");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "NotificationDeliveries");

            migrationBuilder.CreateIndex(
                name: "IX_ReminderRules_PaymentPlanId_DaysBefore_TimeOfDay",
                table: "ReminderRules",
                columns: new[] { "PaymentPlanId", "DaysBefore", "TimeOfDay" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_ReminderRuleId_DueDate",
                table: "NotificationDeliveries",
                columns: new[] { "ReminderRuleId", "DueDate" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_NotificationDeliveries_Status",
                table: "NotificationDeliveries",
                sql: "[Status] BETWEEN 0 AND 3");
        }
    }
}
