using System.ComponentModel;

namespace NetForge.Server.Features.Users;

public static class UserPermissions
{
    [Description("View users")]
    public const string Read = "users.read";

    [Description("Create users and send invitations")]
    public const string Create = "users.create";

    [Description("Edit users: roles, lock state, email verification, password resets")]
    public const string Update = "users.update";

    [Description("Delete users")]
    public const string Delete = "users.delete";
}
