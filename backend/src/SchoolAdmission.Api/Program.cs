using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration["DB_CONNECTION"]
    ?? "Host=localhost;Port=5432;Database=school_admission;Username=postgres;Password=postgres";

var holdSeconds = int.TryParse(builder.Configuration["HOLD_DURATION_SECONDS"], out var s) && s > 0
    ? s
    : 600;

builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddSingleton(new HoldSettings(holdSeconds));
builder.Services.AddControllers();
builder.Services.AddCors(o =>
    o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

await DbInitializer.InitializeAsync(app.Services);

app.UseCors();
app.MapControllers();
app.Run();