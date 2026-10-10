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
[Authorize(Roles = "Student")]
public class AttendanceController : ControllerBase
{
    private readonly AppDbContext _db;
    public AttendanceController(AppDbContext db) => _db = db;

    private int CurrentUserId =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpPost("mark")]
    public async Task<IActionResult> Mark(MarkAttendanceRequest req)
    {
        var session = await _db.Sessions.FindAsync(req.SessionId);
        if (session == null || !session.IsActive)
            return BadRequest("Session not found or ended.");

        if (session.CourseSectionId.HasValue)
        {
            var enrolled = await _db.Enrollments.AnyAsync(e =>
                e.CourseSectionId == session.CourseSectionId.Value &&
                e.StudentId == CurrentUserId);
            if (!enrolled)
                return BadRequest("You are not enrolled in this class.");
        }

        if (DateTime.UtcNow > session.TokenExpiresAt ||
            session.CurrentToken != req.Token)
            return BadRequest("Invalid or expired token. Scan the current QR.");

        if (await _db.Records.AnyAsync(r =>
                r.SessionId == req.SessionId && r.StudentId == CurrentUserId))
            return BadRequest("Attendance already marked.");

        var record = new AttendanceRecord
        {
            SessionId = req.SessionId,
            StudentId = CurrentUserId,
            ScannedAt = DateTime.UtcNow,
            IsVerified = true
        };
        _db.Records.Add(record);
        await _db.SaveChangesAsync();
        return Ok(new { record.Id, Marked = true, record.ScannedAt });
    }
}
