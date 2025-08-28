using Riok.Mapperly.Abstractions;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Entities;

namespace TorrentHub.Mappers;

[Mapper]
public static partial class Mapper
{
    // Note: A lot of fields are ignored here to prevent leaking sensitive data in public profiles.
    [MapperIgnoreSource(nameof(User.PasswordHash))]
    [MapperIgnoreSource(nameof(User.Email))]
    [MapperIgnoreSource(nameof(User.Language))]
    [MapperIgnoreSource(nameof(User.RssKey))]
    [MapperIgnoreSource(nameof(User.Passkey))]
    [MapperIgnoreSource(nameof(User.InviteId))]
    [MapperIgnoreSource(nameof(User.Invite))]
    [MapperIgnoreSource(nameof(User.Torrents))]
    [MapperIgnoreSource(nameof(User.GeneratedInvites))]
    [MapperIgnoreSource(nameof(User.BanStatus))]
    [MapperIgnoreSource(nameof(User.BanReason))]
    [MapperIgnoreSource(nameof(User.BanUntil))]
    [MapperIgnoreSource(nameof(User.CheatWarningCount))]
    public static partial UserPublicProfileDto ToUserPublicProfileDto(User user);

    // Private profile includes more details, but still hides sensitive info.
    [MapperIgnoreSource(nameof(User.PasswordHash))]
    [MapperIgnoreSource(nameof(User.RssKey))]
    [MapperIgnoreSource(nameof(User.Passkey))]
    [MapperIgnoreSource(nameof(User.InviteId))]
    [MapperIgnoreSource(nameof(User.Invite))]
    [MapperIgnoreSource(nameof(User.Torrents))]
    [MapperIgnoreSource(nameof(User.GeneratedInvites))]
    public static partial UserPrivateProfileDto ToUserPrivateProfileDto(User user);
    
    // Maps fields a user is allowed to change on their own profile.
    [MapProperty(nameof(UpdateUserProfileDto.AvatarUrl), nameof(User.Avatar))]
    [MapperIgnoreTarget(nameof(User.Id))]
    [MapperIgnoreTarget(nameof(User.UserName))]
    [MapperIgnoreTarget(nameof(User.Email))]
    [MapperIgnoreTarget(nameof(User.PasswordHash))]
    [MapperIgnoreTarget(nameof(User.UploadedBytes))]
    [MapperIgnoreTarget(nameof(User.DownloadedBytes))]
    [MapperIgnoreTarget(nameof(User.RssKey))]
    [MapperIgnoreTarget(nameof(User.Passkey))]
    [MapperIgnoreTarget(nameof(User.Role))]
    [MapperIgnoreTarget(nameof(User.CreatedAt))]
    [MapperIgnoreTarget(nameof(User.InviteNum))]
    [MapperIgnoreTarget(nameof(User.Coins))]
    [MapperIgnoreTarget(nameof(User.TotalSeedingTimeMinutes))]
    [MapperIgnoreTarget(nameof(User.TotalLeechingTimeMinutes))]
    [MapperIgnoreTarget(nameof(User.IsDoubleUploadActive))]
    [MapperIgnoreTarget(nameof(User.DoubleUploadExpiresAt))]
    [MapperIgnoreTarget(nameof(User.IsNoHRActive))]
    [MapperIgnoreTarget(nameof(User.NoHRExpiresAt))]
    [MapperIgnoreTarget(nameof(User.InviteId))]
    [MapperIgnoreTarget(nameof(User.Invite))]
    [MapperIgnoreTarget(nameof(User.Torrents))]
    [MapperIgnoreTarget(nameof(User.GeneratedInvites))]
    [MapperIgnoreTarget(nameof(User.BanStatus))]
    [MapperIgnoreTarget(nameof(User.BanReason))]
    [MapperIgnoreTarget(nameof(User.BanUntil))]
    [MapperIgnoreTarget(nameof(User.CheatWarningCount))]
    [MapperIgnoreTarget(nameof(User.NominalUploadedBytes))]
    [MapperIgnoreTarget(nameof(User.NominalDownloadedBytes))]
    public static partial void MapTo(UpdateUserProfileDto dto, User user);

