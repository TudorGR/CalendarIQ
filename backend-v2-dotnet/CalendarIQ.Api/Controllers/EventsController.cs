using System.IdentityModel.Tokens.Jwt;
using CalendarIQ.Api.Data;
using CalendarIQ.Api.DTOs;
using CalendarIQ.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CalendarIQ.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IEventValidator _eventValidator;

    public EventsController(AppDbContext db, IEventValidator eventValidator)
    {
        _db = db;
        _eventValidator = eventValidator;
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
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { message = "Invalid or missing token" });

        var errors = _eventValidator.Validate(request);
        if (errors.Count > 0)
            return BadRequest(new { errors });

        var newEvent = new Event
        {
            Title = request.Title!.Trim(),
            Day = request.Day!.Value,
            TimeStart = request.TimeStart!,
            TimeEnd = request.TimeEnd!,
            Category = request.Category ?? "Other",
            UserId = userId.Value,
            Location = request.Location,
            Description = request.Description,
            Locked = request.Locked ?? false,
            ReminderEnabled = request.ReminderEnabled ?? false,
            ReminderTime = request.ReminderTime ?? 15
        };

        _db.Events.Add(newEvent);
        await _db.SaveChangesAsync();

        return StatusCode(201, newEvent);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] CreateEventRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { message = "Invalid or missing token" });

        var existingEvent = await _db.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (existingEvent == null) return NotFound(new { message = "Event not found" });

        if (existingEvent.UserId != userId.Value)
            return Forbid();

        var errors = _eventValidator.Validate(request);
        if (errors.Count > 0)
            return BadRequest(new { errors });

        existingEvent.Title = request.Title!.Trim();
        existingEvent.Day = request.Day!.Value;
        existingEvent.TimeStart = request.TimeStart!;
        existingEvent.TimeEnd = request.TimeEnd!;
        existingEvent.Category = request.Category ?? "Other";
        existingEvent.Location = request.Location;
        existingEvent.Description = request.Description;
        existingEvent.Locked = request.Locked ?? false;
        existingEvent.ReminderEnabled = request.ReminderEnabled ?? false;
        existingEvent.ReminderTime = request.ReminderTime ?? 15;

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