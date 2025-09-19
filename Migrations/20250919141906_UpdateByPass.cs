using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChemGa.Migrations
{
    /// <inheritdoc />
    public partial class UpdateByPass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_globalbypass_UserId",
                table: "globalbypass",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_globalbypass_UserId",
                table: "globalbypass");
        }
    }
}