        // Maps fields an admin is allowed to change.
    [MapperIgnoreTarget(nameof(User.Id))]
    [MapperIgnoreTarget(nameof(User.UserName))]
    [MapperIgnoreTarget(nameof(User.Email))]
    [MapperIgnoreTarget(nameof(User.PasswordHash))]
    [MapperIgnoreTarget(nameof(User.Avatar))]
    [MapperIgnoreTarget(nameof(User.Signature))]
    [MapperIgnoreTarget(nameof(User.Language))]
    [MapperIgnoreTarget(nameof(User.UploadedBytes))]
    [MapperIgnoreTarget(nameof(User.DownloadedBytes))]
    [MapperIgnoreTarget(nameof(User.RssKey))]
    [MapperIgnoreTarget(nameof(User.Passkey))]
    [MapperIgnoreTarget(nameof(User.CreatedAt))]
    [MapperIgnoreTarget(nameof(User.InviteId))]
    [MapperIgnoreTarget(nameof(User.Invite))]
    [MapperIgnoreTarget(nameof(User.Torrents))]
    [MapperIgnoreTarget(nameof(User.GeneratedInvites))]
    public static partial void MapTo(UpdateUserAdminDto dto, User user);

    [MapProperty(nameof(Torrent.UploadedByUser.UserName), nameof(TorrentDto.UploaderUsername))]
    [MapperIgnoreSource(nameof(Torrent.InfoHash))]
    [MapperIgnoreSource(nameof(Torrent.FilePath))]
    [MapperIgnoreSource(nameof(Torrent.UploadedByUserId))]
    public static partial TorrentDto ToTorrentDto(Torrent torrent);

    [MapProperty(nameof(Request.RequestedByUser), nameof(RequestDto.RequestedByUser))]
    [MapProperty(nameof(Request.FilledByUser), nameof(RequestDto.FilledByUser))]
    [MapperIgnoreSource(nameof(Request.RequestedByUserId))]
    [MapperIgnoreSource(nameof(Request.FilledByUserId))]
    [MapperIgnoreSource(nameof(Request.FilledWithTorrent))]
    public static partial RequestDto ToRequestDto(Request request);

    [MapProperty(nameof(Comment.User), nameof(CommentDto.User))]
    [MapperIgnoreSource(nameof(Comment.Torrent))]
    [MapperIgnoreSource(nameof(Comment.UserId))]
    public static partial CommentDto ToCommentDto(Comment comment);

    [MapProperty(nameof(Message.Sender), nameof(MessageDto.Sender))]
    [MapProperty(nameof(Message.Receiver), nameof(MessageDto.Receiver))]
    [MapperIgnoreSource(nameof(Message.SenderId))]
    [MapperIgnoreSource(nameof(Message.ReceiverId))]
    [MapperIgnoreSource(nameof(Message.SenderDeleted))]
    [MapperIgnoreSource(nameof(Message.ReceiverDeleted))]
    public static partial MessageDto ToMessageDto(Message message);

    [MapProperty(nameof(Report.Torrent), nameof(ReportDto.Torrent))]
    [MapProperty(nameof(Report.ReporterUser), nameof(ReportDto.ReporterUser))]
    [MapProperty(nameof(Report.ProcessedByUser), nameof(ReportDto.ProcessedByUser))]
    [MapperIgnoreSource(nameof(Report.TorrentId))]
    [MapperIgnoreSource(nameof(Report.ReporterUserId))]
    [MapperIgnoreSource(nameof(Report.ProcessedByUserId))]
    public static partial ReportDto ToReportDto(Report report);

    [MapProperty(nameof(Announcement.CreatedByUser), nameof(AnnouncementDto.CreatedByUser))]
    [MapperIgnoreSource(nameof(Announcement.CreatedByUserId))]
    public static partial AnnouncementDto ToAnnouncementDto(Announcement announcement);

    [MapProperty(nameof(Invite.GeneratorUser.UserName), nameof(InviteDto.GeneratorUsername))]
    [MapProperty(nameof(Invite.UsedByUser.UserName), nameof(InviteDto.UsedByUsername))]
    [MapperIgnoreSource(nameof(Invite.GeneratorUserId))]
    public static partial InviteDto ToInviteDto(Invite invite);
    
    [MapperIgnoreSource(nameof(Torrent.InfoHash))]
    [MapperIgnoreSource(nameof(Torrent.FilePath))]
    [MapperIgnoreSource(nameof(Torrent.UploadedByUserId))]
    [MapperIgnoreSource(nameof(Torrent.UploadedByUser))]
    [MapperIgnoreSource(nameof(Torrent.IsDeleted))]
    [MapperIgnoreSource(nameof(Torrent.Seeders))]
    [MapperIgnoreSource(nameof(Torrent.Leechers))]
    [MapperIgnoreSource(nameof(Torrent.DeleteReason))]
    [MapperIgnoreSource(nameof(Torrent.PosterPath))]
    [MapperIgnoreSource(nameof(Torrent.BackdropPath))]
    [MapperIgnoreSource(nameof(Torrent.Rating))]
    public static partial TorrentSearchDto ToTorrentSearchDto(Torrent torrent);
}
