using Microsoft.AspNetCore.Mvc;

namespace test_app.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetMessage()
        {
            return Ok(new { message = "Hello from API!" });
        }
    }
}