using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Castara.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "composition_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Carbon = table.Column<double>(type: "REAL", nullable: false),
                    Silicon = table.Column<double>(type: "REAL", nullable: false),
                    Manganese = table.Column<double>(type: "REAL", nullable: false),
                    Phosphorus = table.Column<double>(type: "REAL", nullable: false),
                    Sulfur = table.Column<double>(type: "REAL", nullable: false),
                    Chromium = table.Column<double>(type: "REAL", nullable: false),
                    Nickel = table.Column<double>(type: "REAL", nullable: false),
                    Molybdenum = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_composition_profiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_composition_profiles_Name",
                table: "composition_profiles",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "composition_profiles");
        }
    }
}
