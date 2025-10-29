using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using BencodeNET.Objects;
using Microsoft.Extensions.Logging;
using TorrentHub.Tracker.Services;

namespace TorrentHub.Tracker.Controllers;

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

    [HttpGet("{credential}/announce")]
    public async Task<IActionResult> Announce(string credential)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress;
        _logger.LogInformation("Announce request received from {IpAddress} with credential: {Credential}",
            ipAddress?.ToString(), credential);

        try
        {
            var bencodedResponse = await _announceService.HandleAnnounceAsync(HttpContext);
            return File(bencodedResponse, "text/plain");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during announce request");
            var errorDict = new BDictionary
            {
                { "failure reason", new BString("Internal server error") }
            };
            return File(errorDict.EncodeAsBytes(), "text/plain");
        }
    }
}
