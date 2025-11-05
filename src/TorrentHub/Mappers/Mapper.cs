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
    [MapperIgnoreSource(nameof(User.InviteId))]
    [MapperIgnoreSource(nameof(User.Invite))]
    [MapperIgnoreSource(nameof(User.Torrents))]
    [MapperIgnoreSource(nameof(User.GeneratedInvites))]
    [MapperIgnoreSource(nameof(User.BanStatus))]
    [MapperIgnoreSource(nameof(User.BanReason))]
    [MapperIgnoreSource(nameof(User.BanUntil))]
    [MapperIgnoreSource(nameof(User.CheatWarningCount))]
    [MapperIgnoreSource(nameof(User.TwoFactorSecretKey))]
    [MapperIgnoreSource(nameof(User.TwoFactorType))]
    [MapperIgnoreSource(nameof(User.IsEmailVerified))]
    [MapperIgnoreTarget(nameof(UserPublicProfileDto.InvitedBy))]
    [MapperIgnoreTarget(nameof(UserPublicProfileDto.SeedingSize))]
    [MapperIgnoreTarget(nameof(UserPublicProfileDto.CurrentSeedingCount))]
    [MapperIgnoreTarget(nameof(UserPublicProfileDto.CurrentLeechingCount))]
    public static partial UserPublicProfileDto ToUserPublicProfileDto(User user);

    // Private profile includes more details, but still hides sensitive info.
    [MapperIgnoreSource(nameof(User.PasswordHash))]
    [MapperIgnoreSource(nameof(User.InviteId))]
    [MapperIgnoreSource(nameof(User.Invite))]
    [MapperIgnoreSource(nameof(User.Torrents))]
    [MapperIgnoreSource(nameof(User.GeneratedInvites))]
    [MapperIgnoreSource(nameof(User.TwoFactorSecretKey))]
    [MapperIgnoreSource(nameof(User.IsEmailVerified))]
    [MapProperty(nameof(User.TwoFactorType), nameof(UserPrivateProfileDto.TwoFactorMethod))]
    [MapperIgnoreTarget(nameof(UserPrivateProfileDto.UnreadMessagesCount))]
    public static partial UserPrivateProfileDto ToUserPrivateProfileDto(User user);
    
    // Maps fields a user is allowed to change on their own profile.
    [MapProperty(nameof(UpdateUserProfileDto.AvatarUrl), nameof(User.Avatar))]
    [MapperIgnoreTarget(nameof(User.Id))]
    [MapperIgnoreTarget(nameof(User.UserName))]
    [MapperIgnoreTarget(nameof(User.Email))]
    [MapperIgnoreTarget(nameof(User.PasswordHash))]
    [MapperIgnoreTarget(nameof(User.UploadedBytes))]
    [MapperIgnoreTarget(nameof(User.DownloadedBytes))]
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
    [MapperIgnoreTarget(nameof(User.TwoFactorSecretKey))]
    [MapperIgnoreTarget(nameof(User.TwoFactorType))]
    [MapperIgnoreTarget(nameof(User.IsEmailVerified))]
    [MapperIgnoreTarget(nameof(User.UserTitle))]
    [MapperIgnoreTarget(nameof(User.EquippedBadgeId))]
    [MapperIgnoreTarget(nameof(User.ColorfulUsernameExpiresAt))]
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
    [MapperIgnoreTarget(nameof(User.CreatedAt))]
    [MapperIgnoreTarget(nameof(User.InviteId))]
    [MapperIgnoreTarget(nameof(User.Invite))]
    [MapperIgnoreTarget(nameof(User.Torrents))]
    [MapperIgnoreTarget(nameof(User.GeneratedInvites))]
    [MapperIgnoreTarget(nameof(User.TwoFactorSecretKey))]
    [MapperIgnoreTarget(nameof(User.TwoFactorType))]
    [MapperIgnoreTarget(nameof(User.IsEmailVerified))]
    [MapperIgnoreTarget(nameof(User.UserTitle))]
    [MapperIgnoreTarget(nameof(User.EquippedBadgeId))]
    [MapperIgnoreTarget(nameof(User.ColorfulUsernameExpiresAt))]
    public static partial void MapTo(UpdateUserAdminDto dto, User user);

    [MapProperty(nameof(Torrent.UploadedByUser), nameof(TorrentDto.Uploader))]
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

    [MapProperty(nameof(TorrentComment.User), nameof(TorrentCommentDto.User))]
    [MapProperty(nameof(TorrentComment.ReplyToUser), nameof(TorrentCommentDto.ReplyToUser))]
    [MapperIgnoreSource(nameof(TorrentComment.Torrent))]
    [MapperIgnoreSource(nameof(TorrentComment.UserId))]
    [MapperIgnoreSource(nameof(TorrentComment.ParentTorrentComment))]
    [MapperIgnoreSource(nameof(TorrentComment.Replies))]
    [MapperIgnoreSource(nameof(TorrentComment.ReplyToUserId))]
    [MapperIgnoreTarget(nameof(TorrentCommentDto.Reactions))]
    public static partial TorrentCommentDto ToTorrentCommentDto(TorrentComment comment);

    [MapProperty(nameof(RequestComment.User), nameof(RequestCommentDto.User))]
    [MapProperty(nameof(RequestComment.ReplyToUser), nameof(RequestCommentDto.ReplyToUser))]
    [MapperIgnoreSource(nameof(RequestComment.Request))]
    [MapperIgnoreSource(nameof(RequestComment.UserId))]
    [MapperIgnoreSource(nameof(RequestComment.ParentRequestComment))]
    [MapperIgnoreSource(nameof(RequestComment.Replies))]
    [MapperIgnoreSource(nameof(RequestComment.ReplyToUserId))]
    [MapperIgnoreTarget(nameof(RequestCommentDto.Reactions))]
    public static partial RequestCommentDto ToRequestCommentDto(RequestComment comment);

    [MapProperty(nameof(Message.Sender), nameof(MessageDto.Sender))]
    [MapProperty(nameof(Message.Receiver), nameof(MessageDto.Receiver))]
    [MapperIgnoreSource(nameof(Message.SenderId))]
    [MapperIgnoreSource(nameof(Message.ReceiverId))]
    [MapperIgnoreSource(nameof(Message.SenderDeleted))]
    [MapperIgnoreSource(nameof(Message.ReceiverDeleted))]
    public static partial MessageDto ToMessageDto(Message message);

    [MapProperty(nameof(Report.ReporterUser), nameof(ReportDto.ReporterUser))]
    [MapProperty(nameof(Report.ProcessedByUser), nameof(ReportDto.ProcessedByUser))]
    [MapProperty(nameof(Report.Torrent), nameof(ReportDto.Torrent), Use = nameof(ToOptionalTorrentDto))]
    [MapperIgnoreSource(nameof(Report.TorrentId))]
    [MapperIgnoreSource(nameof(Report.ReporterUserId))]
    [MapperIgnoreSource(nameof(Report.ProcessedByUserId))]
    public static partial ReportDto ToReportDto(Report report);

    // Custom mapping to handle nullable Torrent
    private static TorrentDto? ToOptionalTorrentDto(Torrent? torrent)
    {
        if (torrent is null)
            return null;

        return ToTorrentDto(torrent);
    }

    [MapProperty(nameof(Announcement.CreatedByUser), nameof(AnnouncementDto.CreatedByUser))]
    [MapperIgnoreSource(nameof(Announcement.CreatedByUserId))]
    public static partial AnnouncementDto ToAnnouncementDto(Announcement announcement);

    [MapProperty(nameof(Invite.GeneratorUser.UserName), nameof(InviteDto.GeneratorUsername))]
    [MapProperty(nameof(Invite.UsedByUser.UserName), nameof(InviteDto.UsedByUsername))]
    [MapperIgnoreSource(nameof(Invite.GeneratorUserId))]
    public static partial InviteDto ToInviteDto(Invite invite);

    [MapProperty(nameof(ForumPost.Author), nameof(ForumPostDto.Author))]
    [MapProperty(nameof(ForumPost.ReplyToUser), nameof(ForumPostDto.ReplyToUser))]
    [MapperIgnoreSource(nameof(ForumPost.Topic))]
    [MapperIgnoreSource(nameof(ForumPost.AuthorId))]
    [MapperIgnoreSource(nameof(ForumPost.ParentPost))]
    [MapperIgnoreSource(nameof(ForumPost.Replies))]
    [MapperIgnoreSource(nameof(ForumPost.ReplyToUserId))]
    [MapperIgnoreTarget(nameof(ForumPostDto.Reactions))]
    public static partial ForumPostDto ToForumPostDto(ForumPost post);

    [MapProperty(nameof(ForumTopic.Author), nameof(ForumTopicDto.Author))]
    [MapProperty(nameof(ForumTopic.Posts.Count), nameof(ForumTopicDto.PostCount))]
    [MapperIgnoreSource(nameof(ForumTopic.Category))]
    [MapperIgnoreSource(nameof(ForumTopic.AuthorId))]
    public static partial ForumTopicDto ToForumTopicDto(ForumTopic topic);
    
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

    [MapProperty(nameof(User.UserTitle), nameof(UserDisplayDto.UserTitle))]
    [MapProperty(nameof(User.UserName), nameof(UserDisplayDto.Username))]
    [MapProperty(nameof(User.ColorfulUsernameExpiresAt), nameof(UserDisplayDto.IsColorfulUsernameActive), Use = nameof(MapIsColorful))]
    [MapperIgnoreTarget(nameof(UserDisplayDto.UserLevelName))]
    [MapperIgnoreTarget(nameof(UserDisplayDto.UserLevelColor))]
    [MapperIgnoreTarget(nameof(UserDisplayDto.EquippedBadge))]
    [MapperIgnoreSource(nameof(User.Email))]
    [MapperIgnoreSource(nameof(User.IsEmailVerified))]
    [MapperIgnoreSource(nameof(User.PasswordHash))]
    [MapperIgnoreSource(nameof(User.Signature))]
    [MapperIgnoreSource(nameof(User.Language))]
    [MapperIgnoreSource(nameof(User.UploadedBytes))]
    [MapperIgnoreSource(nameof(User.DownloadedBytes))]
    [MapperIgnoreSource(nameof(User.NominalUploadedBytes))]
    [MapperIgnoreSource(nameof(User.NominalDownloadedBytes))]
    [MapperIgnoreSource(nameof(User.Role))]
    [MapperIgnoreSource(nameof(User.CreatedAt))]
    [MapperIgnoreSource(nameof(User.BanStatus))]
    [MapperIgnoreSource(nameof(User.BanReason))]
    [MapperIgnoreSource(nameof(User.CheatWarningCount))]
    [MapperIgnoreSource(nameof(User.BanUntil))]
    [MapperIgnoreSource(nameof(User.InviteNum))]
    [MapperIgnoreSource(nameof(User.Coins))]
    [MapperIgnoreSource(nameof(User.TotalSeedingTimeMinutes))]
    [MapperIgnoreSource(nameof(User.TotalLeechingTimeMinutes))]
    [MapperIgnoreSource(nameof(User.IsDoubleUploadActive))]
    [MapperIgnoreSource(nameof(User.DoubleUploadExpiresAt))]
    [MapperIgnoreSource(nameof(User.IsNoHRActive))]
    [MapperIgnoreSource(nameof(User.NoHRExpiresAt))]
    [MapperIgnoreSource(nameof(User.InviteId))]
    [MapperIgnoreSource(nameof(User.Invite))]
    [MapperIgnoreSource(nameof(User.TwoFactorSecretKey))]
    [MapperIgnoreSource(nameof(User.TwoFactorType))]
    [MapperIgnoreSource(nameof(User.Torrents))]
    [MapperIgnoreSource(nameof(User.GeneratedInvites))]
    [MapperIgnoreSource(nameof(User.EquippedBadgeId))]
    public static partial UserDisplayDto ToUserDisplayDto(User user);
    
    private static bool MapIsColorful(DateTimeOffset? expiresAt)
    {
        return expiresAt.HasValue && expiresAt.Value > DateTimeOffset.UtcNow;
    }

    // TorrentCredential to CredentialDto mapping - Manual mapping to handle navigation properties
    public static CredentialDto ToCredentialDto(TorrentCredential credential)
    {
        return new CredentialDto
        {
            Id = credential.Id,
            Credential = credential.Credential,
            TorrentId = credential.TorrentId,
            TorrentName = credential.Torrent?.Name ?? "Unknown",
            IsRevoked = credential.IsRevoked,
            CreatedAt = credential.CreatedAt,
            RevokedAt = credential.RevokedAt,
            RevokeReason = credential.RevokeReason,
            LastUsedAt = credential.LastUsedAt,
            FirstUsedAt = credential.FirstUsedAt,
            UsageCount = credential.UsageCount,
            AnnounceCount = credential.AnnounceCount,
            TotalUploadedBytes = credential.TotalUploadedBytes,
            TotalDownloadedBytes = credential.TotalDownloadedBytes,
            LastIpAddress = credential.LastIpAddress,
            LastUserAgent = credential.LastUserAgent
        };
    }
}
