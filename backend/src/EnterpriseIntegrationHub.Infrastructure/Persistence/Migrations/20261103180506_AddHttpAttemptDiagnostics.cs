using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseIntegrationHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHttpAttemptDiagnostics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DurationMilliseconds",
                table: "ExecutionStepAttempts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HttpStatusCode",
                table: "ExecutionStepAttempts",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DurationMilliseconds",
                table: "ExecutionStepAttempts");

            migrationBuilder.DropColumn(
                name: "HttpStatusCode",
                table: "ExecutionStepAttempts");
        }
    }
}
