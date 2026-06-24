using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NetForge.Server.Platform.Auditing;

/// <summary>Marks a property whose value must be redacted from audit change logs.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SensitiveAttribute : Attribute;

/// <summary>
/// Fluent equivalent of <see cref="SensitiveAttribute"/> for properties declared on a base class we
/// don't own (e.g. Identity's PasswordHash / SecurityStamp), where an attribute can't be applied. The
/// audit interceptor redacts both attribute- and annotation-marked properties.
///
/// Lives in its own file (separate from the Audit feature's implementation) so entity configs can call
/// <c>MarkSensitive</c> in every edition — in editions without Audit the annotation is simply inert.
/// </summary>
public static class AuditAnnotations
{
    public const string Sensitive = "Audit:Sensitive";

    public static PropertyBuilder<TProperty> MarkSensitive<TProperty>(this PropertyBuilder<TProperty> builder) =>
        builder.HasAnnotation(Sensitive, true);
}
