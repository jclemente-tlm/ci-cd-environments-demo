using Cicd.Demo.Api;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var deployment = DeploymentInfo.FromConfiguration(app.Configuration);

app.MapGet("/", () => Results.Ok(deployment));
app.MapGet("/environment", () => Results.Ok(new
{
    deployment.Application,
    deployment.Environment,
    deployment.DeployedAtUtc
}));
app.MapGet("/version", () => Results.Ok(new
{
    deployment.Application,
    deployment.Version,
    deployment.Commit,
    deployment.Branch
}));
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/promotion", () => Results.Ok(new
{
    strategy = Promotion.Strategy,
    deployment.Version,
    deployment.Commit
}));

app.Run();

public partial class Program;
