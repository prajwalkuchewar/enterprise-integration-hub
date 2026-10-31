using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseIntegrationHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowRevisionsAndExecutionFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionNumber = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, collation: "SQL_Latin1_General_CP1_CI_AS"),
                    SourceConnectorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TriggerEvent = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowRevisions_Connectors_SourceConnectorId",
                        column: x => x.SourceConnectorId,
                        principalTable: "Connectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowRevisions_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowRevisionNumber = table.Column<int>(type: "int", nullable: false),
                    SourceConnectorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceEventId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, collation: "SQL_Latin1_General_CP1_CS_AS"),
                    TriggerEvent = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EventFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationExecutions_Connectors_SourceConnectorId",
                        column: x => x.SourceConnectorId,
                        principalTable: "Connectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntegrationExecutions_WorkflowRevisions_WorkflowRevisionId",
                        column: x => x.WorkflowRevisionId,
                        principalTable: "WorkflowRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowRevisionSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationConnectorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowRevisionSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowRevisionSteps_Connectors_DestinationConnectorId",
                        column: x => x.DestinationConnectorId,
                        principalTable: "Connectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowRevisionSteps_WorkflowRevisions_WorkflowRevisionId",
                        column: x => x.WorkflowRevisionId,
                        principalTable: "WorkflowRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionDispatchOutbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DispatchedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionDispatchOutbox", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionDispatchOutbox_IntegrationExecutions_IntegrationExecutionId",
                        column: x => x.IntegrationExecutionId,
                        principalTable: "IntegrationExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowRevisionStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationConnectorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationBaseUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Protocol = table.Column<int>(type: "int", nullable: false),
                    AuthenticationType = table.Column<int>(type: "int", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "int", nullable: false),
                    ExecutionOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionSteps_Connectors_DestinationConnectorId",
                        column: x => x.DestinationConnectorId,
                        principalTable: "Connectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExecutionSteps_IntegrationExecutions_IntegrationExecutionId",
                        column: x => x.IntegrationExecutionId,
                        principalTable: "IntegrationExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExecutionSteps_WorkflowRevisionSteps_WorkflowRevisionStepId",
                        column: x => x.WorkflowRevisionStepId,
                        principalTable: "WorkflowRevisionSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionStepAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FailureCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FailureMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionStepAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionStepAttempts_ExecutionSteps_ExecutionStepId",
                        column: x => x.ExecutionStepId,
                        principalTable: "ExecutionSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionDispatchOutbox_ExecutionId",
                table: "ExecutionDispatchOutbox",
                column: "IntegrationExecutionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionStepAttempts_StepId_AttemptNumber",
                table: "ExecutionStepAttempts",
                columns: new[] { "ExecutionStepId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionSteps_DestinationConnectorId",
                table: "ExecutionSteps",
                column: "DestinationConnectorId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionSteps_ExecutionId_ExecutionOrder",
                table: "ExecutionSteps",
                columns: new[] { "IntegrationExecutionId", "ExecutionOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionSteps_WorkflowRevisionStepId",
                table: "ExecutionSteps",
                column: "WorkflowRevisionStepId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationExecutions_SourceConnectorId_SourceEventId",
                table: "IntegrationExecutions",
                columns: new[] { "SourceConnectorId", "SourceEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationExecutions_WorkflowRevisionId",
                table: "IntegrationExecutions",
                column: "WorkflowRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRevisions_SourceConnectorId",
                table: "WorkflowRevisions",
                column: "SourceConnectorId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRevisions_WorkflowId_RevisionNumber",
                table: "WorkflowRevisions",
                columns: new[] { "WorkflowId", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRevisionSteps_DestinationConnectorId",
                table: "WorkflowRevisionSteps",
                column: "DestinationConnectorId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRevisionSteps_RevisionId_ExecutionOrder",
                table: "WorkflowRevisionSteps",
                columns: new[] { "WorkflowRevisionId", "ExecutionOrder" },
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO [WorkflowRevisions]
                    ([Id], [WorkflowId], [RevisionNumber], [Name], [SourceConnectorId], [TriggerEvent], [PublishedAt], [CreatedAt], [UpdatedAt])
                SELECT NEWID(), [Id], [ConfigurationRevision], [Name], [SourceConnectorId], [TriggerEvent],
                    COALESCE([UpdatedAt], [CreatedAt]), [CreatedAt], [UpdatedAt]
                FROM [Workflows]
                WHERE [Status] = 1;

                INSERT INTO [WorkflowRevisionSteps]
                    ([Id], [WorkflowRevisionId], [DestinationConnectorId], [ExecutionOrder])
                SELECT NEWID(), revision.[Id], step.[DestinationConnectorId], step.[ExecutionOrder]
                FROM [WorkflowSteps] step
                INNER JOIN [WorkflowRevisions] revision
                    ON revision.[WorkflowId] = step.[WorkflowId]
                INNER JOIN [Workflows] workflow
                    ON workflow.[Id] = revision.[WorkflowId]
                    AND workflow.[ConfigurationRevision] = revision.[RevisionNumber];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExecutionDispatchOutbox");

            migrationBuilder.DropTable(
                name: "ExecutionStepAttempts");

            migrationBuilder.DropTable(
                name: "ExecutionSteps");

            migrationBuilder.DropTable(
                name: "IntegrationExecutions");

            migrationBuilder.DropTable(
                name: "WorkflowRevisionSteps");

            migrationBuilder.DropTable(
                name: "WorkflowRevisions");
        }
    }
}
