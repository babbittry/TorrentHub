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
        [FromQuery(Name = "uploaded")] long uploaded,
        [FromQuery(Name = "downloaded")] long downloaded,
        [FromQuery(Name = "left")] long left,
        [FromQuery(Name = "compact")] int compact = 0,
        [FromQuery(Name = "no_peer_id")] int noPeerId = 0,
        [FromQuery(Name = "event")] string? @event = null,
        [FromQuery(Name = "numwant")] int numWant = 50,
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
            var responseDictionary = await _announceService.ProcessAnnounceRequest(
                infoHash, peerId, port, uploaded, downloaded, left, @event, numWant, key, ipAddress, passkey);

            var bDict = new BDictionary(responseDictionary);
            var bencodedResponse = bDict.EncodeAsBytes();

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
