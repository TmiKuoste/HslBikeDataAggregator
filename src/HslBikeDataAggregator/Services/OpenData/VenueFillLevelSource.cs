using System.Net.Http.Json;
using System.Text.Json;

using HslBikeDataAggregator.Configuration;

namespace HslBikeDataAggregator.Services.OpenData;

public sealed class VenueFillLevelSource(VenueFillLevelConfig config, HttpClient httpClient) : IOpenDataSource
{
    private static readonly Uri ApiUri = new("https://helsinki.jaskaretail.com/api/select/current_fill_level_ws");

    public string SourceId => config.SourceId;
    public string DisplayName => config.DisplayName;
    public double Lat => config.Lat;
    public double Lon => config.Lon;
    public string AttributionUrl => config.AttributionUrl;
    public string? Unit => config.Unit;
    public string? Description => config.Description;

    public async Task<double?> FetchAsync(CancellationToken cancellationToken)
    {
        var payload = new
        {
            location_id = config.LocationId,
            location_url_name = config.LocationUrlName,
            current_fill_level_type = "SINGLE",
            module_type = "current_fill_level"
        };

        using var response = await httpClient.PostAsJsonAsync(ApiUri, payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var element = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        if (element.TryGetProperty("isError", out var isError) && isError.GetBoolean())
            return null;

        return element.TryGetProperty("result", out var result)
            && result.TryGetProperty("fill_level", out var fillLevel)
            && fillLevel.TryGetInt32(out var amount)
            ? (double)amount
            : null;
    }
}
