using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Cruma.Server.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "notes");

            migrationBuilder.EnsureSchema(
                name: "sync");

            migrationBuilder.EnsureSchema(
                name: "search");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.CreateTable(
                name: "audit_events",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    operation_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    object_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    object_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    client_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    result = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    attributes = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                schema: "notes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "change_feed",
                schema: "sync",
                columns: table => new
                {
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<long>(type: "bigint", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: true),
                    deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_change_feed", x => new { x.owner_user_id, x.sequence });
                });

            migrationBuilder.CreateTable(
                name: "change_feed_counters",
                schema: "sync",
                columns: table => new
                {
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_sequence = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_change_feed_counters", x => x.owner_user_id);
                });

            migrationBuilder.CreateTable(
                name: "index_entries",
                schema: "search",
                columns: table => new
                {
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tokens = table.Column<string>(type: "text", nullable: false),
                    normalizer_version = table.Column<int>(type: "integer", nullable: false),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: false)
                        .Annotation("Npgsql:TsVectorConfig", "simple")
                        .Annotation("Npgsql:TsVectorProperties", new[] { "tokens" }),
                    indexed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_index_entries", x => new { x.owner_user_id, x.entity_type, x.entity_id });
                });

            migrationBuilder.CreateTable(
                name: "notes",
                schema: "notes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_version = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    is_pinned = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    document = table.Column<string>(type: "jsonb", nullable: false),
                    has_conflict = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "processed_changes",
                schema: "sync",
                columns: table => new
                {
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    change_id = table.Column<Guid>(type: "uuid", nullable: false),
                    result_json = table.Column<string>(type: "jsonb", nullable: false),
                    processed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_processed_changes", x => new { x.owner_user_id, x.change_id });
                });

            migrationBuilder.CreateTable(
                name: "tags",
                schema: "notes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_sign_in_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "note_versions",
                schema: "notes",
                columns: table => new
                {
                    note_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<long>(type: "bigint", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    base_number = table.Column<long>(type: "bigint", nullable: true),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    client_instance_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    document = table.Column<string>(type: "jsonb", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: false),
                    overwritten_values = table.Column<string>(type: "jsonb", nullable: false),
                    notices = table.Column<string>(type: "jsonb", nullable: false),
                    has_conflict = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_note_versions", x => new { x.note_id, x.number });
                    table.ForeignKey(
                        name: "fk_note_versions_notes_note_id",
                        column: x => x.note_id,
                        principalSchema: "notes",
                        principalTable: "notes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "linked_identities",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    linked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_linked_identities", x => x.id);
                    table.ForeignKey(
                        name: "fk_linked_identities_user_entity_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_occurred_at_utc",
                schema: "audit",
                table: "audit_events",
                column: "occurred_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_operation_type_occurred_at_utc",
                schema: "audit",
                table: "audit_events",
                columns: new[] { "operation_type", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_user_id_occurred_at_utc",
                schema: "audit",
                table: "audit_events",
                columns: new[] { "user_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_categories_owner_user_id",
                schema: "notes",
                table: "categories",
                column: "owner_user_id",
                unique: true,
                filter: "is_default");

            migrationBuilder.CreateIndex(
                name: "ix_index_entries_normalizer_version",
                schema: "search",
                table: "index_entries",
                column: "normalizer_version");

            migrationBuilder.CreateIndex(
                name: "ix_index_entries_search_vector",
                schema: "search",
                table: "index_entries",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "ix_linked_identities_provider_subject",
                schema: "identity",
                table: "linked_identities",
                columns: new[] { "provider", "subject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_linked_identities_user_id",
                schema: "identity",
                table: "linked_identities",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_notes_owner_user_id_category_id",
                schema: "notes",
                table: "notes",
                columns: new[] { "owner_user_id", "category_id" });

            migrationBuilder.CreateIndex(
                name: "ix_notes_owner_user_id_state_updated_at_utc",
                schema: "notes",
                table: "notes",
                columns: new[] { "owner_user_id", "state", "updated_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_tags_owner_user_id",
                schema: "notes",
                table: "tags",
                column: "owner_user_id");

            // Auditní záznamy jsou append-only i na úrovni databáze (AUD-004).
            migrationBuilder.Sql("""
                CREATE FUNCTION audit.reject_change() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'audit records are append-only';
                END;
                $$;
                CREATE TRIGGER audit_events_append_only BEFORE UPDATE OR DELETE ON audit.audit_events
                    FOR EACH ROW EXECUTE FUNCTION audit.reject_change();
                CREATE TRIGGER audit_events_no_truncate BEFORE TRUNCATE ON audit.audit_events
                    FOR EACH STATEMENT EXECUTE FUNCTION audit.reject_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS audit_events_no_truncate ON audit.audit_events;
                DROP TRIGGER IF EXISTS audit_events_append_only ON audit.audit_events;
                DROP FUNCTION IF EXISTS audit.reject_change();
                """);

            migrationBuilder.DropTable(
                name: "audit_events",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "categories",
                schema: "notes");

            migrationBuilder.DropTable(
                name: "change_feed",
                schema: "sync");

            migrationBuilder.DropTable(
                name: "change_feed_counters",
                schema: "sync");

            migrationBuilder.DropTable(
                name: "index_entries",
                schema: "search");

            migrationBuilder.DropTable(
                name: "linked_identities",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "note_versions",
                schema: "notes");

            migrationBuilder.DropTable(
                name: "processed_changes",
                schema: "sync");

            migrationBuilder.DropTable(
                name: "tags",
                schema: "notes");

            migrationBuilder.DropTable(
                name: "users",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "notes",
                schema: "notes");
        }
    }
}
