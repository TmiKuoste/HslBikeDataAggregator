using System.Text.Json;

namespace HslBikeDataAggregator.Tests.Configuration;

public sealed class ScaffoldConfigurationTests
{
    [Fact]
    public async Task LocalSettingsTemplate_ContainsDigitransitSubscriptionKeyPlaceholder()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var json = await File.ReadAllTextAsync(GetRepositoryFilePath("src", "HslBikeDataAggregator", "local.settings.example.json"), cancellationToken);
        using var document = JsonDocument.Parse(json);

        var key = document.RootElement
            .GetProperty("Values")
            .GetProperty("DigitransitSubscriptionKey")
            .GetString();

        Assert.Equal(string.Empty, key);
    }

    [Fact]
    public async Task LocalSettingsTemplate_ContainsGitHubPagesCorsOrigin()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var json = await File.ReadAllTextAsync(GetRepositoryFilePath("src", "HslBikeDataAggregator", "local.settings.example.json"), cancellationToken);
        using var document = JsonDocument.Parse(json);

        var corsOrigin = document.RootElement
            .GetProperty("Host")
            .GetProperty("CORS")
            .GetString();

        Assert.Equal("https://kuoste.github.io", corsOrigin);
    }

    [Theory]
    [InlineData("README.md", "read-optimised", "read-optimized")]
    [InlineData("README.md", "behaviour", "behavior")]
    [InlineData(".github", "copilot-instructions.md", "serialisation", "serialization")]
    [InlineData(".github", "instructions", "repository-workflow.instructions.md", "behaviour", "behavior")]
    [InlineData("docs", "adr", "001-cold-start-mitigation.md", "minimising", "minimizing")]
    public async Task RepositoryGuidance_UsesBritishEnglishForNormalisedTerms(params string[] values)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var expectedBritish = values[^2];
        var excludedAmerican = values[^1];
        var relativePathParts = values[..^2];
        var content = await File.ReadAllTextAsync(GetRepositoryFilePath(relativePathParts), cancellationToken);

        Assert.Contains(expectedBritish, content);
        Assert.DoesNotContain(excludedAmerican, content);
    }

    private static string GetRepositoryFilePath(params string[] parts)
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return Path.Combine(new[] { repositoryRoot }.Concat(parts).ToArray());
    }
}
