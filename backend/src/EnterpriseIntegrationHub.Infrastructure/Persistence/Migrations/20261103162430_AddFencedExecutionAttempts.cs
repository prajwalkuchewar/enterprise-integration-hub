using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseIntegrationHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFencedExecutionAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActiveAttemptId",
                table: "ExecutionDispatchInbox",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ActiveAttemptLeaseToken",
                table: "ExecutionDispatchInbox",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionDispatchInbox_ActiveAttemptId",
                table: "ExecutionDispatchInbox",
                column: "ActiveAttemptId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ExecutionDispatchInbox_ActiveAttempt",
                table: "ExecutionDispatchInbox",
                sql: "([ActiveAttemptId] IS NULL AND [ActiveAttemptLeaseToken] IS NULL) OR ([ActiveAttemptId] IS NOT NULL AND [ActiveAttemptLeaseToken] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_ExecutionDispatchInbox_ExecutionStepAttempts_ActiveAttemptId",
                table: "ExecutionDispatchInbox",
                column: "ActiveAttemptId",
                principalTable: "ExecutionStepAttempts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExecutionDispatchInbox_ExecutionStepAttempts_ActiveAttemptId",
                table: "ExecutionDispatchInbox");

            migrationBuilder.DropIndex(
                name: "IX_ExecutionDispatchInbox_ActiveAttemptId",
                table: "ExecutionDispatchInbox");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ExecutionDispatchInbox_ActiveAttempt",
                table: "ExecutionDispatchInbox");

            migrationBuilder.DropColumn(
                name: "ActiveAttemptId",
                table: "ExecutionDispatchInbox");

            migrationBuilder.DropColumn(
                name: "ActiveAttemptLeaseToken",
                table: "ExecutionDispatchInbox");
        }
    }
}
