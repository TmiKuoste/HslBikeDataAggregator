using HslBikeDataAggregator.Configuration;
using HslBikeDataAggregator.Models.OpenData;
using HslBikeDataAggregator.Services.OpenData;
using HslBikeDataAggregator.Storage;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace HslBikeDataAggregator.Tests.Services;

public sealed class OpenDataPollServiceTests
{
    [Fact]
    public async Task PollAsync_AppendsValueAndTrimsHistory()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var timestamp = new DateTimeOffset(2026, 5, 1, 10, 15, 0, TimeSpan.Zero);

        var source = CreateSource("uimastadion");
        source.Setup(s => s.FetchAsync(cancellationToken)).ReturnsAsync(45.0);

        var existing = new OpenDataTimeSeries
        {
            SourceId = "uimastadion",
            DisplayName = "Uimastadion",
            Lat = 60.1857,
            Lon = 24.9282,
            AttributionUrl = "https://example.com",
            Timestamps =
            [
                new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 5, 1, 10, 5, 0, TimeSpan.Zero)
            ],
            Values = [30.0, 35.0]
        };

        var blobStorage = new Mock<IOpenDataBlobStorage>();
        blobStorage.Setup(s => s.GetOpenDataTimeSeriesAsync("uimastadion", cancellationToken)).ReturnsAsync(existing);

        OpenDataTimeSeries? written = null;
        blobStorage
            .Setup(s => s.WriteOpenDataTimeSeriesAsync(It.IsAny<OpenDataTimeSeries>(), cancellationToken))
            .Callback<OpenDataTimeSeries, CancellationToken>((ts, _) => written = ts)
            .Returns(Task.CompletedTask);

        var service = CreateService([source.Object], blobStorage.Object, historyLimit: 2, timestamp);

        var result = await service.PollAsync(cancellationToken);

        Assert.NotNull(written);
        Assert.Equal(2, written!.Timestamps.Count);
        Assert.Equal(2, written.Values.Count);
        Assert.Equal(timestamp, written.Timestamps[^1]);
        Assert.Equal(35.0, written.Values[0]);
        Assert.Equal(45.0, written.Values[^1]);

        Assert.Equal(timestamp, result.Timestamp);
        Assert.Equal(1, result.SourceCount);
        Assert.Equal(1, result.SuccessCount);
    }

    [Fact]
    public async Task PollAsync_RecordsSentinel_WhenFetchThrows()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var timestamp = new DateTimeOffset(2026, 5, 1, 10, 15, 0, TimeSpan.Zero);

        var source = CreateSource("uimastadion");
        source.Setup(s => s.FetchAsync(cancellationToken)).ThrowsAsync(new HttpRequestException("timeout"));

        var blobStorage = CreateEmptyBlobStorage();

        OpenDataTimeSeries? written = null;
        blobStorage
            .Setup(s => s.WriteOpenDataTimeSeriesAsync(It.IsAny<OpenDataTimeSeries>(), cancellationToken))
            .Callback<OpenDataTimeSeries, CancellationToken>((ts, _) => written = ts)
            .Returns(Task.CompletedTask);

        var service = CreateService([source.Object], blobStorage.Object, historyLimit: 60, timestamp);

        var result = await service.PollAsync(cancellationToken);

        Assert.NotNull(written);
        Assert.Equal(-1.0, Assert.Single(written!.Values));
        Assert.Equal(1, result.SourceCount);
        Assert.Equal(0, result.SuccessCount);
    }

    [Fact]
    public async Task PollAsync_RecordsSentinel_WhenSourceReturnsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var timestamp = new DateTimeOffset(2026, 5, 1, 10, 15, 0, TimeSpan.Zero);

        var source = CreateSource("uimastadion");
        source.Setup(s => s.FetchAsync(cancellationToken)).ReturnsAsync((double?)null);

        var blobStorage = CreateEmptyBlobStorage();

        OpenDataTimeSeries? written = null;
        blobStorage
            .Setup(s => s.WriteOpenDataTimeSeriesAsync(It.IsAny<OpenDataTimeSeries>(), cancellationToken))
            .Callback<OpenDataTimeSeries, CancellationToken>((ts, _) => written = ts)
            .Returns(Task.CompletedTask);

        var service = CreateService([source.Object], blobStorage.Object, historyLimit: 60, timestamp);

        var result = await service.PollAsync(cancellationToken);

        Assert.NotNull(written);
        Assert.Equal(-1.0, Assert.Single(written!.Values));
        Assert.Equal(1, result.SourceCount);
        Assert.Equal(0, result.SuccessCount);
    }

    [Fact]
    public async Task PollAsync_OneSourceFailureDoesNotAffectOtherSources()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var timestamp = new DateTimeOffset(2026, 5, 1, 10, 15, 0, TimeSpan.Zero);

        var source1 = CreateSource("source-a");
        source1.Setup(s => s.FetchAsync(cancellationToken)).ThrowsAsync(new HttpRequestException("error"));

        var source2 = CreateSource("source-b");
        source2.Setup(s => s.FetchAsync(cancellationToken)).ReturnsAsync(100.0);

        var writtenBySource = new Dictionary<string, OpenDataTimeSeries>();
        var blobStorage = new Mock<IOpenDataBlobStorage>();
        blobStorage
            .Setup(s => s.GetOpenDataTimeSeriesAsync(It.IsAny<string>(), cancellationToken))
            .ReturnsAsync((OpenDataTimeSeries?)null);
        blobStorage
            .Setup(s => s.WriteOpenDataTimeSeriesAsync(It.IsAny<OpenDataTimeSeries>(), cancellationToken))
            .Callback<OpenDataTimeSeries, CancellationToken>((ts, _) => writtenBySource[ts.SourceId] = ts)
            .Returns(Task.CompletedTask);

        var service = CreateService([source1.Object, source2.Object], blobStorage.Object, historyLimit: 60, timestamp);

        var result = await service.PollAsync(cancellationToken);

        Assert.Equal(2, writtenBySource.Count);
        Assert.Equal(-1.0, Assert.Single(writtenBySource["source-a"].Values));
        Assert.Equal(100.0, Assert.Single(writtenBySource["source-b"].Values));
        Assert.Equal(2, result.SourceCount);
        Assert.Equal(1, result.SuccessCount);
    }

    [Fact]
    public async Task PollAsync_RefreshesMetadata_WhenWritingTimeSeries()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var timestamp = new DateTimeOffset(2026, 5, 1, 10, 15, 0, TimeSpan.Zero);

        var source = new Mock<IOpenDataSource>();
        source.SetupGet(s => s.SourceId).Returns("uimastadion");
        source.SetupGet(s => s.DisplayName).Returns("Uimastadion Updated");
        source.SetupGet(s => s.Lat).Returns(60.19);
        source.SetupGet(s => s.Lon).Returns(24.93);
        source.SetupGet(s => s.AttributionUrl).Returns("https://new-attribution.com");
        source.SetupGet(s => s.Unit).Returns("visitors");
        source.SetupGet(s => s.Description).Returns("Live visitor count");
        source.Setup(s => s.FetchAsync(cancellationToken)).ReturnsAsync(50.0);

        var existing = OpenDataTimeSeries.CreateEmpty("uimastadion", "Old Name", 60.0, 25.0, "https://old.com");

        var blobStorage = new Mock<IOpenDataBlobStorage>();
        blobStorage.Setup(s => s.GetOpenDataTimeSeriesAsync("uimastadion", cancellationToken)).ReturnsAsync(existing);

        OpenDataTimeSeries? written = null;
        blobStorage
            .Setup(s => s.WriteOpenDataTimeSeriesAsync(It.IsAny<OpenDataTimeSeries>(), cancellationToken))
            .Callback<OpenDataTimeSeries, CancellationToken>((ts, _) => written = ts)
            .Returns(Task.CompletedTask);

        var service = CreateService([source.Object], blobStorage.Object, historyLimit: 60, timestamp);
        await service.PollAsync(cancellationToken);

        Assert.NotNull(written);
        Assert.Equal("Uimastadion Updated", written!.DisplayName);
        Assert.Equal(60.19, written.Lat);
        Assert.Equal(24.93, written.Lon);
        Assert.Equal("https://new-attribution.com", written.AttributionUrl);
        Assert.Equal("visitors", written.Unit);
        Assert.Equal("Live visitor count", written.Description);
    }

    [Fact]
    public async Task PollAsync_LeavesUnitAndDescriptionNull_WhenSourceDoesNotProvideThem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var timestamp = new DateTimeOffset(2026, 5, 1, 10, 15, 0, TimeSpan.Zero);

        var source = CreateSource("legacy-source");
        source.Setup(s => s.FetchAsync(cancellationToken)).ReturnsAsync(10.0);

        var blobStorage = CreateEmptyBlobStorage();

        OpenDataTimeSeries? written = null;
        blobStorage
            .Setup(s => s.WriteOpenDataTimeSeriesAsync(It.IsAny<OpenDataTimeSeries>(), cancellationToken))
            .Callback<OpenDataTimeSeries, CancellationToken>((ts, _) => written = ts)
            .Returns(Task.CompletedTask);

        var service = CreateService([source.Object], blobStorage.Object, historyLimit: 60, timestamp);
        await service.PollAsync(cancellationToken);

        Assert.NotNull(written);
        Assert.Null(written!.Unit);
        Assert.Null(written.Description);
    }

    private static Mock<IOpenDataSource> CreateSource(string sourceId)
    {
        var source = new Mock<IOpenDataSource>();
        source.SetupGet(s => s.SourceId).Returns(sourceId);
        source.SetupGet(s => s.DisplayName).Returns("Display " + sourceId);
        source.SetupGet(s => s.Lat).Returns(60.0);
        source.SetupGet(s => s.Lon).Returns(25.0);
        source.SetupGet(s => s.AttributionUrl).Returns("https://example.com/" + sourceId);
        return source;
    }

    private static Mock<IOpenDataBlobStorage> CreateEmptyBlobStorage()
    {
        var blobStorage = new Mock<IOpenDataBlobStorage>();
        blobStorage
            .Setup(s => s.GetOpenDataTimeSeriesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpenDataTimeSeries?)null);
        return blobStorage;
    }

    private static OpenDataPollService CreateService(
        IReadOnlyList<IOpenDataSource> sources,
        IOpenDataBlobStorage blobStorage,
        int historyLimit,
        DateTimeOffset timestamp) =>
        new(sources,
            blobStorage,
            Options.Create(new OpenDataOptions { HistoryLimit = historyLimit }),
            new FixedTimeProvider(timestamp),
            NullLogger<OpenDataPollService>.Instance);

    private sealed class FixedTimeProvider(DateTimeOffset timestamp) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => timestamp;
    }
}
