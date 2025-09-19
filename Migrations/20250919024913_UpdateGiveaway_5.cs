using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChemGa.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGiveaway_5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_giveaway_end_at_ended_guild_id",
                table: "giveaway",
                columns: new[] { "end_at", "ended", "guild_id" },
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_giveaway_end_at_ended_guild_id",
                table: "giveaway");
        }
    }
}
