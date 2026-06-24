using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NetForge.Server.Data;

/// <summary>
/// SQLite can't translate <see cref="DateTimeOffset"/> comparison/ordering (only equality), so any
/// date-sorted or range-filtered list 500s in dev. We store DateTimeOffset as UTC ticks (INTEGER) on
/// SQLite only — integers sort and range-compare natively, so the DataGrid's server-side date sort
/// works. Applied in <see cref="AppDbContext.OnModelCreating"/> behind <c>Database.IsSqlite()</c>;
/// Postgres/SQL Server keep their native datetimeoffset/timestamptz when migrations are regenerated
/// for that provider. All timestamps are persisted as UtcNow, so normalizing to a zero offset on the
/// way back loses nothing.
/// </summary>
internal static class SqliteValueConverters
{
    public static readonly ValueConverter<DateTimeOffset, long> DateTimeOffsetToTicks =
        new(v => v.UtcDateTime.Ticks, v => new DateTimeOffset(v, TimeSpan.Zero));

    public static readonly ValueConverter<DateTimeOffset?, long?> NullableDateTimeOffsetToTicks =
        new(
            v => v == null ? null : v.Value.UtcDateTime.Ticks,
            v => v == null ? null : new DateTimeOffset(v.Value, TimeSpan.Zero));

    // SQLite stores decimal as TEXT, so a server-side sort/range-filter on price (or any money column)
    // compares lexicographically ("100" < "9") — wrong. Persist money as integer minor units (cents) on
    // SQLite only, so the DataGrid's price sort/filter works. Demo money is 2dp, so ×100 is exact;
    // aggregates (SUM of revenue) are summed in memory as decimal to stay precise across providers.
    public static readonly ValueConverter<decimal, long> DecimalToCents =
        new(v => (long)Math.Round(v * 100m, MidpointRounding.AwayFromZero), v => v / 100m);

    public static readonly ValueConverter<decimal?, long?> NullableDecimalToCents =
        new(
            v => v == null ? null : (long)Math.Round(v.Value * 100m, MidpointRounding.AwayFromZero),
            v => v == null ? null : v.Value / 100m);
}
