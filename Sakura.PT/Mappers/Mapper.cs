using Riok.Mapperly.Abstractions;
using Sakura.PT.DTOs;
using Sakura.PT.Entities;

namespace Sakura.PT.Mappers;

[Mapper]
public static partial class Mapper
{
    [MapperIgnoreSource(nameof(User.Email))]
    [MapperIgnoreSource(nameof(User.PasswordHash))]
    [MapperIgnoreSource(nameof(User.Language))]
    [MapperIgnoreSource(nameof(User.RssKey))]
    [MapperIgnoreSource(nameof(User.IsBanned))]
    [MapperIgnoreSource(nameof(User.InviteNum))]
    [MapperIgnoreSource(nameof(User.InviteId))]
    [MapperIgnoreSource(nameof(User.Invite))]
    [MapperIgnoreSource(nameof(User.Torrents))]
    [MapperIgnoreSource(nameof(User.GeneratedInvites))]
    [MapperIgnoreSource(nameof(User.Passkey))]
    public static partial UserDto ToUserDto(User user);
    

    [MapProperty(nameof(Torrent.UploadedByUser.UserName), nameof(TorrentDto.UploaderUsername))]
    [MapperIgnoreSource(nameof(Torrent.InfoHash))]
    [MapperIgnoreSource(nameof(Torrent.FilePath))]
    [MapperIgnoreSource(nameof(Torrent.UploadedByUserId))]
    [MapperIgnoreSource(nameof(Torrent.Category))]
    [MapperIgnoreSource(nameof(Torrent.IsDeleted))]
    [MapperIgnoreSource(nameof(Torrent.SearchVector))]
    public static partial TorrentDto ToTorrentDto(Torrent torrent);

    [MapProperty(nameof(Invite.GeneratorUser.UserName), nameof(InviteDto.GeneratorUsername))]
    [MapProperty(nameof(Invite.UsedByUser.UserName), nameof(InviteDto.UsedByUsername))]
    [MapperIgnoreSource(nameof(Invite.GeneratorUserId))]
    [MapperIgnoreSource(nameof(Invite.UsedByUserId))]
    public static partial InviteDto ToInviteDto(Invite invite);
}