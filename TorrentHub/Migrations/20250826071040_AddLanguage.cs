using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TorrentHub.Migrations
{
    /// <inheritdoc />
    public partial class AddLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:badge_code", "early_supporter,torrent_master,community_contributor,coin_collector")
                .Annotation("Npgsql:Enum:forum_category_code", "announcement,general,feedback,invite,watering")
                .Annotation("Npgsql:Enum:report_reason", "illegal_content,misleading_category,low_quality,duplicate,dead_torrent,other")
                .Annotation("Npgsql:Enum:request_status", "pending,filled,pending_confirmation,rejected")
                .Annotation("Npgsql:Enum:store_item_code", "upload_credit10gb,upload_credit50gb,invite_one,invite_five,double_upload,no_hit_and_run,badge")
                .Annotation("Npgsql:Enum:torrent_category", "movie,documentary,series,animation,game,music,variety,sports,concert,other")
                .Annotation("Npgsql:Enum:torrent_sticky_status", "none,official_sticky,user_sticky")
                .Annotation("Npgsql:Enum:user_ban_reason", "cheat,low_ratio,self_ban_request,other")
                .Annotation("Npgsql:Enum:user_role", "mosquito,user,power_user,elite_user,crazy_user,veteran_user,vip,uploader,seeder,moderator,administrator")
                .OldAnnotation("Npgsql:Enum:badge_code", "early_supporter,torrent_master,community_contributor,coin_collector")
                .OldAnnotation("Npgsql:Enum:forum_category_code", "announcement,general,feedback,invite,watering")
                .OldAnnotation("Npgsql:Enum:report_reason", "illegal_content,misleading_category,low_quality,duplicate,dead_torrent,other")
                .OldAnnotation("Npgsql:Enum:request_status", "pending,filled")
                .OldAnnotation("Npgsql:Enum:store_item_code", "upload_credit10gb,upload_credit50gb,invite_one,invite_five,double_upload,no_hit_and_run,badge")
                .OldAnnotation("Npgsql:Enum:torrent_category", "movie,documentary,series,animation,game,music,variety,sports,concert,other")
                .OldAnnotation("Npgsql:Enum:torrent_sticky_status", "none,official_sticky,user_sticky")
                .OldAnnotation("Npgsql:Enum:user_ban_reason", "cheat,low_ratio,self_ban_request,other")
                .OldAnnotation("Npgsql:Enum:user_role", "mosquito,user,power_user,elite_user,crazy_user,veteran_user,vip,uploader,seeder,moderator,administrator");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConfirmationDeadline",
                table: "Requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Requests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "ForumPosts",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "Comments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Announcements",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmationDeadline",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Requests");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:badge_code", "early_supporter,torrent_master,community_contributor,coin_collector")
                .Annotation("Npgsql:Enum:forum_category_code", "announcement,general,feedback,invite,watering")
                .Annotation("Npgsql:Enum:report_reason", "illegal_content,misleading_category,low_quality,duplicate,dead_torrent,other")
                .Annotation("Npgsql:Enum:request_status", "pending,filled")
                .Annotation("Npgsql:Enum:store_item_code", "upload_credit10gb,upload_credit50gb,invite_one,invite_five,double_upload,no_hit_and_run,badge")
                .Annotation("Npgsql:Enum:torrent_category", "movie,documentary,series,animation,game,music,variety,sports,concert,other")
                .Annotation("Npgsql:Enum:torrent_sticky_status", "none,official_sticky,user_sticky")
                .Annotation("Npgsql:Enum:user_ban_reason", "cheat,low_ratio,self_ban_request,other")
                .Annotation("Npgsql:Enum:user_role", "mosquito,user,power_user,elite_user,crazy_user,veteran_user,vip,uploader,seeder,moderator,administrator")
                .OldAnnotation("Npgsql:Enum:badge_code", "early_supporter,torrent_master,community_contributor,coin_collector")
                .OldAnnotation("Npgsql:Enum:forum_category_code", "announcement,general,feedback,invite,watering")
                .OldAnnotation("Npgsql:Enum:report_reason", "illegal_content,misleading_category,low_quality,duplicate,dead_torrent,other")
                .OldAnnotation("Npgsql:Enum:request_status", "pending,filled,pending_confirmation,rejected")
                .OldAnnotation("Npgsql:Enum:store_item_code", "upload_credit10gb,upload_credit50gb,invite_one,invite_five,double_upload,no_hit_and_run,badge")
                .OldAnnotation("Npgsql:Enum:torrent_category", "movie,documentary,series,animation,game,music,variety,sports,concert,other")
                .OldAnnotation("Npgsql:Enum:torrent_sticky_status", "none,official_sticky,user_sticky")
                .OldAnnotation("Npgsql:Enum:user_ban_reason", "cheat,low_ratio,self_ban_request,other")
                .OldAnnotation("Npgsql:Enum:user_role", "mosquito,user,power_user,elite_user,crazy_user,veteran_user,vip,uploader,seeder,moderator,administrator");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "ForumPosts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "Text",
                table: "Comments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Announcements",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);
        }
    }
}
