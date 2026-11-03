using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseIntegrationHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExecutionDispatchOutboxLease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "ExecutionDispatchOutbox",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeadLetteredAt",
                table: "ExecutionDispatchOutbox",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "ExecutionDispatchOutbox",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LeaseExpiresAt",
                table: "ExecutionDispatchOutbox",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeaseOwner",
                table: "ExecutionDispatchOutbox",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextAttemptAt",
                table: "ExecutionDispatchOutbox",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionDispatchOutbox_Dispatchable",
                table: "ExecutionDispatchOutbox",
                columns: new[] { "DispatchedAt", "DeadLetteredAt", "NextAttemptAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExecutionDispatchOutbox_Dispatchable",
                table: "ExecutionDispatchOutbox");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "ExecutionDispatchOutbox");

            migrationBuilder.DropColumn(
                name: "DeadLetteredAt",
                table: "ExecutionDispatchOutbox");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "ExecutionDispatchOutbox");

            migrationBuilder.DropColumn(
                name: "LeaseExpiresAt",
                table: "ExecutionDispatchOutbox");

            migrationBuilder.DropColumn(
                name: "LeaseOwner",
                table: "ExecutionDispatchOutbox");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                table: "ExecutionDispatchOutbox");
        }
    }
}
