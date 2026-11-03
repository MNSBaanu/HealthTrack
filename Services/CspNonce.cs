namespace HealthTrack.Services;

public static class CspNonce
{
    public const string Key = "CspNonce";

    public static string Get(HttpContext context) => context.Items[Key] as string ?? string.Empty;
}
