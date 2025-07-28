using Riok.Mapperly.Abstractions;
using Sakura.PT.DTOs;
using Sakura.PT.Entities;

namespace Sakura.PT.Mappers;

[Mapper]
public static partial class Mapper
{
    
    public static partial UserDto ToUserDto(User user);
    public static partial TorrentDto ToTorrentDto(this Torrent torrent);
}