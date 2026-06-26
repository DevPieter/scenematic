using MassTransit;
using Scenematic.Queue.Contracts.GenerateCoverJobs;

namespace Service.Scenematic.Consumers;

public class GenerateCoverJobCompletedConsumer(
    ILogger<GenerateCoverJobCompletedConsumer> logger
)
    : IConsumer<GenerateCoverJobCompleted>
{
    public async Task Consume(ConsumeContext<GenerateCoverJobCompleted> context)
    {
        logger.LogInformation(
            "Generate cover job completed: {JobId}, MainCoverPath: {MainCoverPath}, DebounceCoverPath: {DebounceCoverPath}, ProcessingCoverPath: {ProcessingCoverPath}",
            context.Message.JobId,
            context.Message.MainCoverPath,
            context.Message.DebounceCoverPath,
            context.Message.ProcessingCoverPath
        );
    }
}