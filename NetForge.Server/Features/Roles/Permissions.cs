using System.ComponentModel;

namespace NetForge.Server.Features.Roles;

public static class RolePermissions
{
    [Description("View roles and the permission catalog")]
    public const string Read = "roles.read";

    [Description("Create roles")]
    public const string Create = "roles.create";

    [Description("Edit roles and assign permissions")]
    public const string Update = "roles.update";

    [Description("Delete roles")]
    public const string Delete = "roles.delete";
}
