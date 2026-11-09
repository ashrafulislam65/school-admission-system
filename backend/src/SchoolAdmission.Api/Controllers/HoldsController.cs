using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Helpers;
using SchoolAdmission.Api.Models;
using SchoolAdmission.Api.Services;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
public class HoldsController(AppDbContext db, HoldSettings settings) : ControllerBase
{
    [HttpPost("classes/{classId}/holds")]
    public async Task<IActionResult> Create(string classId)
    {
        if (!StudentIdHeader.TryGet(Request, out var studentId))
            return BadRequest(new { error = "X-Student-Id header must be the student's 17-digit birth registration number." });

        var exists = await db.Classes.AsNoTracking().AnyAsync(c => c.Id == classId);
        if (!exists)
            return NotFound(new { error = "Class not found." });

        await using var tx = await db.Database.BeginTransactionAsync();

        // Ei class er row lock: ek shomoy ekta request-i seat gonte parbe
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"Classes\" WHERE \"Id\" = {classId} FOR UPDATE");

        var totalSeats = await db.Classes.AsNoTracking()
            .Where(c => c.Id == classId)
            .Select(c => c.TotalSeats)
            .FirstAsync();

        var now = DateTime.UtcNow;
        var active = db.Holds.Where(h => h.ClassId == classId
            && (h.Status == HoldStatus.Confirmed
                || (h.Status == HoldStatus.Held && h.ExpiresAt > now)));

        if (await active.AnyAsync(h => h.StudentId == studentId))
            return Conflict(new { error = "You already have an active application or a confirmed admission in this class." });

        if (await active.CountAsync() >= totalSeats)
            return Conflict(new { error = "No seats are available in this class." });

        var hold = new Hold
        {
            Id = "h_" + Guid.NewGuid().ToString("N")[..8],
            ClassId = classId,
            StudentId = studentId,
            Status = HoldStatus.Held,
            CreatedAt = now,
            ExpiresAt = now.AddSeconds(settings.DurationSeconds)
        };
        db.Holds.Add(hold);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return Created($"/holds/{hold.Id}", new HoldCreatedResponse(
            hold.Id, hold.ClassId, hold.StudentId,
            HoldMapper.EffectiveStatus(hold, now), hold.ExpiresAt));
    }
}