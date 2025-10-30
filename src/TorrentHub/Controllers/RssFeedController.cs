using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Xml;
using TorrentHub.Core.DTOs;
using TorrentHub.Core.Services;
using TorrentHub.Core.Data;
using Microsoft.EntityFrameworkCore;
using TorrentHub.Core.Enums;

namespace TorrentHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RssFeedController : ControllerBase
    {
        private readonly IRssFeedTokenService _tokenService;
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public RssFeedController(
            IRssFeedTokenService tokenService,
            ApplicationDbContext dbContext,
            IConfiguration configuration)
        {
            _tokenService = tokenService;
            _dbContext = dbContext;
            _configuration = configuration;
        }

        /// <summary>
        /// 创建新的RSS Feed Token
        /// </summary>
        [HttpPost("tokens")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<RssFeedTokenResponseDto>>> CreateToken([FromBody] CreateRssFeedTokenRequestDto request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            
            var token = await _tokenService.CreateTokenAsync(
                userId,
                request.FeedType,
                request.Name,
                request.CategoryFilter,
                request.MaxResults,
                request.ExpiresAt
            );

            var baseUrl = _configuration["BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
            var rssUrl = $"{baseUrl}/api/rssfeed/feed/{token.Token}";

            var response = new RssFeedTokenResponseDto
            {
                Token = MapToDto(token),
                RssUrl = rssUrl
            };
            return Ok(ApiResponse<RssFeedTokenResponseDto>.SuccessResult(response));
        }

        /// <summary>
        /// 获取当前用户的所有RSS Tokens
        /// </summary>
        [HttpGet("tokens")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<RssFeedTokenDto>>>> GetTokens()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var tokens = await _tokenService.GetUserTokensAsync(userId);
            
            return Ok(ApiResponse<List<RssFeedTokenDto>>.SuccessResult(tokens.Select(MapToDto).ToList()));
        }

        /// <summary>
        /// 更新RSS Feed Token
        /// </summary>
        [HttpPatch("tokens/{id}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<RssFeedTokenDto>>> UpdateToken(
            int id,
            [FromBody] UpdateRssFeedTokenRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                
                var token = await _tokenService.UpdateTokenAsync(id, userId, request);
                
                if (token == null)
                {
                    return NotFound(ApiResponse<RssFeedTokenDto>.ErrorResult("Token not found or access denied"));
                }

                var dto = MapToDto(token);

                return Ok(ApiResponse<RssFeedTokenDto>.SuccessResult(dto, "Token updated successfully"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<RssFeedTokenDto>.ErrorResult(ex.Message));
            }
            catch (Exception)
            {
                return StatusCode(500, ApiResponse<RssFeedTokenDto>.ErrorResult("An error occurred while updating the token"));
            }
        }

        /// <summary>
        /// 撤销指定的RSS Token
        /// </summary>
        [HttpPost("tokens/{id}/revoke")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> RevokeToken(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _tokenService.RevokeTokenAsync(id, userId);
            
            return result ? Ok(ApiResponse.SuccessResult("Token revoked successfully.")) : NotFound(ApiResponse.ErrorResult("Token not found."));
        }

        /// <summary>
        /// 撤销当前用户的所有RSS Tokens
        /// </summary>
        [HttpPost("tokens/revoke-all")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<int>>> RevokeAllTokens()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var count = await _tokenService.RevokeAllUserTokensAsync(userId);
            
            return Ok(ApiResponse<int>.SuccessResult(count, $"Successfully revoked {count} tokens."));
        }

        /// <summary>
        /// 获取RSS Feed (无需认证，通过token验证)
        /// </summary>
        [HttpGet("feed/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFeed(Guid token)
        {
            // 验证token
            var rssFeedToken = await _tokenService.GetTokenAsync(token);
            if (rssFeedToken == null)
            {
                return NotFound("Invalid or revoked RSS token");
            }

            // 检查是否过期
            if (!await _tokenService.ValidateTokenAsync(token))
            {
                return Unauthorized("RSS token has expired");
            }

            // 更新使用统计
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = Request.Headers["User-Agent"].ToString();
            await _tokenService.UpdateTokenUsageAsync(rssFeedToken.Id, ipAddress, userAgent);

            // 获取种子列表
            var torrentsQuery = _dbContext.Torrents
                .Where(t => !t.IsDeleted)
                .OrderByDescending(t => t.CreatedAt)
                .AsQueryable();

            // 应用分类过滤
            if (rssFeedToken.CategoryFilter != null && rssFeedToken.CategoryFilter.Length > 0)
            {
                var categoryEnums = rssFeedToken.CategoryFilter
                    .Select(c => Enum.TryParse<TorrentCategory>(c, out var cat) ? cat : (TorrentCategory?)null)
                    .Where(c => c.HasValue)
                    .Select(c => c!.Value)
                    .ToList();
                
                if (categoryEnums.Any())
                {
                    torrentsQuery = torrentsQuery.Where(t => categoryEnums.Contains(t.Category));
                }
            }

            // 限制结果数量
            var torrents = await torrentsQuery
                .Take(rssFeedToken.MaxResults)
                .Include(t => t.UploadedByUser)
                .ToListAsync();

            // 生成RSS XML
            var rssXml = GenerateRssXml(torrents, rssFeedToken);
            
            return Content(rssXml, "application/rss+xml", Encoding.UTF8);
        }

        private string GenerateRssXml(List<Core.Entities.Torrent> torrents, Core.Entities.RssFeedToken token)
        {
            var baseUrl = _configuration["BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
            
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8
            };

            using var stringWriter = new StringWriter();
            using var writer = XmlWriter.Create(stringWriter, settings);

            writer.WriteStartDocument();
            writer.WriteStartElement("rss");
            writer.WriteAttributeString("version", "2.0");
            writer.WriteAttributeString("xmlns", "atom", null, "http://www.w3.org/2005/Atom");

            writer.WriteStartElement("channel");
            
            // Channel metadata
            writer.WriteElementString("title", $"TorrentHub RSS Feed - {token.Name ?? token.FeedType.ToString()}");
            writer.WriteElementString("link", baseUrl);
            writer.WriteElementString("description", $"RSS feed for {token.FeedType.ToString()} torrents");
            writer.WriteElementString("language", "zh-CN");
            writer.WriteElementString("lastBuildDate", DateTimeOffset.UtcNow.ToString("R"));

            // atom:link for self
            writer.WriteStartElement("atom", "link", "http://www.w3.org/2005/Atom");
            writer.WriteAttributeString("href", $"{baseUrl}/api/rssfeed/feed/{token.Token}");
            writer.WriteAttributeString("rel", "self");
            writer.WriteAttributeString("type", "application/rss+xml");
            writer.WriteEndElement();

            // Items
            foreach (var torrent in torrents)
            {
                writer.WriteStartElement("item");
                
                writer.WriteElementString("title", torrent.Name);
                writer.WriteElementString("link", $"{baseUrl}/torrents/{torrent.Id}");
                writer.WriteElementString("guid", $"{baseUrl}/torrents/{torrent.Id}");
                writer.WriteElementString("pubDate", torrent.CreatedAt.ToString("R"));
                writer.WriteElementString("category", torrent.Category.ToString());
                
                // Description with metadata
                var description = $@"
                    <![CDATA[
                    <p><strong>Category:</strong> {torrent.Category}</p>
                    <p><strong>Size:</strong> {FormatBytes(torrent.Size)}</p>
                    <p><strong>Uploader:</strong> {torrent.UploadedByUser?.UserName ?? "Unknown"}</p>
                    <p><strong>Seeders:</strong> {torrent.Seeders} | <strong>Leechers:</strong> {torrent.Leechers}</p>
                    {(!string.IsNullOrEmpty(torrent.Description) ? $"<p>{torrent.Description}</p>" : "")}
                    ]]>
                ";
                writer.WriteRaw(description);

                // Enclosure for torrent file download
                writer.WriteStartElement("enclosure");
                writer.WriteAttributeString("url", $"{baseUrl}/api/torrent/{torrent.Id}/download");
                writer.WriteAttributeString("length", torrent.Size.ToString());
                writer.WriteAttributeString("type", "application/x-bittorrent");
                writer.WriteEndElement(); // enclosure

                writer.WriteEndElement(); // item
            }

            writer.WriteEndElement(); // channel
            writer.WriteEndElement(); // rss
            writer.WriteEndDocument();

            return stringWriter.ToString();
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private RssFeedTokenDto MapToDto(Core.Entities.RssFeedToken token)
        {
            return new RssFeedTokenDto
            {
                Id = token.Id,
                Token = token.Token,
                FeedType = token.FeedType,
                Name = token.Name,
                CategoryFilter = token.CategoryFilter,
                MaxResults = token.MaxResults,
                IsActive = token.IsActive,
                ExpiresAt = token.ExpiresAt,
                LastUsedAt = token.LastUsedAt,
                UsageCount = token.UsageCount,
                UserAgent = token.UserAgent,
                LastIp = token.LastIp,
                CreatedAt = token.CreatedAt,
                RevokedAt = token.RevokedAt
            };
        }
    }
}