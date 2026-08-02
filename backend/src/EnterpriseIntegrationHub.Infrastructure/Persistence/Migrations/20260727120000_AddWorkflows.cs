using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseIntegrationHub.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EnterpriseIntegrationHubDbContext))]
[Migration("20260727120000_AddWorkflows")]
public partial class AddWorkflows : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Workflows",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, collation: "SQL_Latin1_General_CP1_CI_AS"),
                SourceConnectorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TriggerEvent = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Workflows", x => x.Id);
                table.ForeignKey("FK_Workflows_Connectors_SourceConnectorId", x => x.SourceConnectorId, "Connectors", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "WorkflowSteps",
            columns: table => new
            {
                WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DestinationConnectorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ExecutionOrder = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WorkflowSteps", x => new { x.WorkflowId, x.ExecutionOrder });
                table.ForeignKey("FK_WorkflowSteps_Connectors_DestinationConnectorId", x => x.DestinationConnectorId, "Connectors", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_WorkflowSteps_Workflows_WorkflowId", x => x.WorkflowId, "Workflows", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_Workflows_Name", table: "Workflows", column: "Name", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Workflows_SourceConnectorId", table: "Workflows", column: "SourceConnectorId");
        migrationBuilder.CreateIndex(name: "IX_WorkflowSteps_DestinationConnectorId", table: "WorkflowSteps", column: "DestinationConnectorId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "WorkflowSteps");
        migrationBuilder.DropTable(name: "Workflows");
    }
}
