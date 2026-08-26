using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetForge.Server.Platform.Authorization;
using NetForge.Server.Platform.MultiTenancy;

namespace NetForge.Server.Data.Seed;

/// <summary>
/// Demo content: a directory of users whose sign-up dates follow a plausible growth curve, spread across
/// a handful of roles, plus a photo for the seeded admin. Runs in Development or wherever
/// <c>Seed:DemoData</c> is on. Idempotent and non-destructive — the user pass skips once the table holds
/// more than the admin.
///
/// The volume is deliberate. A couple of dozen rows made every screen that summarises them look broken:
/// the Users grid fitted on one page (nothing to sort, filter, or page through), and the dashboard's
/// daily-signups series was one user every three days — a 0/1 sawtooth with no trend, which is what made
/// the charts read as noise rather than data. A few hundred rows on a real arrival curve is what makes
/// those screens look like a running product on first launch.
/// </summary>
public static class DemoUsersSeeder
{
    /// <summary>How far back the seeded sign-ups reach. Comfortably past the dashboard's 30-day window
    /// so a wider range has history behind it too.</summary>
    public const int HistoryDays = 120;

    /// <summary>Roughly how many users to end up with; the arrival curve distributes them over the window.</summary>
    private const int TargetUsers = 420;

    /// <summary>The shared password for every seeded demo account.</summary>
    private const string DemoPassword = "Demo123!$";

    private static readonly string[] FirstNames =
    [
        "Ava", "Liam", "Noah", "Emma", "Olivia", "Sophia", "Mia", "Lucas", "Ethan", "Amelia",
        "Aria", "Leo", "Zoe", "Kai", "Maya", "Ivan", "Nora", "Omar", "Lena", "Theo",
        "Iris", "Ruby", "Finn", "Sara", "Hana", "Diego", "Priya", "Ana", "Yusuf", "Elena",
        "Marcus", "Chen", "Freya", "Tomas", "Naomi", "Rafael", "Ines", "Jonas", "Aisha", "Viktor",
    ];

    // Wide enough that few of the ~420 addresses need a numeric disambiguator: 40 x 60 is 2400
    // combinations, which keeps collisions to well under a tenth of the directory. At 30 surnames nearly
    // a fifth of the Users grid read "…2@demo.local", which is the sort of detail that gives a seed away.
    private static readonly string[] LastNames =
    [
        "Bennett", "Okafor", "Nakamura", "Silva", "Kowalski", "Haddad", "Lindqvist", "Moreau", "Rossi", "Novak",
        "Fischer", "Petrov", "Duarte", "Castillo", "Ahmed", "Weaver", "Larsen", "Marchetti", "Oyelaran", "Kim",
        "Brennan", "Delgado", "Vasquez", "Osei", "Hartley", "Yilmaz", "Sandoval", "Whitfield", "Rahman", "Bergstrom",
        "Adeyemi", "Barros", "Cardoso", "Dvorak", "Eriksen", "Faulkner", "Gallagher", "Halvorsen", "Ibrahim", "Jansen",
        "Kaminski", "Laurent", "Mbeki", "Nordstrom", "Ortega", "Pahlavi", "Quinlan", "Ravenhill", "Sokolov", "Tanaka",
        "Ueda", "Villalobos", "Wexler", "Ximenes", "Yoshida", "Zielinski", "Abernathy", "Bouchard", "Chaudhry", "Donnelly",
    ];

    /// <summary>
    /// Demo roles beyond the seeded Admin/Member, so "users by role" is a real distribution instead of a
    /// single bar and /admin/roles has something to show. Weight = relative share of the directory.
    /// </summary>
    private static readonly (string Name, int Weight)[] RoleMix =
    [
        (SystemRoles.Member, 58),
        ("Support", 16),
        ("Analyst", 13),
        ("Manager", 9),
        (SystemRoles.Admin, 4),
    ];

