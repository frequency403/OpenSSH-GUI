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
                name: "ConnectionCredentialsDtos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Hostname = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthType = table.Column<int>(type: "INTEGER", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: true),
                    PasswordEncrypted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionCredentialsDtos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KeyDtos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AbsolutePath = table.Column<string>(type: "TEXT", nullable: false),
                    Format = table.Column<int>(type: "INTEGER", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyDtos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    ConvertPpkAutomatically = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Version);
                });

            migrationBuilder.CreateTable(
                name: "ConnectionCredentialsDtoSshKeyDto",
                columns: table => new
                {
                    ConnectionCredentialsDtoId = table.Column<int>(type: "INTEGER", nullable: false),
                    KeyDtosId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionCredentialsDtoSshKeyDto", x => new { x.ConnectionCredentialsDtoId, x.KeyDtosId });
                    table.ForeignKey(
                        name: "FK_ConnectionCredentialsDtoSshKeyDto_ConnectionCredentialsDtos_ConnectionCredentialsDtoId",
                        column: x => x.ConnectionCredentialsDtoId,
                        principalTable: "ConnectionCredentialsDtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConnectionCredentialsDtoSshKeyDto_KeyDtos_KeyDtosId",
                        column: x => x.KeyDtosId,
                        principalTable: "KeyDtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionCredentialsDtoSshKeyDto_KeyDtosId",
                table: "ConnectionCredentialsDtoSshKeyDto",
                column: "KeyDtosId");

            migrationBuilder.CreateIndex(
                name: "IX_KeyDtos_AbsolutePath",
                table: "KeyDtos",
                column: "AbsolutePath",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConnectionCredentialsDtoSshKeyDto");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "ConnectionCredentialsDtos");

            migrationBuilder.DropTable(
                name: "KeyDtos");
        }
    }
}
