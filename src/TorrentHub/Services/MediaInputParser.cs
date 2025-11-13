using System.Text.RegularExpressions;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Enums;

namespace TorrentHub.Services;

public class MediaInputParser
{
    // 豆瓣URL正则 - 支持各种变体 (http/https可选, movie/www子域名可选)
    // 匹配: https://movie.douban.com/subject/3068206/
    //       http://www.douban.com/subject/3068206
    //       douban.com/subject/3068206
    private static readonly Regex DoubanUrlRegex = new(
        @"(?:https?:\/\/)?(?:movie\.|www\.)?douban\.com\/subject\/(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    // IMDb URL正则 - 支持各种变体 (http/https可选, www可选)
    // 匹配: https://www.imdb.com/title/tt1229238/
    //       http://imdb.com/title/tt1229238
    //       www.imdb.com/title/tt1229238
    private static readonly Regex ImdbUrlRegex = new(
        @"(?:https?:\/\/)?(?:www\.)?imdb\.com\/title\/(tt\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    // Technical Specs parsing regexes
    private static readonly Regex ResolutionRegex = new(
        @"(?:(\d{3,4})p|(\d{3,4}x\d{3,4}))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex VideoCodecRegex = new(
        @"(?:x264|x265|H\.264|H\.265|HEVC|AVC|VC-1|MPEG-2)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex AudioCodecRegex = new(
        @"(?:AAC|AC3|DTS|DTS-HD|TrueHD|FLAC|MP3|Atmos|DTS-X)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex SourceRegex = new(
        @"(?:BluRay|Blu-ray|WEB-DL|WEBRip|HDTV|DVDRip|BDRip)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    public ParsedMediaInput Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return CreateInvalid("输入不能为空");

        input = input.Trim();

        // 1. 豆瓣URL匹配
        var doubanMatch = DoubanUrlRegex.Match(input);
        if (doubanMatch.Success)
            return CreateValid(MediaIdSource.DoubanUrl, doubanMatch.Groups[1].Value);

        // 2. IMDb URL匹配
        var imdbMatch = ImdbUrlRegex.Match(input);
        if (imdbMatch.Success)
            return CreateValid(MediaIdSource.ImdbUrl, imdbMatch.Groups[1].Value);

        // 3. IMDb ID: tt1229238 (tt + 7-8位数字，电影或电视剧)
        if (Regex.IsMatch(input, @"^tt\d{7,8}$"))
            return CreateValid(MediaIdSource.ImdbId, input);

        // 4. 豆瓣ID: 3068206 (纯数字，6-10位，电影或电视剧)
        if (Regex.IsMatch(input, @"^\d{6,10}$"))
            return CreateValid(MediaIdSource.DoubanId, input);

        return CreateInvalid("无法识别的格式，请输入豆瓣/IMDb的ID或链接");
    }

    private ParsedMediaInput CreateValid(MediaIdSource source, string id)
    {
        return new ParsedMediaInput
        {
            Source = source,
            Id = id,
            IsValid = true
        };
    }

    private ParsedMediaInput CreateInvalid(string message)
    {
        return new ParsedMediaInput
        {
            Source = MediaIdSource.Unknown,
            IsValid = false,
            ErrorMessage = message
        };
    }

    public TechnicalSpecsDto? ParseTechnicalSpecs(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
            return null;

        var specs = new TechnicalSpecsDto();
        bool hasAnySpec = false;

        // Parse Resolution
        var resolutionMatch = ResolutionRegex.Match(filename);
        if (resolutionMatch.Success)
        {
            specs.Resolution = resolutionMatch.Value;
            hasAnySpec = true;
        }

        // Parse Video Codec
        var videoCodecMatch = VideoCodecRegex.Match(filename);
        if (videoCodecMatch.Success)
        {
            specs.VideoCodec = videoCodecMatch.Value.ToUpperInvariant();
            hasAnySpec = true;
        }

        // Parse Audio Codec
        var audioCodecMatch = AudioCodecRegex.Match(filename);
        if (audioCodecMatch.Success)
        {
            specs.AudioCodec = audioCodecMatch.Value.ToUpperInvariant();
            hasAnySpec = true;
        }

        // Parse Source
        var sourceMatch = SourceRegex.Match(filename);
        if (sourceMatch.Success)
        {
            specs.Source = sourceMatch.Value;
            hasAnySpec = true;
        }

        return hasAnySpec ? specs : null;
    }
}