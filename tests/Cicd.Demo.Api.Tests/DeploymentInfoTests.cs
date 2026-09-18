using Microsoft.Extensions.Configuration;
using Cicd.Demo.Api;
using Xunit;

namespace Cicd.Demo.Api.Tests;

public sealed class DeploymentInfoTests
{
    [Fact]
    public void PromotionStrategy_UsesImmutableArtifactIdentity()
    {
        Assert.Equal("build-once-promote-by-digest", Promotion.Strategy);
    }

    [Fact]
    public void FromConfiguration_UsesPipelineValues()
    {
        var values = new Dictionary<string, string?>
        {
            ["APP_ENVIRONMENT"] = "qa",
            ["APP_VERSION"] = "1.2.3",
            ["APP_COMMIT_SHA"] = "abc123",
            ["APP_BRANCH"] = "main",
            ["APP_DEPLOYED_AT"] = "2026-08-24T12:00:00Z"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var result = DeploymentInfo.FromConfiguration(configuration);

        Assert.Equal("qa", result.Environment);
        Assert.Equal("1.2.3", result.Version);
        Assert.Equal("abc123", result.Commit);
        Assert.Equal("main", result.Branch);
    }

    [Fact]
    public void FromConfiguration_HasSafeLocalDefaults()
    {
        var configuration = new ConfigurationBuilder().Build();

        var result = DeploymentInfo.FromConfiguration(configuration);

        Assert.Equal("local", result.Environment);
        Assert.Equal("0.0.0-local", result.Version);
        Assert.Equal("ci-cd-environments-demo", result.Application);
    }
}
