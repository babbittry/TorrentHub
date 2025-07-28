using Microsoft.AspNetCore.Mvc;

namespace Sakura.PT.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HelloController : ControllerBase
    {
        private readonly ILogger<HelloController> _logger;

        public HelloController(ILogger<HelloController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetString")]
        public String Get()
        {
            return "hello world!";
        }
    }
}
