using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolAdmission.Api.Data;
using Xunit;

namespace SchoolAdmission.Tests;

public class HoldRulesTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task SameStudent_CannotHoldTwiceInOneClass()
    {
        var classId = await _client.CreateClassAsync("Duplicate class", 5);
        var student = ApiHelpers.StudentId(1);

        Assert.Equal(HttpStatusCode.Created, (await _client.ApplyAsync(classId, student)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.ApplyAsync(classId, student)).StatusCode);
    }

    [Fact]
    public async Task Student_CannotConfirmSomeoneElsesHold()
    {
        var classId = await _client.CreateClassAsync("Owner class", 5);
        var holdId = await (await _client.ApplyAsync(classId, ApiHelpers.StudentId(1))).HoldIdAsync();

        var res = await _client.ConfirmAsync(holdId, ApiHelpers.StudentId(2));

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal("HELD", await _client.GetHoldStatusAsync(holdId));
    }

    [Fact]
    public async Task CancelledHold_FreesTheSeat()
    {
        var classId = await _client.CreateClassAsync("Cancel class", 1);
        var student = ApiHelpers.StudentId(1);
        var holdId = await (await _client.ApplyAsync(classId, student)).HoldIdAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await _client.CancelAsync(holdId, student)).StatusCode);

        var cls = await _client.GetClassAsync(classId);
        Assert.Equal(1, cls.GetProperty("availableSeats").GetInt32());
        Assert.Equal(HttpStatusCode.Created,
            (await _client.ApplyAsync(classId, ApiHelpers.StudentId(2))).StatusCode);
    }

    [Fact]
    public async Task ExpiredHold_FreesSeatAndCannotBeConfirmed()
    {
        var classId = await _client.CreateClassAsync("Expiry class", 1);
        var student = ApiHelpers.StudentId(1);
        var holdId = await (await _client.ApplyAsync(classId, student)).HoldIdAsync();

        // Moyad shesh howar obostha: database e expiresAt ke ager shomoy kori
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hold = await db.Holds.SingleAsync(h => h.Id == holdId);
            hold.ExpiresAt = DateTime.UtcNow.AddSeconds(-5);
            await db.SaveChangesAsync();
        }

        Assert.Equal("EXPIRED", await _client.GetHoldStatusAsync(holdId));
        var cls = await _client.GetClassAsync(classId);
        Assert.Equal(1, cls.GetProperty("availableSeats").GetInt32());
        Assert.Equal(HttpStatusCode.Conflict, (await _client.ConfirmAsync(holdId, student)).StatusCode);
        Assert.Equal(HttpStatusCode.Created,
            (await _client.ApplyAsync(classId, ApiHelpers.StudentId(2))).StatusCode);
    }
}