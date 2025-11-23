using BencodeNET.Objects;
using BencodeNET.Parsing;
using BencodeNET.Torrents;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using TorrentHub.Core.Services;

namespace TorrentHub.Services;

/// <summary>
/// 种子清洗服务实现
/// </summary>
public class TorrentWashingService : ITorrentWashingService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TorrentWashingService> _logger;
    private readonly BencodeParser _parser = new();

    public TorrentWashingService(IConfiguration configuration, ILogger<TorrentWashingService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// 清洗种子文件
    /// </summary>
    public byte[] WashTorrent(byte[] torrentBytes, int torrentId, string trackerUrl)
    {
        try
        {
            _logger.LogInformation("开始清洗种子，TorrentId: {TorrentId}", torrentId);

            // 解析种子文件
            using var stream = new MemoryStream(torrentBytes);
            var torrentDict = _parser.Parse<BDictionary>(stream);

            // 获取配置
            var newSource = _configuration["SiteInfo:TorrentSource"] ?? "PT@TorrentHub";
            var baseUrl = _configuration["SiteInfo:BaseUrl"] ?? "https://yoursite.com";
            var newComment = $"{baseUrl}/details/{torrentId}";

            _logger.LogDebug("清洗配置 - NewSource: {NewSource}, Comment: {Comment}", newSource, newComment);

            // 1. 替换 announce
            torrentDict["announce"] = new BString(trackerUrl);
            _logger.LogDebug("已替换 announce: {Announce}", trackerUrl);

            // 2. 删除 announce-list
            if (torrentDict.ContainsKey("announce-list"))
            {
                torrentDict.Remove("announce-list");
                _logger.LogDebug("已删除 announce-list");
            }

            // 3. 设置 comment
            torrentDict["comment"] = new BString(newComment);
            _logger.LogDebug("已设置 comment: {Comment}", newComment);

            // 4. 删除 created by
            if (torrentDict.ContainsKey("created by"))
            {
                torrentDict.Remove("created by");
                _logger.LogDebug("已删除 created by");
            }

            // 5. 删除 nodes (DHT)
            if (torrentDict.ContainsKey("nodes"))
            {
                torrentDict.Remove("nodes");
                _logger.LogDebug("已删除 nodes");
            }

            // 6. 删除 url-list (WebSeed)
            if (torrentDict.ContainsKey("url-list"))
            {
                torrentDict.Remove("url-list");
                _logger.LogDebug("已删除 url-list");
            }

            // 7. 删除 httpseeds
            if (torrentDict.ContainsKey("httpseeds"))
            {
                torrentDict.Remove("httpseeds");
                _logger.LogDebug("已删除 httpseeds");
            }

            // 8. 修改 info 字典
            if (torrentDict.Get<BDictionary>("info") is BDictionary infoDict)
            {
                // 设置 source
                infoDict["source"] = new BString(newSource);
                _logger.LogDebug("已设置 info.source: {Source}", newSource);

                // 设置 private = 1
                infoDict["private"] = new BNumber(1);
                _logger.LogDebug("已设置 info.private = 1");
            }

            // 编码为字节数组
            using var outputStream = new MemoryStream();
            torrentDict.EncodeTo(outputStream);
            var washedBytes = outputStream.ToArray();

            var originalInfoHash = CalculateInfoHash(torrentBytes);
            var newInfoHash = CalculateInfoHash(washedBytes);

            _logger.LogInformation("种子清洗完成 - TorrentId: {TorrentId}, 原InfoHash: {OriginalHash}, 新InfoHash: {NewHash}",
                torrentId, originalInfoHash, newInfoHash);

            return washedBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清洗种子失败，TorrentId: {TorrentId}", torrentId);
            throw;
        }
    }

    /// <summary>
    /// 计算种子的 InfoHash
    /// </summary>
    public string CalculateInfoHash(byte[] torrentBytes)
    {
        try
        {
            using var stream = new MemoryStream(torrentBytes);
            var torrentDict = _parser.Parse<BDictionary>(stream);

            if (torrentDict.Get<BDictionary>("info") is BDictionary infoDict)
            {
                using var infoStream = new MemoryStream();
                infoDict.EncodeTo(infoStream);
                var infoBytes = infoStream.ToArray();

                using var sha1 = SHA1.Create();
                var hashBytes = sha1.ComputeHash(infoBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }

            throw new InvalidOperationException("种子文件中未找到 info 字典");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "计算 InfoHash 失败");
            throw;
        }
    }
}