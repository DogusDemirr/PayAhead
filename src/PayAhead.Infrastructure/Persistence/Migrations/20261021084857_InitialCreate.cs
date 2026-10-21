using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayAhead.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DeviceTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Platform = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    PushToken = table.Column<string>(type: "varchar(512)", unicode: false, maxLength: 512, nullable: false, collation: "Latin1_General_100_BIN2"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceTokens", x => x.Id);
                    table.CheckConstraint("CK_DeviceTokens_Platform", "[Platform] IN ('ios', 'android')");
                    table.ForeignKey(
                        name: "FK_DeviceTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PaymentPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CurrencyCode = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Frequency = table.Column<int>(type: "int", nullable: false),
                    Interval = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AnchorDay = table.Column<int>(type: "int", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentPlans", x => x.Id);
                    table.UniqueConstraint("AK_PaymentPlans_Id_UserId", x => new { x.Id, x.UserId });
                    table.CheckConstraint("CK_PaymentPlans_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_PaymentPlans_Anchor", "[AnchorDay] IS NULL OR [AnchorDay] BETWEEN 1 AND 31");
                    table.CheckConstraint("CK_PaymentPlans_Currency", "[CurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z]%' AND LEN([CurrencyCode]) = 3");
                    table.CheckConstraint("CK_PaymentPlans_Dates", "[EndDate] IS NULL OR [EndDate] >= [StartDate]");
                    table.CheckConstraint("CK_PaymentPlans_Frequency", "[Frequency] BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_PaymentPlans_Interval", "[Interval] > 0");
                    table.ForeignKey(
                        name: "FK_PaymentPlans_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReplacedByTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.CheckConstraint("CK_RefreshTokens_Expiry", "[ExpiresAtUtc] > [CreatedAtUtc]");
                    table.ForeignKey(
                        name: "FK_RefreshTokens_RefreshTokens_ReplacedByTokenId",
                        column: x => x.ReplacedByTokenId,
                        principalTable: "RefreshTokens",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    DefaultCurrencyCode = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NotificationsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ProductId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExternalSubscriptionId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CurrentPeriodEndUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastVerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PaymentOccurrences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentOccurrences", x => x.Id);
                    table.CheckConstraint("CK_PaymentOccurrences_Amount", "[ActualAmount] IS NULL OR [ActualAmount] > 0");
                    table.CheckConstraint("CK_PaymentOccurrences_State", "([Status] = 1 AND [PaidAtUtc] IS NOT NULL AND [ActualAmount] IS NOT NULL) OR ([Status] <> 1 AND [PaidAtUtc] IS NULL AND [ActualAmount] IS NULL)");
                    table.CheckConstraint("CK_PaymentOccurrences_Status", "[Status] BETWEEN 0 AND 2");
                    table.ForeignKey(
                        name: "FK_PaymentOccurrences_PaymentPlans_PaymentPlanId",
                        column: x => x.PaymentPlanId,
                        principalTable: "PaymentPlans",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReminderRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DaysBefore = table.Column<int>(type: "int", nullable: false),
                    TimeOfDay = table.Column<TimeOnly>(type: "time", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReminderRules", x => x.Id);
                    table.UniqueConstraint("AK_ReminderRules_Id_PaymentPlanId_UserId", x => new { x.Id, x.PaymentPlanId, x.UserId });
                    table.CheckConstraint("CK_ReminderRules_DaysBefore", "[DaysBefore] BETWEEN 0 AND 365");
                    table.ForeignKey(
                        name: "FK_ReminderRules_PaymentPlans_PaymentPlanId_UserId",
                        columns: x => new { x.PaymentPlanId, x.UserId },
                        principalTable: "PaymentPlans",
                        principalColumns: new[] { "Id", "UserId" });
                });

            migrationBuilder.CreateTable(
                name: "NotificationDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReminderRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastAttemptAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDeliveries", x => x.Id);
                    table.CheckConstraint("CK_NotificationDeliveries_Attempts", "[AttemptCount] >= 0");
                    table.CheckConstraint("CK_NotificationDeliveries_Status", "[Status] BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_NotificationDeliveries_ReminderRules_ReminderRuleId_PaymentPlanId_UserId",
                        columns: x => new { x.ReminderRuleId, x.PaymentPlanId, x.UserId },
                        principalTable: "ReminderRules",
                        principalColumns: new[] { "Id", "PaymentPlanId", "UserId" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_CreatedAtUtc_Id",
                table: "AuditLogs",
                columns: new[] { "UserId", "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTokens_PushToken",
                table: "DeviceTokens",
                column: "PushToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTokens_UserId_IsActive",
                table: "DeviceTokens",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_ReminderRuleId_DueDate",
                table: "NotificationDeliveries",
                columns: new[] { "ReminderRuleId", "DueDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_ReminderRuleId_PaymentPlanId_UserId",
                table: "NotificationDeliveries",
                columns: new[] { "ReminderRuleId", "PaymentPlanId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_Status_ScheduledAtUtc",
                table: "NotificationDeliveries",
                columns: new[] { "Status", "ScheduledAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_UserId_DueDate",
                table: "NotificationDeliveries",
                columns: new[] { "UserId", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOccurrences_PaymentPlanId_DueDate",
                table: "PaymentOccurrences",
                columns: new[] { "PaymentPlanId", "DueDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlans_UserId_IsActive_StartDate_Id",
                table: "PaymentPlans",
                columns: new[] { "UserId", "IsActive", "StartDate", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ReplacedByTokenId",
                table: "RefreshTokens",
                column: "ReplacedByTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_ExpiresAtUtc",
                table: "RefreshTokens",
                columns: new[] { "UserId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReminderRules_PaymentPlanId_DaysBefore_TimeOfDay",
                table: "ReminderRules",
                columns: new[] { "PaymentPlanId", "DaysBefore", "TimeOfDay" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReminderRules_PaymentPlanId_UserId",
                table: "ReminderRules",
                columns: new[] { "PaymentPlanId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReminderRules_UserId",
                table: "ReminderRules",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedEmail",
                table: "Users",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_Provider_ExternalSubscriptionId",
                table: "UserSubscriptions",
                columns: new[] { "Provider", "ExternalSubscriptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId_CurrentPeriodEndUtc",
                table: "UserSubscriptions",
                columns: new[] { "UserId", "CurrentPeriodEndUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "DeviceTokens");

            migrationBuilder.DropTable(
                name: "NotificationDeliveries");

            migrationBuilder.DropTable(
                name: "PaymentOccurrences");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "UserSettings");

            migrationBuilder.DropTable(
                name: "UserSubscriptions");

            migrationBuilder.DropTable(
                name: "ReminderRules");

            migrationBuilder.DropTable(
                name: "PaymentPlans");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
