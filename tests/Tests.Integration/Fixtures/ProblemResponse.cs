using System.Text.Json.Serialization;

namespace NetForge.Tests.Integration.Fixtures;

/// <summary>
/// Just enough of the RFC 7807 body to assert on in tests. The platform flattens its <c>code</c> and
/// <c>traceId</c> into the ProblemDetails extensions, which serialize as top-level members.
/// </summary>
public sealed record ProblemResponse(
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("detail")] string? Detail,
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("traceId")] string? TraceId);
