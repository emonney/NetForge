namespace NetForge.Tests.Integration.Fixtures;

/// <summary>
/// Binds every integration test class to one shared <see cref="CustomWebApplicationFactory"/> — the host
/// boots and the catalog seeds exactly once for the whole suite. Tests stay independent by acting on
/// data they create (unique SKUs) or asserting invariants rather than absolute row counts.
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "Integration";
}
