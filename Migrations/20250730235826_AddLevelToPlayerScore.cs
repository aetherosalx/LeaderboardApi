using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderboardApi.Migrations
{
    /// <inheritdoc />
    public partial class AddLevelToPlayerScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Level",
                table: "PlayerScores",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Level",
                table: "PlayerScores");
        }
    }
}
