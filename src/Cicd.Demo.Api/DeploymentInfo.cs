namespace Cicd.Demo.Api;

public sealed record DeploymentInfo(
    string Application,
    string Environment,
    string Version,
    string Commit,
    string Branch,
    string DeployedAtUtc)
{
    public static DeploymentInfo FromConfiguration(IConfiguration configuration) => new(
        Application: configuration["APP_NAME"] ?? "ci-cd-environments-demo",
        Environment: configuration["APP_ENVIRONMENT"] ?? "local",
        Version: configuration["APP_VERSION"] ?? "0.0.0-local",
        Commit: configuration["APP_COMMIT_SHA"] ?? "local",
        Branch: configuration["APP_BRANCH"] ?? "local",
        DeployedAtUtc: configuration["APP_DEPLOYED_AT"] ?? "not-deployed");
}
