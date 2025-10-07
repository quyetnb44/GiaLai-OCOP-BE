using Microsoft.AspNetCore.Mvc;

namespace GiaLaiOCOP.Api.Controllers
{
    [ApiController]
    [Route("/")]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Ok("Backend đang chạy, nhưng đây là API, không phải trang web.");
        }
    }
}
