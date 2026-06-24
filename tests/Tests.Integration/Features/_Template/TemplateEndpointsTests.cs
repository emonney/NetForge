using System.Net;
using System.Net.Http.Json;
using NetForge.Server.Features._Template;
using NetForge.Tests.Integration.Fixtures;
using Shouldly;

namespace NetForge.Tests.Integration.Features._Template;

/// <summary>
/// Copy-source integration test for a slice's HTTP surface — the counterpart to <c>Features/_Template</c>
/// on the server. Copy this folder to <c>Features/{Domain}/</c>, rename <c>Template</c> → <c>{Domain}</c>,
/// swap in your slice's route + permission constants, then delete the <c>Skip</c>.
///
/// It's skipped here on purpose: the <c>_Template</c> slice is unregistered scaffolding (no mapped route,
/// no EF table), so the call would 404. Renaming to a real, registered slice makes it pass.
/// </summary>
[Collection(IntegrationCollection.Name)]
public sealed class TemplateEndpointsTests(CustomWebApplicationFactory factory)
{
    [Fact(Skip = "_Template is unregistered scaffolding — copy to a real slice and remove this Skip.")]
    public async Task Create_then_get_round_trips_the_item()
    {
        // var client = factory.CreateAuthenticatedClient(permissions: [TemplateItemPermissions.Read, TemplateItemPermissions.Create]);
        var client = factory.CreateAuthenticatedClient(permissions: ["template-items.read", "template-items.create"]);

        var create = await client.PostAsJsonAsync("/api/template-items",
            new CreateTemplateItemRequest(Name: "From a test", Description: null));

        create.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await create.Content.ReadFromJsonAsync<TemplateItemDto>();
        created!.Name.ShouldBe("From a test");

        var get = await client.GetAsync($"/api/template-items/{created.Id}");
        get.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
