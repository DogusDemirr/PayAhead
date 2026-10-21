using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayAhead.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VerifiedPremiumMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoRenew",
                table: "UserSubscriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "UserSubscriptions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsStoreVerified",
                table: "UserSubscriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextVerificationAtUtc",
                table: "UserSubscriptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProtectedEvidence",
                table: "UserSubscriptions",
                type: "varchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "UserSubscriptions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "StoreEnvironment",
                table: "UserSubscriptions",
                type: "varchar(16)",
                unicode: false,
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "UserSubscriptions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddUniqueConstraint(
                name: "AK_UserSubscriptions_Id_UserId",
                table: "UserSubscriptions",
                columns: new[] { "Id", "UserId" });

            migrationBuilder.CreateTable(
                name: "StorePurchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Store = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    TransactionId = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false, collation: "Latin1_General_100_BIN2"),
                    TokenHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true, collation: "Latin1_General_100_BIN2"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorePurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorePurchases_UserSubscriptions_UserSubscriptionId_UserId",
                        columns: x => new { x.UserSubscriptionId, x.UserId },
                        principalTable: "UserSubscriptions",
                        principalColumns: new[] { "Id", "UserId" });
                });

            migrationBuilder.CreateTable(
                name: "StoreTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Store = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    TransactionId = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false, collation: "Latin1_General_100_BIN2"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreTransactions_UserSubscriptions_UserSubscriptionId_UserId",
                        columns: x => new { x.UserSubscriptionId, x.UserId },
                        principalTable: "UserSubscriptions",
                        principalColumns: new[] { "Id", "UserId" });
                });

            migrationBuilder.CreateTable(
                name: "StoreWebhookInbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Store = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    EventId = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false, collation: "Latin1_General_100_BIN2"),
                    ReferenceHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    ProtectedReference = table.Column<string>(type: "varchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LeaseOwner = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeaseExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorCode = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreWebhookInbox", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StorePurchases_Store_TokenHash",
                table: "StorePurchases",
                columns: new[] { "Store", "TokenHash" },
                unique: true,
                filter: "[TokenHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StorePurchases_Store_TransactionId",
                table: "StorePurchases",
                columns: new[] { "Store", "TransactionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StorePurchases_UserSubscriptionId_UserId",
                table: "StorePurchases",
                columns: new[] { "UserSubscriptionId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_StoreTransactions_Store_TransactionId",
                table: "StoreTransactions",
                columns: new[] { "Store", "TransactionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreTransactions_UserSubscriptionId_UserId",
                table: "StoreTransactions",
                columns: new[] { "UserSubscriptionId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_StoreWebhookInbox_CompletedAtUtc_NextAttemptAtUtc_LeaseExpiresAtUtc",
                table: "StoreWebhookInbox",
                columns: new[] { "CompletedAtUtc", "NextAttemptAtUtc", "LeaseExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StoreWebhookInbox_Store_EventId",
                table: "StoreWebhookInbox",
                columns: new[] { "Store", "EventId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StorePurchases");

            migrationBuilder.DropTable(
                name: "StoreTransactions");

            migrationBuilder.DropTable(
                name: "StoreWebhookInbox");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_UserSubscriptions_Id_UserId",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "AutoRenew",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "IsStoreVerified",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "NextVerificationAtUtc",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "ProtectedEvidence",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "StoreEnvironment",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "UserSubscriptions");
        }
    }
}
