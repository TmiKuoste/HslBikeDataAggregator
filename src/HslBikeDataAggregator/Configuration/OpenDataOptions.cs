namespace HslBikeDataAggregator.Configuration;

public sealed class OpenDataOptions
{
    public int HistoryLimit { get; set; } = 60;
    public List<VenueFillLevelConfig> VenueFillLevelSources { get; set; } = [];
}

public sealed class VenueFillLevelConfig
{
    public required string SourceId { get; set; }
    public required string DisplayName { get; set; }
    public required double Lat { get; set; }
    public required double Lon { get; set; }
    public required string AttributionUrl { get; set; }
    public required string LocationId { get; set; }
    public required string LocationUrlName { get; set; }
    public string? Unit { get; set; }
    public string? Description { get; set; }
}
