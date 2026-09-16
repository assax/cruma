using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cruma.Desktop.Storage.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "local_versions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NoteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Document = table.Column<string>(type: "TEXT", nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_local_versions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServerVersion = table.Column<long>(type: "INTEGER", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TagIds = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: true),
                    IsPinned = table.Column<bool>(type: "INTEGER", nullable: false),
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    Document = table.Column<string>(type: "TEXT", nullable: false),
                    HasConflict = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pending_changes",
                columns: table => new
                {
                    Sequence = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChangeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", nullable: false),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Operation = table.Column<string>(type: "TEXT", nullable: false),
                    ChangedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    SentAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastErrorCode = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pending_changes", x => x.Sequence);
                });

            migrationBuilder.CreateTable(
                name: "sync_state",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Cursor = table.Column<long>(type: "INTEGER", nullable: false),
                    LastSyncedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_state", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_local_versions_NoteId_UpdatedAtUtc",
                table: "local_versions",
                columns: new[] { "NoteId", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_notes_CategoryId",
                table: "notes",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_notes_State_UpdatedAtUtc",
                table: "notes",
                columns: new[] { "State", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_pending_changes_EntityType_EntityId",
                table: "pending_changes",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_pending_changes_ChangeId",
                table: "pending_changes",
                column: "ChangeId",
                unique: true);
        
            // Index vyhledávání FTS5 (search-pattern.md §4): tokenizer ascii jen dělí tokeny z Cruma.Search, sám nenormalizuje.
            migrationBuilder.Sql("""
                CREATE VIRTUAL TABLE search_index USING fts5(
                    entity_type UNINDEXED,
                    entity_id UNINDEXED,
                    state UNINDEXED,
                    normalizer_version UNINDEXED,
                    tokens,
                    tokenize = 'ascii'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS search_index;");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "local_versions");

            migrationBuilder.DropTable(
                name: "notes");

            migrationBuilder.DropTable(
                name: "pending_changes");

            migrationBuilder.DropTable(
                name: "sync_state");

            migrationBuilder.DropTable(
                name: "tags");
        }
    }
}
