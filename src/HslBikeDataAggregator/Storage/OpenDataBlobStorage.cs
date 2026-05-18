using System.Text.Json;

using Azure.Storage.Blobs;

using HslBikeDataAggregator.Models.OpenData;

namespace HslBikeDataAggregator.Storage;

public sealed class OpenDataBlobStorage(BlobContainerClient blobContainerClient) : IOpenDataBlobStorage
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<OpenDataTimeSeries?> GetOpenDataTimeSeriesAsync(string sourceId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);

        var content = await DownloadBlobContentAsync(BikeDataBlobNames.OpenDataTimeSeries(sourceId), cancellationToken);
        return content?.ToObjectFromJson<OpenDataTimeSeries>(SerializerOptions);
    }

    public async Task WriteOpenDataTimeSeriesAsync(OpenDataTimeSeries timeSeries, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(timeSeries);

        await blobContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        await blobContainerClient
            .GetBlobClient(BikeDataBlobNames.OpenDataTimeSeries(timeSeries.SourceId))
            .UploadAsync(BinaryData.FromObjectAsJson(timeSeries, SerializerOptions), overwrite: true, cancellationToken);
    }

    private async Task<BinaryData?> DownloadBlobContentAsync(string blobName, CancellationToken cancellationToken)
    {
        await blobContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        try
        {
            var response = await blobContainerClient
                .GetBlobClient(blobName)
                .DownloadContentAsync(cancellationToken);

            return response.Value.Content;
        }
        catch (Azure.RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }
}
