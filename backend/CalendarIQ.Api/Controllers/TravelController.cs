using CalendarIQ.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CalendarIQ.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TravelController : ControllerBase
{
    private readonly TravelService _travelService;
    public TravelController(TravelService travelService)
    {
        _travelService = travelService;
    }

    [HttpPost("location-to-coords")]
    public async Task<IActionResult> LocationToCoords([FromBody] LocationRequest request)
    {
        try
        {
            var result = await _travelService.LocationToCoordinatesAsync(request.Location);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching travel data: {ex}");
            return StatusCode(500, new { error = "An error occurred while fetching travel data" });
        }
    }

    [HttpGet("weather")]
    public async Task<IActionResult> GetWeather(
        [FromQuery] double latitude,
        [FromQuery] double longitude)
    {
        var result = await _travelService.GetWeatherForecastAsync(latitude, longitude);
        return Ok(result);
    }
}