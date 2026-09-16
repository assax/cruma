using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cruma.Desktop.Storage.Migrations
{
    /// <inheritdoc />
    public partial class SearchIndexRows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mapa entita → rowid FTS5: mazání a přepis řádku indexu bez průchodu celou tabulkou.
            migrationBuilder.Sql("""
                CREATE TABLE search_index_rows (
                    entity_type TEXT NOT NULL,
                    entity_id TEXT NOT NULL,
                    fts_rowid INTEGER NOT NULL,
                    PRIMARY KEY (entity_type, entity_id)
                );
                INSERT INTO search_index_rows (entity_type, entity_id, fts_rowid)
                    SELECT entity_type, entity_id, rowid FROM search_index;
                """);


        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS search_index_rows;");


        }
    }
}
