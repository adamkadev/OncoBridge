using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OncoBridge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FindingPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "finding",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    check_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    citation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    expected = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    actual = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_domain_entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_finding", x => x.id);
                    table.CheckConstraint("ck_finding_target_shape", "(target_kind = 'SourceResource' AND target_domain_entity_type IS NULL)\nOR (target_kind = 'DomainEntity' AND target_domain_entity_type IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_finding_batch",
                        column: x => x.batch_id,
                        principalTable: "import_batch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_finding_batch_category",
                table: "finding",
                columns: new[] { "batch_id", "category" });

            migrationBuilder.CreateIndex(
                name: "ix_finding_batch_check_id",
                table: "finding",
                columns: new[] { "batch_id", "check_id" });

            migrationBuilder.CreateIndex(
                name: "ix_finding_batch_id",
                table: "finding",
                column: "batch_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "finding");
        }
    }
}
