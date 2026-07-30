using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace App.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class T306AddIntegrationAndAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IntegrationLogs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    integration_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    operation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    external_resource = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    is_success = table.Column<bool>(type: "boolean", nullable: false),
                    status_code = table.Column<int>(type: "integer", nullable: true),
                    duration_ms = table.Column<long>(type: "bigint", nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationLogs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    integration_log_id = table.Column<long>(type: "bigint", nullable: false),
                    integration_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    operation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_success = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_IntegrationLogs_integration_log_id",
                        column: x => x.integration_log_id,
                        principalTable: "IntegrationLogs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_created_at",
                table: "AuditLogs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_integration_log_id",
                table: "AuditLogs",
                column: "integration_log_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationLogs_created_at",
                table: "IntegrationLogs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationLogs_integration_type",
                table: "IntegrationLogs",
                column: "integration_type");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationLogs_integration_type_operation",
                table: "IntegrationLogs",
                columns: new[] { "integration_type", "operation" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "IntegrationLogs");
        }
    }
}
