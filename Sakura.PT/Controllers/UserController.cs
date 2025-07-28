using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sakura.PT.Data;
using Sakura.PT.DTOs;
using Sakura.PT.Mappers;

namespace Sakura.PT.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    public UserController(ApplicationDbContext context)
    {
        _context = context;
    }
    
    /*[HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await _context.Users.ToListAsync();
        var userDtos = users.Select(u => u.ToUserDto());
        return Ok(userDtos);
    }*/

}