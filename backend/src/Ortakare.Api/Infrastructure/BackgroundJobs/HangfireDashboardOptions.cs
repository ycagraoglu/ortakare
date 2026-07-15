namespace Ortakare.Api.Infrastructure.BackgroundJobs;

public sealed class HangfireDashboardOptions
{
    public const string SectionName = "Hangfire:Dashboard";

    public bool Enabled { get; init; }
    public string Path { get; init; } = "/operations/jobs";
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
