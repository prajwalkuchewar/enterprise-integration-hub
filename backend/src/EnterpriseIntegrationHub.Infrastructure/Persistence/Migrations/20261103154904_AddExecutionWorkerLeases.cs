using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseIntegrationHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExecutionWorkerLeases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AvailableAt",
                table: "ExecutionDispatchInbox",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAt",
                table: "ExecutionDispatchInbox",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LeaseExpiresAt",
                table: "ExecutionDispatchInbox",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LeaseToken",
                table: "ExecutionDispatchInbox",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionDispatchInbox_CompletedAt_AvailableAt_LeaseExpiresAt",
                table: "ExecutionDispatchInbox",
                columns: new[] { "CompletedAt", "AvailableAt", "LeaseExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionDispatchInbox_LeaseToken",
                table: "ExecutionDispatchInbox",
                column: "LeaseToken",
                unique: true,
                filter: "[LeaseToken] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExecutionDispatchInbox_CompletedAt_AvailableAt_LeaseExpiresAt",
                table: "ExecutionDispatchInbox");

            migrationBuilder.DropIndex(
                name: "IX_ExecutionDispatchInbox_LeaseToken",
                table: "ExecutionDispatchInbox");

            migrationBuilder.DropColumn(
                name: "AvailableAt",
                table: "ExecutionDispatchInbox");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "ExecutionDispatchInbox");

            migrationBuilder.DropColumn(
                name: "LeaseExpiresAt",
                table: "ExecutionDispatchInbox");

            migrationBuilder.DropColumn(
                name: "LeaseToken",
                table: "ExecutionDispatchInbox");
        }
    }
}
