using HslBikeDataAggregator.Storage;

namespace HslBikeDataAggregator.Tests.Storage;

public sealed class BikeDataBlobNamesTests
{
    [Theory]
    [InlineData("smoove:001", "availability/smoove:001.json")]
    [InlineData("HSL-042", "availability/HSL-042.json")]
    [InlineData("station_99", "availability/station_99.json")]
    [InlineData("smoove:*159", "availability/smoove:*159.json")]
    [InlineData("smoove:*151", "availability/smoove:*151.json")]
    [InlineData("station@123", "availability/station@123.json")]
    [InlineData("station#456", "availability/station#456.json")]
    [InlineData("station(789)", "availability/station(789).json")]
    public void AvailabilityProfile_ValidStationId_ReturnsSafeBlobName(string stationId, string expected)
    {
        var result = BikeDataBlobNames.AvailabilityProfile(stationId);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("smoove:001", "destinations/smoove:001.json")]
    [InlineData("HSL-042", "destinations/HSL-042.json")]
    [InlineData("station_99", "destinations/station_99.json")]
    [InlineData("smoove:*159", "destinations/smoove:*159.json")]
    [InlineData("smoove:*151", "destinations/smoove:*151.json")]
    [InlineData("station@123", "destinations/station@123.json")]
    [InlineData("station#456", "destinations/station#456.json")]
    [InlineData("station(789)", "destinations/station(789).json")]
    public void DestinationProfile_ValidStationId_ReturnsSafeBlobName(string stationId, string expected)
    {
        var result = BikeDataBlobNames.DestinationProfile(stationId);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("../../../etc/passwd", "Station ID cannot contain path separators.")]
    [InlineData("station/../secret", "Station ID cannot contain path separators.")]
    [InlineData("station/id", "Station ID cannot contain path separators.")]
    [InlineData("station\\id", "Station ID cannot contain path separators.")]
    [InlineData("station\0id", "Station ID cannot contain control characters.")]
    [InlineData("station\nid", "Station ID cannot contain control characters.")]
    public void AvailabilityProfile_PathTraversalAttempt_ThrowsArgumentException(string maliciousId, string expectedMessage)
    {
        var exception = Assert.Throws<ArgumentException>(() => BikeDataBlobNames.AvailabilityProfile(maliciousId));

        Assert.Equal("stationId", exception.ParamName);
        Assert.StartsWith(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData("../../../etc/passwd", "Station ID cannot contain path separators.")]
    [InlineData("station/../secret", "Station ID cannot contain path separators.")]
    [InlineData("station/id", "Station ID cannot contain path separators.")]
    [InlineData("station\\id", "Station ID cannot contain path separators.")]
    [InlineData("station\0id", "Station ID cannot contain control characters.")]
    public void DestinationProfile_PathTraversalAttempt_ThrowsArgumentException(string maliciousId, string expectedMessage)
    {
        var exception = Assert.Throws<ArgumentException>(() => BikeDataBlobNames.DestinationProfile(maliciousId));

        Assert.Equal("stationId", exception.ParamName);
        Assert.StartsWith(expectedMessage, exception.Message);
    }

    [Fact]
    public void AvailabilityProfile_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => BikeDataBlobNames.AvailabilityProfile(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AvailabilityProfile_EmptyOrWhitespace_ThrowsArgumentException(string stationId)
    {
        Assert.Throws<ArgumentException>(() => BikeDataBlobNames.AvailabilityProfile(stationId));
    }

    [Fact]
    public void AvailabilityProfile_StandaloneDotDot_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => BikeDataBlobNames.AvailabilityProfile(".."));

        Assert.Equal("stationId", exception.ParamName);
        Assert.StartsWith("Station ID cannot contain '..'.", exception.Message);
    }

    [Fact]
    public void DestinationProfile_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => BikeDataBlobNames.DestinationProfile(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DestinationProfile_EmptyOrWhitespace_ThrowsArgumentException(string stationId)
    {
        Assert.Throws<ArgumentException>(() => BikeDataBlobNames.DestinationProfile(stationId));
    }
}
