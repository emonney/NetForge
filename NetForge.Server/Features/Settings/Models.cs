using System.Text.Json;
using NetForge.Server.Platform.Settings;

namespace NetForge.Server.Features.Settings;

/// <summary>A configurable setting for the admin UI. <see cref="Kind"/> ("boolean"/"number"/"string"/"choice")
/// tells the client which input to render; <see cref="Value"/> is the current App-scope value (or the
/// default when unset); <see cref="Options"/> is present for "choice" settings (a dropdown).</summary>
public record SettingDto(
    string Key, string Category, string Kind, JsonElement Value, JsonElement DefaultValue,
    IReadOnlyList<SettingOption>? Options = null);

/// <summary>Settings grouped by category for the admin page.</summary>
public record SettingCategoryDto(string Category, IReadOnlyList<SettingDto> Settings);

/// <summary>New value for a setting, as raw JSON validated against the setting's declared type.</summary>
public record UpdateSettingRequest(JsonElement Value);
