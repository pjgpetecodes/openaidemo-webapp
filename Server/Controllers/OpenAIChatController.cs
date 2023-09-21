using Microsoft.AspNetCore.Mvc;
using openaidemo_webapp.Shared;

namespace openaidemo_webapp.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class OpenAIChatController : ControllerBase
{
    private readonly ILogger<OpenAIChatController> _logger;

    public OpenAIChatController(ILogger<OpenAIChatController> logger)
    {
        _logger = logger;
    }

}
