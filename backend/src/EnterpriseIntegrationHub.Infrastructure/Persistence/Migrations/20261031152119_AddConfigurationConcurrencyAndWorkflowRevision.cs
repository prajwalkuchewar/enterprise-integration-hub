using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseIntegrationHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurationConcurrencyAndWorkflowRevision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConfigurationRevision",
                table: "Workflows",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Workflows",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ExternalSystems",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Connectors",
                type: "rowversion",
                rowVersion: true,
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfigurationRevision",
                table: "Workflows");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Workflows");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ExternalSystems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Connectors");
        }
    }
}
