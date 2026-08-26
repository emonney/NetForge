using Microsoft.EntityFrameworkCore;
using NetForge.Server.Platform.MultiTenancy;
using NetForge.Server.Platform.Persistence;

namespace NetForge.Server.Data.Seed;

/// <summary>
/// Demo content for the surfaces that record history rather than own it — the audit trail, the signed-in
/// user's notifications, outgoing webhook endpoints with their delivery log, and a couple of discussion
/// threads. Runs after the users + sales passes, in Development or wherever <c>Seed:DemoData</c> is on.
/// Idempotent — each pass no-ops once its table has rows.
///
/// All of it is seeded deliberately rather than left to accumulate, because none of it accumulates on a
/// site nobody has used yet. Seeding also suppresses the audit interceptor (see
/// <see cref="Platform.Auditing.AuditSuppression"/>), so without this the trail would be empty on first
/// run, and before that suppression existed it was worse: several hundred rows all stamped within the
/// same second, which is a spike, not a history. An admin screen that opens on its empty state is the
/// single clearest tell that an app is a shell.
/// </summary>
public static class DemoActivitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        // Keeps the method a valid async no-op in a build that ships none of these subsystems, where the
        // conditional guards above strip every call.
        await Task.CompletedTask;
    }




}
