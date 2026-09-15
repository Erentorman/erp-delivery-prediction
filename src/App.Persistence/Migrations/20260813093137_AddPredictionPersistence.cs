using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace App.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPredictionPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PredictionResults",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    erp_order_ref = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_simulation = table.Column<bool>(type: "boolean", nullable: false),
                    simulation_input_summary = table.Column<string>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    data_sufficiency_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    final_working_lead_time_minutes = table.Column<long>(type: "bigint", nullable: true),
                    production_start = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    production_end = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    ship_date = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    delivery_date = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    requested_delivery_date = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    critical_path_summary = table.Column<string>(type: "jsonb", nullable: true),
                    calculated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    actual_production_start = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    actual_production_end = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    actual_shipping_date = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    actual_delivery_date = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    actual_total_working_lead_time_minutes = table.Column<long>(type: "bigint", nullable: true),
                    delivered_late = table.Column<bool>(type: "boolean", nullable: true),
                    created_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredictionResults", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "PredictionProviderResults",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    prediction_result_id = table.Column<long>(type: "bigint", nullable: false),
                    provider_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    provider_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    working_lead_time_minutes = table.Column<long>(type: "bigint", nullable: true),
                    estimated_delivery_date = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    model_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    feature_schema_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    training_dataset_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    feature_payload = table.Column<string>(type: "jsonb", nullable: true),
                    warnings = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredictionProviderResults", x => x.id);
                    table.ForeignKey(
                        name: "FK_PredictionProviderResults_PredictionResults_prediction_resu~",
                        column: x => x.prediction_result_id,
                        principalTable: "PredictionResults",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PredictionProviderResults_prediction_result_id_provider_type",
                table: "PredictionProviderResults",
                columns: new[] { "prediction_result_id", "provider_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PredictionProviderResults_provider_type",
                table: "PredictionProviderResults",
                column: "provider_type");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionResults_calculated_at",
                table: "PredictionResults",
                column: "calculated_at");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionResults_erp_order_ref",
                table: "PredictionResults",
                column: "erp_order_ref");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionResults_is_simulation",
                table: "PredictionResults",
                column: "is_simulation");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PredictionProviderResults");

            migrationBuilder.DropTable(
                name: "PredictionResults");
        }
    }
}
