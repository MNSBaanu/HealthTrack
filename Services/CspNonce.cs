namespace HealthTrack.Services;

// Per-request nonce that allows approved inline scripts under the Content Security Policy.
public static class CspNonce
{
    public const string Key = "CspNonce";

    // Returns the nonce for the current request.
    public static string Get(HttpContext context) => context.Items[Key] as string ?? string.Empty;
}
