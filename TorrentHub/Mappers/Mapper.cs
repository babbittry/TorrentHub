using Riok.Mapperly.Abstractions;
using TorrentHub.DTOs;
using TorrentHub.Entities;

namespace TorrentHub.Mappers;

[Mapper]
public static partial class Mapper
{
    [MapperIgnoreSource(nameof(User.PasswordHash))]
    [MapperIgnoreSource(nameof(User.Language))]
    [MapperIgnoreSource(nameof(User.RssKey))]
    [MapperIgnoreSource(nameof(User.IsBanned))]
    [MapperIgnoreSource(nameof(User.InviteId))]
    [MapperIgnoreSource(nameof(User.Invite))]
    [MapperIgnoreSource(nameof(User.Torrents))]
    [MapperIgnoreSource(nameof(User.GeneratedInvites))]
    [MapperIgnoreSource(nameof(User.Passkey))]
    [MapperIgnoreSource(nameof(User.Email))]
    [MapperIgnoreSource(nameof(User.BanReason))]
    [MapperIgnoreSource(nameof(User.BanUntil))]
    public static partial UserPublicProfileDto ToUserPublicProfileDto(User user);

    [MapperIgnoreSource(nameof(User.PasswordHash))]
    [MapperIgnoreSource(nameof(User.Language))]
    [MapperIgnoreSource(nameof(User.RssKey))]
    [MapperIgnoreSource(nameof(User.IsBanned))]
    [MapperIgnoreSource(nameof(User.InviteId))]
    [MapperIgnoreSource(nameof(User.Invite))]
    [MapperIgnoreSource(nameof(User.Torrents))]
    [MapperIgnoreSource(nameof(User.GeneratedInvites))]
    [MapperIgnoreSource(nameof(User.Passkey))]
    public static partial UserPrivateProfileDto ToUserPrivateProfileDto(User user);
    
    [MapProperty(nameof(UpdateUserProfileDto.AvatarUrl), nameof(User.Avatar))]
    [MapperIgnoreTarget(nameof(User.Id))]
    [MapperIgnoreTarget(nameof(User.UserName))]
    [MapperIgnoreTarget(nameof(User.Email))]
    [MapperIgnoreTarget(nameof(User.PasswordHash))]
    [MapperIgnoreTarget(nameof(User.Language))]
    [MapperIgnoreTarget(nameof(User.UploadedBytes))]
    [MapperIgnoreTarget(nameof(User.DownloadedBytes))]
    [MapperIgnoreTarget(nameof(User.RssKey))]
    [MapperIgnoreTarget(nameof(User.Passkey))]
    [MapperIgnoreTarget(nameof(User.Role))]
    [MapperIgnoreTarget(nameof(User.CreatedAt))]
    [MapperIgnoreTarget(nameof(User.IsBanned))]
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
    [MapperIgnoreTarget(nameof(User.BanReason))]
    [MapperIgnoreTarget(nameof(User.BanUntil))]
    [MapperIgnoreTarget(nameof(User.NominalUploadedBytes))]
    [MapperIgnoreTarget(nameof(User.NominalDownloadedBytes))]
    public static partial void MapTo(UpdateUserProfileDto dto, User user);

    [MapProperty(nameof(UpdateUserAdminDto.BanReason), nameof(User.BanReason))]
    [MapProperty(nameof(UpdateUserAdminDto.BanUntil), nameof(User.BanUntil))]
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
    [MapperIgnoreTarget(nameof(User.NominalUploadedBytes))]
    [MapperIgnoreTarget(nameof(User.NominalDownloadedBytes))]
    public static partial void MapTo(UpdateUserAdminDto dto, User user);

    [MapProperty(nameof(Torrent.UploadedByUser.UserName), nameof(TorrentDto.UploaderUsername))]
    [MapperIgnoreSource(nameof(Torrent.InfoHash))]
    [MapperIgnoreSource(nameof(Torrent.FilePath))]
    [MapperIgnoreSource(nameof(Torrent.UploadedByUserId))]
    [MapperIgnoreSource(nameof(Torrent.IsDeleted))]
    public static partial TorrentDto ToTorrentDto(Torrent torrent);

    [MapProperty(nameof(Request.RequestedByUser), nameof(RequestDto.RequestedByUser))]
    [MapProperty(nameof(Request.FilledByUser), nameof(RequestDto.FilledByUser))]
    [MapperIgnoreSource(nameof(Request.RequestedByUserId))]
    [MapperIgnoreSource(nameof(Request.FilledByUserId))]
    [MapperIgnoreSource(nameof(Request.FilledWithTorrent))] // Assuming we don't need the full torrent object in RequestDto
    public static partial RequestDto ToRequestDto(Request request);

    [MapProperty(nameof(Comment.User), nameof(CommentDto.User))]
    [MapperIgnoreSource(nameof(Comment.Torrent))] // Assuming we don't need the full torrent object in CommentDto
    [MapperIgnoreSource(nameof(Comment.UserId))] // Mapped via User navigation property
    public static partial CommentDto ToCommentDto(Comment comment);

    [MapProperty(nameof(Message.Sender), nameof(MessageDto.Sender))]
    [MapProperty(nameof(Message.Receiver), nameof(MessageDto.Receiver))]
    [MapperIgnoreSource(nameof(Message.SenderId))] // Mapped via Sender navigation property
    [MapperIgnoreSource(nameof(Message.ReceiverId))] // Mapped via Receiver navigation property
    [MapperIgnoreSource(nameof(Message.SenderDeleted))] // Not needed in DTO
    [MapperIgnoreSource(nameof(Message.ReceiverDeleted))] // Not needed in DTO
    public static partial MessageDto ToMessageDto(Message message);

    [MapProperty(nameof(Report.Torrent), nameof(ReportDto.Torrent))]
    [MapProperty(nameof(Report.ReporterUser), nameof(ReportDto.ReporterUser))]
    [MapProperty(nameof(Report.ProcessedByUser), nameof(ReportDto.ProcessedByUser))]
    [MapperIgnoreSource(nameof(Report.TorrentId))] // Mapped via Torrent navigation property
    [MapperIgnoreSource(nameof(Report.ReporterUserId))] // Mapped via ReporterUser navigation property
    [MapperIgnoreSource(nameof(Report.ProcessedByUserId))] // Mapped via ProcessedByUser navigation property
    public static partial ReportDto ToReportDto(Report report);

    [MapProperty(nameof(Announcement.CreatedByUser), nameof(AnnouncementDto.CreatedByUser))]
    [MapperIgnoreSource(nameof(Announcement.CreatedByUserId))] // Mapped via CreatedByUser navigation property
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
    public static partial TorrentSearchDto ToTorrentSearchDto(Torrent torrent);
}
