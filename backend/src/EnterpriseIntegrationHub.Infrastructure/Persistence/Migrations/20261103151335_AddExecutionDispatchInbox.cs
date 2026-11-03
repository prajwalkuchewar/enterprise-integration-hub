using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseIntegrationHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExecutionDispatchInbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExecutionDispatchInbox",
                columns: table => new
                {
                    DispatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionDispatchInbox", x => x.DispatchId);
                    table.ForeignKey(
                        name: "FK_ExecutionDispatchInbox_ExecutionDispatchOutbox_DispatchId",
                        column: x => x.DispatchId,
                        principalTable: "ExecutionDispatchOutbox",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExecutionDispatchInbox_IntegrationExecutions_IntegrationExecutionId",
                        column: x => x.IntegrationExecutionId,
                        principalTable: "IntegrationExecutions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionDispatchInbox_IntegrationExecutionId",
                table: "ExecutionDispatchInbox",
                column: "IntegrationExecutionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExecutionDispatchInbox");
        }
    }
}
