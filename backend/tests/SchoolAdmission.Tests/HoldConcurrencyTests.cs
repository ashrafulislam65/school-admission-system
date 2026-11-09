using System.Net;
using Xunit;

namespace SchoolAdmission.Tests;

public class HoldConcurrencyTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task ManyConcurrentHolds_NeverExceedTotalSeats()
    {
        const int seats = 5;
        const int requests = 60;
        var classId = await _client.CreateClassAsync("Concurrency class", seats);

        // 60 jon student ek shathe apply kore, seat matro 5ta
        var responses = await Task.WhenAll(Enumerable.Range(1, requests)
            .Select(i => Task.Run(() => _client.ApplyAsync(classId, ApiHelpers.StudentId(i)))));

        Assert.Equal(seats, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(requests - seats, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));

        var cls = await _client.GetClassAsync(classId);
        Assert.Equal(seats, cls.GetProperty("heldSeats").GetInt32());
        Assert.Equal(0, cls.GetProperty("availableSeats").GetInt32());
    }

    [Fact]
    public async Task LastSeat_OnlyOneOfManyConcurrentStudentsGetsIt()
    {
        var classId = await _client.CreateClassAsync("Last seat class", 1);

        var responses = await Task.WhenAll(Enumerable.Range(1, 30)
            .Select(i => Task.Run(() => _client.ApplyAsync(classId, ApiHelpers.StudentId(i)))));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(29, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task ConcurrentDuplicateConfirms_CountEachSeatOnce()
    {
        const int seats = 5;
        var classId = await _client.CreateClassAsync("Confirm class", seats);

        var applies = await Task.WhenAll(Enumerable.Range(1, 20).Select(async i =>
        {
            var student = ApiHelpers.StudentId(i);
            return (Student: student, Res: await _client.ApplyAsync(classId, student));
        }));
        var winners = applies.Where(a => a.Res.StatusCode == HttpStatusCode.Created).ToList();
        Assert.Equal(seats, winners.Count);

        // Proti hold er confirm duibar ek shathe pathai
        var confirmTasks = new List<Task<HttpResponseMessage>>();
        foreach (var w in winners)
        {
            var holdId = await w.Res.HoldIdAsync();
            confirmTasks.Add(Task.Run(() => _client.ConfirmAsync(holdId, w.Student)));
            confirmTasks.Add(Task.Run(() => _client.ConfirmAsync(holdId, w.Student)));
        }
        var confirms = await Task.WhenAll(confirmTasks);
        Assert.All(confirms, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));

        var cls = await _client.GetClassAsync(classId);
        Assert.Equal(seats, cls.GetProperty("confirmedSeats").GetInt32());
        Assert.Equal(0, cls.GetProperty("heldSeats").GetInt32());
        Assert.Equal(0, cls.GetProperty("availableSeats").GetInt32());
    }
}