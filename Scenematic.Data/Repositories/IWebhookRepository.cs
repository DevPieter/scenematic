using Scenematic.Data.Models.DTOs.Webhooks;
using Scenematic.Data.Models.Entities.Webhooks;

namespace Scenematic.Data.Repositories;

public interface IWebhookRepository
{
    Task<Webhook> RegisterAsync(
        RegisterWebhookDto dto
    );
}