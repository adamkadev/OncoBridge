using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OncoBridge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialProvenanceSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "import_batch",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_system_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    received_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    raw_payload = table.Column<byte[]>(type: "bytea", nullable: false),
                    content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    bundle_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    entry_count = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    normalizer_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_batch", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "source_resource",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_index = table.Column<int>(type: "integer", nullable: false),
                    resource_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    resource_json = table.Column<string>(type: "jsonb", nullable: true),
                    source_logical_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    full_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_source_resource", x => x.id);
                    table.ForeignKey(
                        name: "fk_source_resource_batch",
                        column: x => x.batch_id,
                        principalTable: "import_batch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lineage",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    domain_entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    domain_entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    source_resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transformation_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    transformation_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lineage", x => x.id);
                    table.ForeignKey(
                        name: "fk_lineage_source_resource",
                        column: x => x.source_resource_id,
                        principalTable: "source_resource",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_import_batch_content_hash",
                table: "import_batch",
                column: "content_hash");

            migrationBuilder.CreateIndex(
                name: "ix_lineage_domain_entity",
                table: "lineage",
                columns: new[] { "domain_entity_type", "domain_entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_lineage_source_resource_id",
                table: "lineage",
                column: "source_resource_id");

            migrationBuilder.CreateIndex(
                name: "ix_source_resource_resource_type",
                table: "source_resource",
                column: "resource_type");

            migrationBuilder.CreateIndex(
                name: "ux_source_resource_batch_entry_index",
                table: "source_resource",
                columns: new[] { "batch_id", "entry_index" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lineage");

            migrationBuilder.DropTable(
                name: "source_resource");

            migrationBuilder.DropTable(
                name: "import_batch");
        }
    }
}
