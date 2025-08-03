using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using BencodeNET.Objects;
using Sakura.PT.Services;
using Microsoft.Extensions.Logging;

namespace Sakura.PT.Controllers;

[ApiController]
[Route("[controller]")]
public class AnnounceController : ControllerBase
{
    private readonly IAnnounceService _announceService;
    private readonly ILogger<AnnounceController> _logger;

    public AnnounceController(IAnnounceService announceService, ILogger<AnnounceController> logger)
    {
        _announceService = announceService;
        _logger = logger;
    }

    [HttpGet("{passkey}/announce")] // Passkey is now part of the route
    public async Task<IActionResult> Announce(
        string passkey,
        [FromQuery(Name = "info_hash")] string infoHash,
        [FromQuery(Name = "peer_id")] string peerId,
        [FromQuery(Name = "port")] int port,
        [FromQuery(Name = "uploaded")] ulong uploaded,
        [FromQuery(Name = "downloaded")] ulong downloaded,
        [FromQuery(Name = "left")] long left,
        [FromQuery(Name = "compact")] int? compact = null,
        [FromQuery(Name = "no_peer_id")] int? noPeerId = null,
        [FromQuery(Name = "event")] string? @event = null,
        [FromQuery(Name = "numwant")]
        [DefaultValue(50)]
        int? numWant = null,
        [FromQuery(Name = "key")] string? key = null,
        [FromQuery(Name = "trackerid")] string? trackerId = null)
    {
        _logger.LogInformation("Announce request received from {IpAddress} for infoHash: {InfoHash}, peerId: {PeerId}, event: {Event}", HttpContext.Connection.RemoteIpAddress?.ToString(), infoHash, peerId, @event);

        // Basic validation
        if (string.IsNullOrEmpty(infoHash) || string.IsNullOrEmpty(peerId) || port == 0)
        {
            _logger.LogWarning("Announce request missing required parameters.");
            return BadRequest("Missing required parameters.");
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrEmpty(ipAddress))
        {
            _logger.LogError("Could not determine client IP address for announce request.");
            return BadRequest("Could not determine client IP address.");
        }

        try
        {
            // Set default values for nullable parameters
            var compactValue = compact ?? 0;
            var noPeerIdValue = noPeerId ?? 0;
            var numWantValue = numWant ?? 50;
            
            var responseDictionary = await _announceService.ProcessAnnounceRequest(
                infoHash, peerId, port, uploaded, downloaded, left, @event, numWantValue, key, ipAddress, passkey);

            var bencodedResponse = responseDictionary.EncodeAsBytes();

            _logger.LogInformation("Announce request processed successfully for infoHash: {InfoHash}, peerId: {PeerId}.", infoHash, peerId);
            return File(bencodedResponse, "text/plain");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Announce request failed due to invalid argument: {ErrorMessage}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Announce request failed due to unauthorized access: {ErrorMessage}", ex.Message);
            return Unauthorized(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Announce request failed because torrent not found: {ErrorMessage}", ex.Message);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during announce request for infoHash: {InfoHash}, peerId: {PeerId}.", infoHash, peerId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
}
