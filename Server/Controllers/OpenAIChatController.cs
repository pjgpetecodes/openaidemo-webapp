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
    
    /*     

    [HttpGet]
    public IEnumerable<WeatherForecast> Get()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }

    */

    // POST api/<OpenAIChatController>
    [HttpPost]
    public void Post([FromForm] string value)
    {
        var date = new DateTime();
        Console.WriteLine(value);

        
    }


}
