using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("classes")]
public class ClassesController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClassRequest req)
    {
        var name = req.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 200)
            return BadRequest(new { error = "Class name is required (max 200 characters)." });
        if (req.TotalSeats is null or <= 0)
            return BadRequest(new { error = "totalSeats must be a positive number." });

        var cls = new SchoolClass
        {
            Id = "c_" + Guid.NewGuid().ToString("N")[..8],
            Name = name,
            TotalSeats = req.TotalSeats.Value
        };
        db.Classes.Add(cls);
        await db.SaveChangesAsync();

        return Created($"/classes/{cls.Id}", new ClassCreatedResponse(cls.Id, cls.Name, cls.TotalSeats));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var classes = await db.Classes.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        var counts = await GetCountsAsync();
        return Ok(classes.Select(c => ToResponse(c, counts)));
    }

    [HttpGet("{classId}")]
    public async Task<IActionResult> GetById(string classId)
    {
        var cls = await db.Classes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == classId);
        if (cls is null)
            return NotFound(new { error = "Class not found." });

        var counts = await GetCountsAsync(classId);
        return Ok(ToResponse(cls, counts));
    }

    // Moyad shesh howa HELD seat gulo gonar bhetor porbe na, tai seat nijei phire ashe
    private async Task<Dictionary<string, (int Held, int Confirmed)>> GetCountsAsync(string? classId = null)
    {
        var now = DateTime.UtcNow;
        var query = db.Holds.AsNoTracking()
            .Where(h => h.Status == HoldStatus.Confirmed
                     || (h.Status == HoldStatus.Held && h.ExpiresAt > now));
        if (classId is not null)
            query = query.Where(h => h.ClassId == classId);

        var rows = await query
            .GroupBy(h => new { h.ClassId, h.Status })
            .Select(g => new { g.Key.ClassId, g.Key.Status, Count = g.Count() })
            .ToListAsync();

        var result = new Dictionary<string, (int Held, int Confirmed)>();
        foreach (var r in rows)
        {
            result.TryGetValue(r.ClassId, out var c);
            result[r.ClassId] = r.Status == HoldStatus.Held
                ? (c.Held + r.Count, c.Confirmed)
                : (c.Held, c.Confirmed + r.Count);
        }
        return result;
    }

    private static ClassResponse ToResponse(SchoolClass c, Dictionary<string, (int Held, int Confirmed)> counts)
    {
        counts.TryGetValue(c.Id, out var n);
        return new ClassResponse(c.Id, c.Name, c.TotalSeats, n.Held, n.Confirmed,
            c.TotalSeats - n.Held - n.Confirmed);
    }
}