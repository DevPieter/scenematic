using MassTransit;
using Scenematic.Data.Repositories;
using Scenematic.Logic.Services;
using Scenematic.Queue.Contracts.GenerateCoverJobs;

namespace Service.Scenematic.Consumers;

public class GenerateCoverJobResultConsumer(
    ICoverJobRepository repository,
    CoverGeneratorJobService service,
    WebhookService webhookService,
    ILogger<GenerateCoverJobResultConsumer> logger
) : IConsumer<GenerateCoverResult>
{
    public async Task Consume(ConsumeContext<GenerateCoverResult> context)
    {
        if (!context.Message.Success) return;

        var jobResult = await service.GetByIdAsync(
            jobId: context.Message.JobId,
            includeResults: true
        );
        if (jobResult.IsError || jobResult.Data is null) return;

        var success = jobResult.Data.SuccessResult;
        if (success is null) return;

        logger.LogInformation(
            "Generate cover job completed: {JobId}, MainCoverPath: {MainCoverPath}",
            context.Message.JobId,
            success.MainCoverPath
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
                    success.MainCoverPath
                    // success.DebounceCoverPath,
                    // success.ProcessingCoverPath
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