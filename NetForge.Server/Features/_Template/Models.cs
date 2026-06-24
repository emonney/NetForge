namespace NetForge.Server.Features._Template;

// Copy this slice to Features/{Domain}/ and rename Template → {Domain} throughout.
// Underscore-prefixed folders are copy-source scaffolding: excluded from feature
// registration (FeatureRegistration) and from EF model building (AppDbContext).

public class TemplateItem
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public record TemplateItemDto(int Id, string Name, string? Description, DateTimeOffset CreatedAt);

public record CreateTemplateItemRequest(string Name, string? Description);

public record UpdateTemplateItemRequest(string Name, string? Description);
