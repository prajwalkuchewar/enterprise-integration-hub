using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseIntegrationHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncWorkflowModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Connectors_ExternalSystemId",
                table: "Connectors",
                column: "ExternalSystemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Connectors_ExternalSystems_ExternalSystemId",
                table: "Connectors",
                column: "ExternalSystemId",
                principalTable: "ExternalSystems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Connectors_ExternalSystems_ExternalSystemId",
                table: "Connectors");

            migrationBuilder.DropIndex(
                name: "IX_Connectors_ExternalSystemId",
                table: "Connectors");
        }
    }
}
