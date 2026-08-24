using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace App.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class T910AddPredictionResults : Migration
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
                    erp_order_ref = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    final_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    fallback_reason = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    combination_strategy = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    rule_based_weight = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    ai_weight = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    final_working_lead_time_minutes = table.Column<long>(type: "bigint", nullable: true),
                    absolute_difference_minutes = table.Column<long>(type: "bigint", nullable: true),
                    relative_difference_percent = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    production_start = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    production_end = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    delivery_date = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    calculated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
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
                    estimated_delivery_date = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    model_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    feature_schema_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    training_dataset_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    feature_payload = table.Column<string>(type: "jsonb", nullable: true),
                    warnings = table.Column<string>(type: "jsonb", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
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
                name: "IX_PredictionResults_final_status",
                table: "PredictionResults",
                column: "final_status");
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
