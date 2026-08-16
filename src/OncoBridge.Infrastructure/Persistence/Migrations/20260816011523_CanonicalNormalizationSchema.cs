using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OncoBridge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CanonicalNormalizationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "normalized_at",
                table: "import_batch",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "patient",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_identifier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    birth_date_day = table.Column<int>(type: "integer", nullable: true),
                    birth_date_instant = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    birth_date_month = table.Column<int>(type: "integer", nullable: true),
                    birth_date_precision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    birth_date_year = table.Column<int>(type: "integer", nullable: true),
                    sex_at_birth_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    sex_at_birth_display = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    sex_at_birth_system = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patient", x => x.id);
                    table.ForeignKey(
                        name: "fk_patient_batch",
                        column: x => x.batch_id,
                        principalTable: "import_batch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cancer_surgical_procedure",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    body_site_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    body_site_display = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    body_site_system = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    code_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    code_display = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    code_system = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    performed_kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    performed_date_day = table.Column<int>(type: "integer", nullable: true),
                    performed_date_instant = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    performed_date_month = table.Column<int>(type: "integer", nullable: true),
                    performed_date_precision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    performed_date_year = table.Column<int>(type: "integer", nullable: true),
                    performed_period_kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    performed_end_day = table.Column<int>(type: "integer", nullable: true),
                    performed_end_instant = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    performed_end_month = table.Column<int>(type: "integer", nullable: true),
                    performed_end_precision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    performed_end_year = table.Column<int>(type: "integer", nullable: true),
                    performed_start_day = table.Column<int>(type: "integer", nullable: true),
                    performed_start_instant = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    performed_start_month = table.Column<int>(type: "integer", nullable: true),
                    performed_start_precision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    performed_start_year = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cancer_surgical_procedure", x => x.id);
                    table.ForeignKey(
                        name: "fk_cancer_surgical_procedure_batch",
                        column: x => x.batch_id,
                        principalTable: "import_batch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cancer_surgical_procedure_patient",
                        column: x => x.patient_id,
                        principalTable: "patient",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "primary_cancer_diagnosis",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    body_site_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    body_site_display = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    body_site_system = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    code_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    code_display = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    code_system = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    onset_kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    onset_date_day = table.Column<int>(type: "integer", nullable: true),
                    onset_date_instant = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    onset_date_month = table.Column<int>(type: "integer", nullable: true),
                    onset_date_precision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    onset_date_year = table.Column<int>(type: "integer", nullable: true),
                    onset_period_kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    onset_end_day = table.Column<int>(type: "integer", nullable: true),
                    onset_end_instant = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    onset_end_month = table.Column<int>(type: "integer", nullable: true),
                    onset_end_precision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    onset_end_year = table.Column<int>(type: "integer", nullable: true),
                    onset_start_day = table.Column<int>(type: "integer", nullable: true),
                    onset_start_instant = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    onset_start_month = table.Column<int>(type: "integer", nullable: true),
                    onset_start_precision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    onset_start_year = table.Column<int>(type: "integer", nullable: true),
                    recorded_date_day = table.Column<int>(type: "integer", nullable: true),
                    recorded_date_instant = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    recorded_date_month = table.Column<int>(type: "integer", nullable: true),
                    recorded_date_precision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    recorded_date_year = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_primary_cancer_diagnosis", x => x.id);
                    table.ForeignKey(
                        name: "fk_primary_cancer_diagnosis_batch",
                        column: x => x.batch_id,
                        principalTable: "import_batch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_primary_cancer_diagnosis_patient",
                        column: x => x.patient_id,
                        principalTable: "patient",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cancer_staging",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    primary_cancer_diagnosis_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effective_day = table.Column<int>(type: "integer", nullable: true),
                    effective_instant = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    effective_month = table.Column<int>(type: "integer", nullable: true),
                    effective_precision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    effective_year = table.Column<int>(type: "integer", nullable: true),
                    method_kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    method_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    method_display = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    method_system = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    stage_group_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    stage_group_display = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    stage_group_system = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cancer_staging", x => x.id);
                    table.ForeignKey(
                        name: "fk_cancer_staging_batch",
                        column: x => x.batch_id,
                        principalTable: "import_batch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cancer_staging_patient",
                        column: x => x.patient_id,
                        principalTable: "patient",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cancer_staging_primary_cancer_diagnosis",
                        column: x => x.primary_cancer_diagnosis_id,
                        principalTable: "primary_cancer_diagnosis",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stage_category",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    axis = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    source_resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    staging_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    code_display = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    code_system = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stage_category", x => x.id);
                    table.ForeignKey(
                        name: "fk_stage_category_source_resource",
                        column: x => x.source_resource_id,
                        principalTable: "source_resource",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_stage_category_staging",
                        column: x => x.staging_id,
                        principalTable: "cancer_staging",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cancer_staging_batch_id",
                table: "cancer_staging",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_cancer_staging_patient_id",
                table: "cancer_staging",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_cancer_staging_primary_cancer_diagnosis_id",
                table: "cancer_staging",
                column: "primary_cancer_diagnosis_id");

            migrationBuilder.CreateIndex(
                name: "ix_cancer_surgical_procedure_batch_id",
                table: "cancer_surgical_procedure",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_cancer_surgical_procedure_patient_id",
                table: "cancer_surgical_procedure",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_patient_batch_id",
                table: "patient",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_primary_cancer_diagnosis_batch_id",
                table: "primary_cancer_diagnosis",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_primary_cancer_diagnosis_patient_id",
                table: "primary_cancer_diagnosis",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_stage_category_source_resource_id",
                table: "stage_category",
                column: "source_resource_id");

            migrationBuilder.CreateIndex(
                name: "ux_stage_category_staging_axis",
                table: "stage_category",
                columns: new[] { "staging_id", "axis" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cancer_surgical_procedure");

            migrationBuilder.DropTable(
                name: "stage_category");

            migrationBuilder.DropTable(
                name: "cancer_staging");

            migrationBuilder.DropTable(
                name: "primary_cancer_diagnosis");

            migrationBuilder.DropTable(
                name: "patient");

            migrationBuilder.DropColumn(
                name: "normalized_at",
                table: "import_batch");
        }
    }
}
