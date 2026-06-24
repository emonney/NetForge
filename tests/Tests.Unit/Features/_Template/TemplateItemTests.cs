using NetForge.Server.Features._Template;
using Shouldly;

namespace NetForge.Tests.Unit.Features._Template;

/// <summary>
/// Copy-source unit tests for a slice's pure logic — the counterpart to <c>Features/_Template</c> on the
/// server. Copy this folder to <c>Features/{Domain}/</c>, rename <c>Template</c> → <c>{Domain}</c>
/// throughout, and point the references at your slice's validator + mapper. Like the rest of the
/// <c>_Template</c> scaffolding, it exercises types that aren't wired into the running app — but because
/// validators and mappers are pure, these tests really do run and pass as-is.
/// </summary>
public class TemplateItemTests
{
    [Fact]
    public void Validator_requires_a_name()
    {
        var validator = new CreateTemplateItemValidator();

        validator.Validate(new CreateTemplateItemRequest(Name: "Widget", Description: null)).IsValid.ShouldBeTrue();
        validator.Validate(new CreateTemplateItemRequest(Name: "", Description: null)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void ToDto_copies_the_fields()
    {
        var created = DateTimeOffset.UtcNow;
        var entity = new TemplateItem { Id = 1, Name = "Widget", Description = "Demo", CreatedAt = created };

        var dto = entity.ToDto();

        dto.Id.ShouldBe(1);
        dto.Name.ShouldBe("Widget");
        dto.Description.ShouldBe("Demo");
        dto.CreatedAt.ShouldBe(created);
    }
}
