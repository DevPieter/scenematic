using Microsoft.AspNetCore.Mvc;
using Scenematic.Data.Models.DTOs.Cover;
using Scenematic.Logic.Models.Arguments.CoverJobArgs;
using Scenematic.Logic.Services;
using Service.Scenematic.Models.Requests;

namespace Service.Scenematic.Controllers;

[ApiController, Route("api/v1/cover")]
public class CoverController(
    CoverGeneratorJobService jobService
) : ControllerBase
{
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateCoverAsync(GenerateCoverRequest request)
    {
        var result = await jobService.RegisterOrGetAsync(new RegisterCoverJobArgs(
            Title: request.Title,
            Artist: request.Artist,
            CoverImageUrl: request.CoverImageUrl,
            CoverVotes: request.CoverVotes.Select(voteRequest => new CoverVoteDto(
                VoteLevel: voteRequest.VoteLevel,
                AvatarUrl: voteRequest.AvatarUrl
            )).ToList(),
            CallbackUrl: request.CallbackUrl
        ));

        if (result.IsError || result.Data is null) return Problem(result.Message);

        var job = result.Data.Job;
        var webhook = result.Data.Webhook;

        return Ok(new GenerateCoverResponse(
            JobId: job.Id,
            State: job.State,
            SuccessResult: job.SuccessResult is null
                ? null
                : new GenerateCoverSuccessResultResponse(
                    MainCoverPath: job.SuccessResult.MainCoverPath,
                    DebounceCoverPath: job.SuccessResult.DebounceCoverPath,
                    ProcessingCoverPath: job.SuccessResult.ProcessingCoverPath,
                    CompletedAt: job.CompletedAt!.Value
                ),
            FailureResult: job.FailureResult is null
                ? null
                : new GenerateCoverFailureResultResponse(
                    ErrorMessage: job.FailureResult.ErrorMessage,
                    FailedAt: job.FailedAt!.Value
                ),
            Callback: webhook is null
                ? null
                : new GenerateCoverCallbackResponse(
                    Url: webhook.Url,
                    Secret: webhook.Secret
                )
        ));
    }
}