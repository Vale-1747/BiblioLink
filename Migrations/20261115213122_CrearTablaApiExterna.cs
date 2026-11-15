using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiblioLink.Migrations
{
    /// <inheritdoc />
    public partial class CrearTablaApiExterna : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiExternas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NombreSistema = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UrlBase = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    ApiKey = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiExternas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiExternas_NombreSistema",
                table: "ApiExternas",
                column: "NombreSistema",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiExternas");
        }
    }
}
