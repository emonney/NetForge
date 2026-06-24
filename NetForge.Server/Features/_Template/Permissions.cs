using System.ComponentModel;

namespace NetForge.Server.Features._Template;

// One constant per action. Aggregated into the permission catalog by reflection (any *Permissions
// class) and enforced via .RequirePermission(...). The [Description] feeds the admin catalog UI;
// omit it and the catalog humanises the name ("template.read" → "Read template"). Wildcards work:
// a role granted "template.*" gets all of these.
public static class TemplatePermissions
{
    [Description("View template items")]
    public const string Read = "template.read";

    [Description("Create template items")]
    public const string Create = "template.create";

    [Description("Edit template items")]
    public const string Update = "template.update";

    [Description("Delete template items")]
    public const string Delete = "template.delete";
}
