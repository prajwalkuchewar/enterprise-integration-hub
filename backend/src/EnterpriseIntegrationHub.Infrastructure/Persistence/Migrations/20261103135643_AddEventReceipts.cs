using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseIntegrationHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceConnectorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceEventId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, collation: "SQL_Latin1_General_CP1_CS_AS"),
                    EventFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IntegrationExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventReceipts_Connectors_SourceConnectorId",
                        column: x => x.SourceConnectorId,
                        principalTable: "Connectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventReceipts_IntegrationExecutions_IntegrationExecutionId",
                        column: x => x.IntegrationExecutionId,
                        principalTable: "IntegrationExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventReceipts_IntegrationExecutionId",
                table: "EventReceipts",
                column: "IntegrationExecutionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventReceipts_SourceConnectorId_SourceEventId",
                table: "EventReceipts",
                columns: new[] { "SourceConnectorId", "SourceEventId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventReceipts");
        }
    }
}
