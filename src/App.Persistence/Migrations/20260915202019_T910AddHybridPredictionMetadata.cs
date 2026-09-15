using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class T910AddHybridPredictionMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "absolute_difference_minutes",
                table: "PredictionResults",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ai_weight",
                table: "PredictionResults",
                type: "numeric(4,2)",
                precision: 4,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "combination_strategy",
                table: "PredictionResults",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fallback_reason",
                table: "PredictionResults",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "final_status",
                table: "PredictionResults",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "relative_difference_percent",
                table: "PredictionResults",
                type: "numeric(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "rule_based_weight",
                table: "PredictionResults",
                type: "numeric(4,2)",
                precision: 4,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "duration_ms",
                table: "PredictionProviderResults",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PredictionResults_final_status",
                table: "PredictionResults",
                column: "final_status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PredictionResults_final_status",
                table: "PredictionResults");

            migrationBuilder.DropColumn(
                name: "absolute_difference_minutes",
                table: "PredictionResults");

            migrationBuilder.DropColumn(
                name: "ai_weight",
                table: "PredictionResults");

            migrationBuilder.DropColumn(
                name: "combination_strategy",
                table: "PredictionResults");

            migrationBuilder.DropColumn(
                name: "fallback_reason",
                table: "PredictionResults");

            migrationBuilder.DropColumn(
                name: "final_status",
                table: "PredictionResults");

            migrationBuilder.DropColumn(
                name: "relative_difference_percent",
                table: "PredictionResults");

            migrationBuilder.DropColumn(
                name: "rule_based_weight",
                table: "PredictionResults");

            migrationBuilder.DropColumn(
                name: "duration_ms",
                table: "PredictionProviderResults");
        }
    }
}
