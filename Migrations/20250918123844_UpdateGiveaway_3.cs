using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChemGa.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGiveaway_3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Prize",
                table: "giveaway",
                newName: "prize");

            migrationBuilder.RenameColumn(
                name: "WinnerIds",
                table: "giveaway",
                newName: "winners");

            migrationBuilder.RenameColumn(
                name: "WinnerCount",
                table: "giveaway",
                newName: "winner_cnt");

            migrationBuilder.RenameColumn(
                name: "StartAt",
                table: "giveaway",
                newName: "start_at");

            migrationBuilder.RenameColumn(
                name: "RoleRestrictions",
                table: "giveaway",
                newName: "roles_restrict");

            migrationBuilder.RenameColumn(
                name: "RoleRequirements",
                table: "giveaway",
                newName: "roles_required");

            migrationBuilder.RenameColumn(
                name: "ParticipantIds",
                table: "giveaway",
                newName: "participants");

            migrationBuilder.RenameColumn(
                name: "MessageId",
                table: "giveaway",
                newName: "message_id");

            migrationBuilder.RenameColumn(
                name: "IsEnded",
                table: "giveaway",
                newName: "ended");

            migrationBuilder.RenameColumn(
                name: "HostId",
                table: "giveaway",
                newName: "host_id");

            migrationBuilder.RenameColumn(
                name: "GuildId",
                table: "giveaway",
                newName: "guild_id");

            migrationBuilder.RenameColumn(
                name: "EndAt",
                table: "giveaway",
                newName: "end_at");

            migrationBuilder.RenameColumn(
                name: "ChannelId",
                table: "giveaway",
                newName: "channel_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "giveaway",
                newName: "_id");

            migrationBuilder.RenameIndex(
                name: "IX_giveaway_MessageId",
                table: "giveaway",
                newName: "IX_giveaway_message_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "prize",
                table: "giveaway",
                newName: "Prize");

            migrationBuilder.RenameColumn(
                name: "winners",
                table: "giveaway",
                newName: "WinnerIds");

            migrationBuilder.RenameColumn(
                name: "winner_cnt",
                table: "giveaway",
                newName: "WinnerCount");

            migrationBuilder.RenameColumn(
                name: "start_at",
                table: "giveaway",
                newName: "StartAt");

            migrationBuilder.RenameColumn(
                name: "roles_restrict",
                table: "giveaway",
                newName: "RoleRestrictions");

            migrationBuilder.RenameColumn(
                name: "roles_required",
                table: "giveaway",
                newName: "RoleRequirements");

            migrationBuilder.RenameColumn(
                name: "participants",
                table: "giveaway",
                newName: "ParticipantIds");

            migrationBuilder.RenameColumn(
                name: "message_id",
                table: "giveaway",
                newName: "MessageId");

            migrationBuilder.RenameColumn(
                name: "host_id",
                table: "giveaway",
                newName: "HostId");

            migrationBuilder.RenameColumn(
                name: "guild_id",
                table: "giveaway",
                newName: "GuildId");

            migrationBuilder.RenameColumn(
                name: "ended",
                table: "giveaway",
                newName: "IsEnded");

            migrationBuilder.RenameColumn(
                name: "end_at",
                table: "giveaway",
                newName: "EndAt");

            migrationBuilder.RenameColumn(
                name: "channel_id",
                table: "giveaway",
                newName: "ChannelId");

            migrationBuilder.RenameColumn(
                name: "_id",
                table: "giveaway",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_giveaway_message_id",
                table: "giveaway",
                newName: "IX_giveaway_MessageId");
        }
    }
}
