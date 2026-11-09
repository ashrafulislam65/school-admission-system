using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SchoolAdmission.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    public ApiFactory()
    {
        // Test alada database e chole, dev database chhoy na
        var conn = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
            ?? "Host=localhost;Port=5433;Database=school_admission_test;Username=postgres;Password=postgres";
        Environment.SetEnvironmentVariable("DB_CONNECTION", conn);
    }
}

public static class ApiHelpers
{
    // 17 ongker student id
    public static string StudentId(int n) => $"2015269{n:D10}";

    public static async Task<string> CreateClassAsync(this HttpClient c, string name, int seats)
    {
        var res = await c.PostAsJsonAsync("/classes", new { name, totalSeats = seats });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("id").GetString()!;
    }

    public static Task<HttpResponseMessage> ApplyAsync(this HttpClient c, string classId, string studentId) =>
        Send(c, HttpMethod.Post, $"/classes/{classId}/holds", studentId);

    public static Task<HttpResponseMessage> ConfirmAsync(this HttpClient c, string holdId, string studentId) =>
        Send(c, HttpMethod.Post, $"/holds/{holdId}/confirm", studentId);

    public static Task<HttpResponseMessage> CancelAsync(this HttpClient c, string holdId, string studentId) =>
        Send(c, HttpMethod.Delete, $"/holds/{holdId}", studentId);

    public static async Task<JsonElement> GetClassAsync(this HttpClient c, string classId) =>
        await c.GetFromJsonAsync<JsonElement>($"/classes/{classId}");

    public static async Task<string> GetHoldStatusAsync(this HttpClient c, string holdId)
    {
        var json = await c.GetFromJsonAsync<JsonElement>($"/holds/{holdId}");
        return json.GetProperty("status").GetString()!;
    }

    public static async Task<string> HoldIdAsync(this HttpResponseMessage res)
    {
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("holdId").GetString()!;
    }

    private static Task<HttpResponseMessage> Send(HttpClient c, HttpMethod method, string url, string studentId)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Add("X-Student-Id", studentId);
        return c.SendAsync(req);
    }
}