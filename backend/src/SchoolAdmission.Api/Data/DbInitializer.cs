namespace SchoolAdmission.Api.Data;

public static class DbInitializer
{
    // Docker e Postgres ready hote kichu second lage, tai retry kori
    public static async Task InitializeAsync(IServiceProvider services)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.EnsureCreatedAsync();
                return;
            }
            catch (Exception) when (attempt < 15)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
    }
}