using System.Net;
using OncoBridge.Api.Tests.Hosting;

namespace OncoBridge.Api.Tests.Reads;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class NotFoundTests(ApiPostgreSqlFixture fixture)
{
    [Fact]
    public async Task An_unknown_import_is_not_found()
    {
        using HttpResponseMessage response =
            await fixture.Client.GetAsync($"{ApiFixtures.ImportsRoute}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task The_findings_of_an_unknown_import_are_not_found()
    {
        using HttpResponseMessage response =
            await fixture.Client.GetAsync($"{ApiFixtures.ImportsRoute}/{Guid.NewGuid()}/findings");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task An_unknown_patient_record_is_not_found()
    {
        using HttpResponseMessage response =
            await fixture.Client.GetAsync($"/api/v1/patients/{Guid.NewGuid()}/record");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task The_timeline_of_an_unknown_patient_is_not_found()
    {
        using HttpResponseMessage response =
            await fixture.Client.GetAsync($"/api/v1/patients/{Guid.NewGuid()}/timeline");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task An_id_with_no_provenance_is_not_found()
    {
        using HttpResponseMessage response =
            await fixture.Client.GetAsync($"/api/v1/domain/{Guid.NewGuid()}/provenance");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_non_guid_id_does_not_reach_the_endpoint()
    {
        using HttpResponseMessage response =
            await fixture.Client.GetAsync($"{ApiFixtures.ImportsRoute}/not-a-guid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
