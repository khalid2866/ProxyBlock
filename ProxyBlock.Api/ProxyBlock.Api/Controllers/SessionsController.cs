using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProxyBlock.Api.Data;
using ProxyBlock.Api.DTOs;
using ProxyBlock.Api.Models;
using System.Security.Claims;

namespace ProxyBlock.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SessionsController : ControllerBase
{
    private readonly AppDbContext _db;
    public SessionsController(AppDbContext db) => _db = db;

    private int CurrentUserId =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpPost("start")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Start(StartSessionRequest req)
    {
        var session = new AttendanceSession
        {
            TeacherId = CurrentUserId,
            CourseName = req.CourseName,
            Latitude = req.Latitude,
            Longitude = req.Longitude,
            CurrentToken = NewToken(),
            TokenExpiresAt = DateTime.UtcNow.AddSeconds(10)
        };
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();
        return Ok(new { session.Id, session.CurrentToken, session.TokenExpiresAt });
    }

    [HttpGet("{id}/token")]
    public async Task<IActionResult> GetToken(int id)
    {
        var session = await _db.Sessions.FindAsync(id);
        if (session == null || !session.IsActive)
            return NotFound("Session not found or ended.");

        if (DateTime.UtcNow >= session.TokenExpiresAt)
        {
            session.CurrentToken = NewToken();
            session.TokenExpiresAt = DateTime.UtcNow.AddSeconds(10);
            await _db.SaveChangesAsync();
        }
        return Ok(new { session.CurrentToken, session.TokenExpiresAt });
    }

    [HttpPost("{id}/end")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> End(int id)
    {
        var session = await _db.Sessions.FindAsync(id);
        if (session == null) return NotFound();
        if (session.TeacherId != CurrentUserId) return Forbid();

        session.IsActive = false;
        session.EndedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { session.Id, Ended = true });
    }

    private static string NewToken() =>
        Guid.NewGuid().ToString("N")[..12].ToUpper();
}
