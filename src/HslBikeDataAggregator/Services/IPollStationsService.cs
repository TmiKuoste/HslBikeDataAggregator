namespace HslBikeDataAggregator.Services;

public interface IPollStationsService
{
    Task<PollStationsResult> PollAsync(CancellationToken cancellationToken);
}
