using System.Net.Http.Json;
using System.Text.Json;

namespace HealthTrack.Services;

// Optional AI client used by the chatbot when no FAQ answer matches.
public class ChatAiClient(HttpClient http, IConfiguration configuration, ILogger<ChatAiClient> logger)
{
    private const string SystemPrompt = "You are the HealthTrack help assistant. Answer only questions about how to use the HealthTrack "
        + "fitness and health journal app (workouts, meals, weight metrics, progress, plans, appointments, notifications, account settings). "
        + "Keep answers under 80 words. Never give medical, diagnostic, medication, nutrition or treatment advice; for those, tell the user "
        + "to speak with a healthcare professional. If the question is unrelated to HealthTrack, say you can only help with the app.";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(configuration["Anthropic:ApiKey"]);

    // Sends the question to the AI service, or returns null when it is not configured or fails.
    public async Task<string?> AskAsync(string question, CancellationToken cancellationToken)
    {
        if (!IsConfigured) return null;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            request.Headers.Add("x-api-key", configuration["Anthropic:ApiKey"]);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = JsonContent.Create(new
            {
                model = configuration["Anthropic:Model"] ?? "claude-haiku-5-5",
                max_tokens = 300,
                system = SystemPrompt,
                messages = new[] { new { role = "user", content = question } }
            });
            using var response = await http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            return document.RootElement.GetProperty("content")[0].GetProperty("text").GetString()?.Trim();
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException or KeyNotFoundException or InvalidOperationException)
        {
            logger.LogWarning(exception, "AI chat answer failed");
            return null;
        }
    }
}
