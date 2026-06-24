namespace NetForge.Server.Features.Health;

/// <summary>Projected <see cref="Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport"/> for the dashboard.</summary>
public sealed record HealthReportDto(
    string Status,
    double TotalDurationMs,
    DateTimeOffset CheckedAt,
    IReadOnlyList<HealthEntryDto> Checks);

public sealed record HealthEntryDto(
    string Name,
    string Status,
    string? Description,
    double DurationMs,
    IReadOnlyList<string> Tags,
    string? Error,
    IReadOnlyDictionary<string, string> Data);
