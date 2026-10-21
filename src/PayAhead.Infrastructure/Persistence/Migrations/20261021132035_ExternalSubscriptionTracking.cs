using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayAhead.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExternalSubscriptionTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReminderRules_PaymentPlanId_DaysBefore_TimeOfDay",
                table: "ReminderRules");

            migrationBuilder.AddColumn<int>(
                name: "Target",
                table: "ReminderRules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "TrackedSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TrialStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TrialEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AutoRenew = table.Column<bool>(type: "bit", nullable: false),
                    EnableTrialReminders = table.Column<bool>(type: "bit", nullable: false),
                    CancellationRequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AccessEndsOn = table.Column<DateOnly>(type: "date", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackedSubscriptions", x => x.Id);
                    table.CheckConstraint("CK_TrackedSubscriptions_Access", "[AccessEndsOn] IS NULL OR [AccessEndsOn] >= [StartDate]");
                    table.CheckConstraint("CK_TrackedSubscriptions_Status", "[Status] BETWEEN 0 AND 3");
                    table.CheckConstraint("CK_TrackedSubscriptions_Trial", "([TrialStartDate] IS NULL AND [TrialEndDate] IS NULL) OR ([TrialStartDate] IS NOT NULL AND [TrialEndDate] IS NOT NULL AND [TrialEndDate] >= [TrialStartDate] AND [TrialStartDate] >= [StartDate])");
                    table.CheckConstraint("CK_TrackedSubscriptions_TrialRequired", "[Status] <> 0 OR [TrialEndDate] IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_TrackedSubscriptions_PaymentPlans_PaymentPlanId_UserId",
                        columns: x => new { x.PaymentPlanId, x.UserId },
                        principalTable: "PaymentPlans",
                        principalColumns: new[] { "Id", "UserId" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReminderRules_PaymentPlanId_Target_DaysBefore_TimeOfDay",
                table: "ReminderRules",
                columns: new[] { "PaymentPlanId", "Target", "DaysBefore", "TimeOfDay" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TrackedSubscriptions_PaymentPlanId",
                table: "TrackedSubscriptions",
                column: "PaymentPlanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrackedSubscriptions_PaymentPlanId_UserId",
                table: "TrackedSubscriptions",
                columns: new[] { "PaymentPlanId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrackedSubscriptions_UserId_IsDeleted_CreatedAtUtc_Id",
                table: "TrackedSubscriptions",
                columns: new[] { "UserId", "IsDeleted", "CreatedAtUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrackedSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_ReminderRules_PaymentPlanId_Target_DaysBefore_TimeOfDay",
                table: "ReminderRules");

            migrationBuilder.DropColumn(
                name: "Target",
                table: "ReminderRules");

            migrationBuilder.CreateIndex(
                name: "IX_ReminderRules_PaymentPlanId_DaysBefore_TimeOfDay",
                table: "ReminderRules",
                columns: new[] { "PaymentPlanId", "DaysBefore", "TimeOfDay" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