    public static async Task SeedAsync(IServiceProvider services)
    {
        var users = services.GetRequiredService<UserManager<AppUser>>();
        if (await users.Users.CountAsync() > 1) return; // only on a fresh DB (admin alone)

        var db = services.GetRequiredService<AppDbContext>();
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        var rng = new Random(42); // deterministic so the seeded set is stable run-to-run

        var roleIds = await EnsureDemoRolesAsync(roles);
        var seeded = BuildDirectory(users, rng);

        db.Users.AddRange(seeded);
        await db.SaveChangesAsync();

        // Role assignment goes into TenantUserRole — the table the claims factory reads — never
        // AspNetUserRoles. Written in bulk here rather than through ITenantRoleService because that
        // diffs the existing set per user, which is hundreds of round-trips against a table we know is
        // empty. Application code must still go through the service.
        db.Set<TenantUserRole>().AddRange(seeded.Select(user => new TenantUserRole
        {
            TenantId = TenancyOptions.DefaultTenant,
            UserId = user.Id,
            RoleId = roleIds[PickRole(rng)],
            CreatedAt = user.CreatedAt,
        }));
        await db.SaveChangesAsync();
    }

    /// <summary>One <see cref="AppUser"/> per simulated sign-up, dated by <see cref="ArrivalCurve"/>.</summary>
    private static List<AppUser> BuildDirectory(UserManager<AppUser> users, Random rng)
    {
        // One hash for the whole cohort. Hashing individually is ~20ms of PBKDF2 each, which turns a
        // few-hundred-row demo seed into a half-minute of boot; these are throwaway accounts that all
        // share the same published password anyway, so per-account salting buys nothing here. Never do
        // this for real accounts.
        var sharedHash = users.PasswordHasher.HashPassword(new AppUser { UserName = "demo" }, DemoPassword);

        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seeded = new List<AppUser>(TargetUsers);
        var now = DateTimeOffset.UtcNow;

        foreach (var (dayOffset, count) in ArrivalCurve(rng))
        {
            for (var n = 0; n < count; n++)
            {
                var first = FirstNames[rng.Next(FirstNames.Length)];
                var last = LastNames[rng.Next(LastNames.Length)];
                var email = UniqueEmail(first, last, taken);

                // Spread within the day so the Users grid sorts sensibly on CreatedAt. A row that lands
                // in the future — today, past the current hour — is pulled back into the part of today
                // that has already happened, *not* simply "now minus an hour": subtracting blindly walks
                // across midnight in the small hours and files today's sign-ups under yesterday, which
                // shows up as the sign-ups chart diving to zero on its final day.
                var day = DateTime.SpecifyKind(now.UtcDateTime.Date.AddDays(-dayOffset), DateTimeKind.Utc);
                var createdAt = new DateTimeOffset(day.AddHours(rng.Next(7, 22)).AddMinutes(rng.Next(60)));
                if (createdAt > now)
                {
                    var elapsed = now - new DateTimeOffset(day);
                    createdAt = new DateTimeOffset(day).AddSeconds(rng.NextDouble() * elapsed.TotalSeconds);
                }

                var index = seeded.Count;
                seeded.Add(new AppUser
                {
                    UserName = email,
                    Email = email,
                    NormalizedUserName = email.ToUpperInvariant(),
                    NormalizedEmail = email.ToUpperInvariant(),
                    DisplayName = $"{first} {last}",
                    PasswordHash = sharedHash,
                    SecurityStamp = Guid.NewGuid().ToString("N"),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    EmailConfirmed = index % 9 != 0,       // ~1 in 9 still unconfirmed
                    TwoFactorEnabled = index % 6 == 0,     // a few with 2FA
                    LockoutEnabled = true,
                    CreatedAt = createdAt,
                });
            }
        }

        return seeded;
    }

