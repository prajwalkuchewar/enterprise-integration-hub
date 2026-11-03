using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseIntegrationHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConnectorIngestionKeyHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IngestionKeyHash",
                table: "Connectors",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Connectors_IngestionKeyHash",
                table: "Connectors",
                column: "IngestionKeyHash",
                unique: true,
                filter: "[IngestionKeyHash] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Connectors_IngestionKeyHash",
                table: "Connectors");

            migrationBuilder.DropColumn(
                name: "IngestionKeyHash",
                table: "Connectors");
        }
    }
}
