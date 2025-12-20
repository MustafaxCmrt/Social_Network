using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers.Abstraction
{
    [Route("api/[controller]")]
    [ApiController]
    public abstract class AppController : ControllerBase
    {
    }
}
