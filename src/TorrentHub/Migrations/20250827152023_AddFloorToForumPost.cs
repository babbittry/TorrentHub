using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TorrentHub.Migrations
{
    /// <inheritdoc />
    public partial class AddFloorToForumPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Floor",
                table: "ForumPosts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Floor",
                table: "ForumPosts");
        }
    }
}
