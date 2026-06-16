using System.IdentityModel.Tokens.Jwt;
using CalendarIQ.Api.DTOs;
using CalendarIQ.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CalendarIQ.Api.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{

    private readonly IChatActionService _chatService;
    private readonly IChatIntentService _chatIntentService;

    public ChatController(IChatActionService chatService, IChatIntentService chatIntentService)
    {
        _chatService = chatService;
        _chatIntentService = chatIntentService;
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

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var intent = await _chatIntentService.DetermineIntentAsync(request.Text, userId.Value);
        var response = await _chatService.ExecuteActionAsync(intent, userId.Value, request.WorkingHoursStart, request.WorkingHoursEnd);
        return Ok(response);
    }
}
