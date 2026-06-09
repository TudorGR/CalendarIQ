using System.IdentityModel.Tokens.Jwt;
using CalendarIQ.Api.Data;
using CalendarIQ.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CalendarIQ.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _db;

    public EventsController(AppDbContext db)
    {
        _db = db;
    }

    private int? GetCurrentUserId()
    {
        if (!Request.Headers.TryGetValue("x-auth-token", out var tokenValue))
            return null;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(tokenValue.FirstOrDefault());
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

            return userIdClaim != null ? int.Parse(userIdClaim) : null;
        }
        catch
        {
            return null;
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetEvents()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { message = "Invalid or missing token" });

        var events = await _db.Events.Where(e => e.UserId == userId.Value).ToListAsync();

        return Ok(events);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEvent([FromBody] Event newEvent)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { message = "Invalid or missing token" });

        //validation to be added as per the backend specifications

        newEvent.UserId = userId.Value;

        _db.Events.Add(newEvent);
        await _db.SaveChangesAsync();
        return StatusCode(201, newEvent);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] Event updatedData)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { message = "Invalid or missing token" });

        var existingEvent = await _db.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (existingEvent == null) return NotFound(new { message = "Event not found" });

        if (existingEvent.UserId != userId.Value)
            return Forbid();

        existingEvent.Title = updatedData.Title;
        existingEvent.Day = updatedData.Day;
        existingEvent.TimeStart = updatedData.TimeStart;
        existingEvent.TimeEnd = updatedData.TimeEnd;
        existingEvent.Category = updatedData.Category;

        await _db.SaveChangesAsync();
        return Ok(existingEvent);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { message = "Invalid or missing token" });

        var existingEvent = await _db.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (existingEvent == null) return NotFound(new { message = "Event not found" });

        if (existingEvent.UserId != userId.Value)
            return Forbid();

        _db.Events.Remove(existingEvent);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Event deleted successfully" });
    }
}