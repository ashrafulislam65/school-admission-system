using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Services;

public static class HoldMapper
{
    // Moyad shesh hole database e HELD thakleo API EXPIRED dekhabe
    public static string EffectiveStatus(Hold hold, DateTime now)
    {
        if (hold.Status == HoldStatus.Held && hold.ExpiresAt <= now)
            return "EXPIRED";
        return hold.Status.ToString().ToUpperInvariant();
    }
}