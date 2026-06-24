namespace NetForge.Server.Features._Template;

// Manual record-to-record mapping — no AutoMapper. Static extension methods only.
internal static class TemplateMappings
{
    public static TemplateItemDto ToDto(this TemplateItem entity) =>
        new(entity.Id, entity.Name, entity.Description, entity.CreatedAt);
}
