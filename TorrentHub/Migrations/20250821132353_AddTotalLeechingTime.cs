using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TorrentHub.Migrations
{
    /// <inheritdoc />
    public partial class AddTotalLeechingTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalLeechingTimeMinutes",
                table: "Users",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NominalDownloadedBytes",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "TotalLeechingTimeMinutes",
                table: "Users",
                newName: "DisplayUploadedBytes");

            migrationBuilder.RenameColumn(
                name: "NominalUploadedBytes",
                table: "Users",
                newName: "DisplayDownloadedBytes");
        }
    }
}