    /// <summary>
    /// Sign-ups per day, oldest day first, shaped like a product that is growing: a rising base rate, a
    /// weekday/weekend rhythm, and day-to-day noise on top.
    ///
    /// The shape is the point. A uniform rate — what a naive "one user every N days" seed produces —
    /// draws a flat row of identical spikes, so every sparkline on the dashboard looks the same and none
    /// of them look like data.
    /// </summary>
    private static IEnumerable<(int DayOffset, int Count)> ArrivalCurve(Random rng)
    {
        var today = DateTime.UtcNow.Date;

        // Solved from the target so tuning the curve doesn't silently change the directory size.
        var weights = new double[HistoryDays];
        for (var day = 0; day < HistoryDays; day++)
        {
            var age = (HistoryDays - 1 - day) / (double)(HistoryDays - 1); // 0 = oldest, 1 = today
            var trend = 0.45 + 1.55 * age * age;                           // accelerating growth
            var weekday = today.AddDays(-day).DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 0.45 : 1.15;
            weights[day] = trend * weekday;
        }

        var scale = TargetUsers / weights.Sum();
        for (var day = HistoryDays - 1; day >= 0; day--)
        {
            // Noise is kept narrow on purpose. Wide noise swamps the trend at these daily volumes, and
            // then the week-on-week deltas on the dashboard come out negative as often as not — a demo
            // whose every KPI is down reads as a broken seed, not as candour.
            var expected = weights[day] * scale * (0.86 + rng.NextDouble() * 0.28);
            // Carry the fraction into a coin flip rather than truncating, or low-rate days round to zero
            // and the early part of the window flatlines.
            var count = (int)expected + (rng.NextDouble() < expected % 1 ? 1 : 0);
            if (count > 0) yield return (day, count);
        }
    }

    /// <summary>
    /// Creates the demo roles if absent and returns every mix role's id, keyed by name.
    ///
    /// Each gets a permission set that reads like a job. One blanket <c>users.read</c> apiece left
    /// /admin/roles showing three identical one-permission rows, which says nothing about what roles are
    /// *for* — and the permission matrix, one of the more convincing screens in the product, rendered as
    /// a single ticked column.
    /// </summary>
    private static async Task<Dictionary<string, string>> EnsureDemoRolesAsync(RoleManager<IdentityRole> roles)
    {
        var ids = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (name, _) in RoleMix)
        {
            var role = await roles.FindByNameAsync(name);
            if (role is null)
            {
                role = new IdentityRole(name);
                if (!(await roles.CreateAsync(role)).Succeeded) continue;
                foreach (var permission in PermissionsFor(name))
                    await roles.AddClaimAsync(role, new System.Security.Claims.Claim(PermissionClaims.ClaimType, permission));
            }
            ids[name] = role.Id;
        }
        return ids;
    }

    /// <summary>
    /// A plausible grant set per demo role. Admin and Member are seeded elsewhere (wildcard / sign-up
    /// baseline) and are left alone here.
    /// </summary>
    private static string[] PermissionsFor(string role)
    {
        var core = role switch
        {
            "Manager" => new[]
            {
                Features.Users.UserPermissions.Read, Features.Users.UserPermissions.Create,
                Features.Users.UserPermissions.Update, Features.Roles.RolePermissions.Read,
                Features.Settings.SettingPermissions.Read,
            },
            "Analyst" => [Features.Users.UserPermissions.Read, Features.Roles.RolePermissions.Read, Features.Settings.SettingPermissions.Read],
            "Support" => [Features.Users.UserPermissions.Read, Features.Users.UserPermissions.Update],
            _ => [],
        };
        if (core.Length == 0) return core;


        return core;
    }

    private static string PickRole(Random rng)
    {
        var roll = rng.Next(RoleMix.Sum(r => r.Weight));
        foreach (var (name, weight) in RoleMix)
        {
            roll -= weight;
            if (roll < 0) return name;
        }
        return SystemRoles.Member;
    }

    private static string UniqueEmail(string first, string last, HashSet<string> taken)
    {
        var stem = $"{first}.{last}".ToLowerInvariant();
        var email = $"{stem}@demo.local";
        for (var suffix = 2; !taken.Add(email); suffix++) email = $"{stem}{suffix}@demo.local";
        return email;
    }

}
