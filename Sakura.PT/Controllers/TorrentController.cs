using Microsoft.AspNetCore.Mvc;
using Sakura.PT.Data;

namespace Sakura.PT.Controllers;

[ApiController]
[Route("[controller]")]
public class TorrentController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TorrentController(ApplicationDbContext context)
    {
        _context = context;
    }
}