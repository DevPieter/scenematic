using MassTransit;
using Scenematic.Queue.Contracts.GenerateCoverJobs;

namespace Service.Scenematic.Consumers;

public class GenerateCoverJobFailedConsumer(
    ILogger<GenerateCoverJobFailedConsumer> logger
)
    : IConsumer<GenerateCoverJobFailed>
{
    public async Task Consume(ConsumeContext<GenerateCoverJobFailed> context)
    {
        logger.LogInformation("Generate cover job failed");
    }
}