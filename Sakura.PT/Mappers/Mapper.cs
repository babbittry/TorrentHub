using Riok.Mapperly.Abstractions;
using Sakura.PT.DTOs;
using Sakura.PT.Entities;

namespace Sakura.PT.Mappers;

[Mapper]
public static partial class Mapper
{
    public static partial UserDto ToUserDto(User user);

    [MapProperty(nameof(Torrent.UploadedByUser.UserName), nameof(TorrentDto.UploaderUsername))]
    public static partial TorrentDto ToTorrentDto(Torrent torrent);

    [MapProperty(nameof(Invite.GeneratorUser.UserName), nameof(InviteDto.GeneratorUsername))]
    [MapProperty(nameof(Invite.UsedByUser.UserName), nameof(InviteDto.UsedByUsername))]
    public static partial InviteDto ToInviteDto(Invite invite);
}