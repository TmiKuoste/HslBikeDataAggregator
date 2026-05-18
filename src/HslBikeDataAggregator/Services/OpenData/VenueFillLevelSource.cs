using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

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

        // API may return an object or a single-element array
        var obj = element.ValueKind == JsonValueKind.Array && element.GetArrayLength() > 0
            ? element[0]
            : element;

        return obj.TryGetProperty("currentAmount", out var prop) && prop.TryGetInt32(out var amount)
            ? (double)amount
            : null;
    }
}
