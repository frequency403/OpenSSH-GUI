using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenSSH_GUI.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KeyDtos",
                columns: table => new
                {
                    AbsolutePath = table.Column<string>(type: "TEXT", nullable: false),
                    Format = table.Column<int>(type: "INTEGER", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyDtos", x => x.AbsolutePath);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    ConvertPpkAutomatically = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxSavedServers = table.Column<int>(type: "INTEGER", nullable: false),
                    LastUsedServers = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConnectionCredentials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Hostname = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    AuthType = table.Column<int>(type: "INTEGER", nullable: false),
                    SettingsFileId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConnectionCredentials_Settings_SettingsFileId",
                        column: x => x.SettingsFileId,
                        principalTable: "Settings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionCredentials_SettingsFileId",
                table: "ConnectionCredentials",
                column: "SettingsFileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConnectionCredentials");

            migrationBuilder.DropTable(
                name: "KeyDtos");

            migrationBuilder.DropTable(
                name: "Settings");
        }
    }
}
