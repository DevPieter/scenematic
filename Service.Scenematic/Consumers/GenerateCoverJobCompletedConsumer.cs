using MassTransit;
using Scenematic.Data.Repositories;
using Scenematic.Logic.Services;
using Scenematic.Queue.Contracts.GenerateCoverJobs;

namespace Service.Scenematic.Consumers;

public class GenerateCoverJobCompletedConsumer(
    ICoverJobRepository repository,
    WebhookService webhookService,
    ILogger<GenerateCoverJobCompletedConsumer> logger
)
    : IConsumer<GenerateCoverJobCompleted>
{
    // TODO - Generalize event, one event for both success and failure

    public async Task Consume(ConsumeContext<GenerateCoverJobCompleted> context)
    {
        logger.LogInformation(
            "Generate cover job completed: {JobId}, MainCoverPath: {MainCoverPath}, DebounceCoverPath: {DebounceCoverPath}, ProcessingCoverPath: {ProcessingCoverPath}",
            context.Message.JobId,
            context.Message.MainCoverPath,
            context.Message.DebounceCoverPath,
            context.Message.ProcessingCoverPath
        );

        var webhooks = await repository.GetWebhooksAsync(context.Message.JobId);
        foreach (var webhook in webhooks)
        {
            try
            {
                // TODO - Move payload to Object
                var result = await webhookService.SendWebhookAsync(webhook, new
                {
                    context.Message.JobId,
                    context.Message.MainCoverPath,
                    context.Message.DebounceCoverPath,
                    context.Message.ProcessingCoverPath
                });

                logger.LogInformation(
                    "Webhook sent for job id {JobId} to {WebhookUrl}. Result: {Result}",
                    context.Message.JobId,
                    webhook.Url,
                    result
                );
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to send webhook for job id {JobId} to {WebhookUrl}",
                    context.Message.JobId,
                    webhook.Url
                );
            }
        }
    }
}