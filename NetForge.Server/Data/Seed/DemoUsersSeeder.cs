using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace NetForge.Server.Data.Seed;

/// <summary>
/// Demo content: a couple dozen users with staggered <see cref="AppUser.CreatedAt"/> and varied flags so the
/// Users DataGrid has real data to sort, filter, page, and select across on first run, plus a photo for the
/// seeded admin. Runs in Development or wherever <c>Seed:DemoData</c> is on. Idempotent and non-destructive —
/// the user pass skips once the table holds more than the admin.
/// </summary>
public static class DemoUsersSeeder
{
    private static readonly string[] Names =
    [
        "Ava", "Liam", "Noah", "Emma", "Olivia", "Sophia", "Mia", "Lucas", "Ethan", "Amelia",
        "Aria", "Leo", "Zoe", "Kai", "Maya", "Ivan", "Nora", "Omar", "Lena", "Theo",
        "Iris", "Ruby", "Finn", "Sara",
    ];

    public static async Task SeedAsync(IServiceProvider services)
    {
        var users = services.GetRequiredService<UserManager<AppUser>>();
        if (await users.Users.CountAsync() > 1) return; // only on a fresh DB (admin alone)

        var rng = new Random(42); // deterministic so the seeded set is stable run-to-run
        for (var i = 0; i < Names.Length; i++)
        {
            var email = $"{Names[i].ToLowerInvariant()}@demo.local";
            if (await users.FindByEmailAsync(email) is not null) continue;

            var user = new AppUser
            {
                UserName = email,
                Email = email,
                DisplayName = Names[i],
                EmailConfirmed = i % 4 != 0,                              // ~1 in 4 unconfirmed
                TwoFactorEnabled = i % 6 == 0,                            // a few with 2FA
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-(i * 3) - rng.Next(0, 3)),
            };

            await users.CreateAsync(user, "Demo123!$");
        }
    }

}
