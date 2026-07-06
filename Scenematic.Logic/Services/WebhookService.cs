using System.Net.Http.Json;
using Scenematic.Common.Models.Common;
using Scenematic.Data.Models.Entities.Webhooks;

namespace Scenematic.Logic.Services;

public class WebhookService(HttpClient client)
{
    // TODO - The following needs to be added (to the project, not just this service):
    // - URL validation
    // - Webhook queuing
    // - Retry logic with exponential backoff

    public async Task<Result> SendWebhookAsync(Webhook webhook, object payload)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url);

            request.Content = JsonContent.Create(payload);
            request.Headers.Add("X-Webhook-Secret", webhook.Secret);

            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode) return Result.Success("Webhook sent successfully");

            var errorContent = await response.Content.ReadAsStringAsync();
            return Result.InternalError($"Failed to send webhook: {response.StatusCode}, {errorContent}");
        }
        catch (Exception ex)
        {
            return Result.InternalError($"Exception occurred while sending webhook: {ex.Message}");
        }
    }
}