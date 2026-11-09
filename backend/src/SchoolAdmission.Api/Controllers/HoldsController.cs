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
        private const string BadStudentIdMessage =
        "X-Student-Id header must be the student's 17-digit birth registration number.";
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
    [HttpGet("holds/{holdId}")]
        public async Task<IActionResult> GetById(string holdId)
        {
            var hold = await db.Holds.AsNoTracking().FirstOrDefaultAsync(h => h.Id == holdId);
            if (hold is null)
                return NotFound(new { error = "Application not found." });

            return Ok(new HoldResponse(
                hold.Id, hold.ClassId,
                HoldMapper.EffectiveStatus(hold, DateTime.UtcNow), hold.ExpiresAt));
        }
            [HttpPost("holds/{holdId}/confirm")]
    public async Task<IActionResult> Confirm(string holdId)
    {
        if (!StudentIdHeader.TryGet(Request, out var studentId))
            return BadRequest(new { error = BadStudentIdMessage });

        await using var tx = await db.Database.BeginTransactionAsync();

        // Ei hold er row lock: confirm ar cancel ek shathe elew ekta-i jitbe
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"Holds\" WHERE \"Id\" = {holdId} FOR UPDATE");

        var hold = await db.Holds.FirstOrDefaultAsync(h => h.Id == holdId);
        if (hold is null)
            return NotFound(new { error = "Application not found." });
        if (hold.StudentId != studentId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { error = "This application belongs to another student." });

        // Abar confirm korle kichu bodlay na, tai confirmedSeats ekbar-i bare
        if (hold.Status == HoldStatus.Confirmed)
            return Ok(new ConfirmResponse(hold.Id, "CONFIRMED"));
        if (hold.Status == HoldStatus.Cancelled)
            return Conflict(new { error = "This application was cancelled." });

        var now = DateTime.UtcNow;
        if (hold.Status == HoldStatus.Expired || hold.ExpiresAt <= now)
        {
            hold.Status = HoldStatus.Expired;
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return Conflict(new { error = "The payment time for this application has expired." });
        }

        hold.Status = HoldStatus.Confirmed;
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new ConfirmResponse(hold.Id, "CONFIRMED"));
    }
}