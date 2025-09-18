using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChemGa.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "giveaway",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    MessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    HostId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Prize = table.Column<string>(type: "text", nullable: false),
                    StartAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WinnerCount = table.Column<int>(type: "integer", nullable: false),
                    ParticipantIds = table.Column<decimal[]>(type: "numeric(20,0)[]", nullable: false),
                    WinnerIds = table.Column<decimal[]>(type: "numeric(20,0)[]", nullable: false),
                    IsEnded = table.Column<bool>(type: "boolean", nullable: false),
                    RoleRestrictions = table.Column<decimal[]>(type: "numeric(20,0)[]", nullable: false),
                    RoleRequirements = table.Column<decimal[]>(type: "numeric(20,0)[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_giveaway", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_giveaway_MessageId",
                table: "giveaway",
                column: "MessageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "giveaway");
        }
    }
}
