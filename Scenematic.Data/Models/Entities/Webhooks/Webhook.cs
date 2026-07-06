using System.ComponentModel.DataAnnotations;

namespace Scenematic.Data.Models.Entities.Webhooks;

public class Webhook
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [MaxLength(256)]
    public string Url { get; init; }

    [MaxLength(256)] // TODO - Check length
    public string Secret { get; init; } = null!;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    private Webhook()
    {
    }

    public Webhook(
        string url,
        string secret
    )
    {
        Url = url;
        Secret = secret;
    }
}