using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DealFinder.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixAiCacheStoresAnalysisOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Summary",
                table: "AiEvaluationCaches",
                newName: "RiskLabel");

            migrationBuilder.RenameColumn(
                name: "Score",
                table: "AiEvaluationCaches",
                newName: "SellerTrustScore");

            migrationBuilder.AddColumn<string>(
                name: "AnalysisSummary",
                table: "AiEvaluationCaches",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "ConditionScore",
                table: "AiEvaluationCaches",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "DetectedIssuesJson",
                table: "AiEvaluationCaches",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PositiveSignalsJson",
                table: "AiEvaluationCaches",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "RiskScore",
                table: "AiEvaluationCaches",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnalysisSummary",
                table: "AiEvaluationCaches");

            migrationBuilder.DropColumn(
                name: "ConditionScore",
                table: "AiEvaluationCaches");

            migrationBuilder.DropColumn(
                name: "DetectedIssuesJson",
                table: "AiEvaluationCaches");

            migrationBuilder.DropColumn(
                name: "PositiveSignalsJson",
                table: "AiEvaluationCaches");

            migrationBuilder.DropColumn(
                name: "RiskScore",
                table: "AiEvaluationCaches");

            migrationBuilder.RenameColumn(
                name: "SellerTrustScore",
                table: "AiEvaluationCaches",
                newName: "Score");

            migrationBuilder.RenameColumn(
                name: "RiskLabel",
                table: "AiEvaluationCaches",
                newName: "Summary");
        }
    }
}
