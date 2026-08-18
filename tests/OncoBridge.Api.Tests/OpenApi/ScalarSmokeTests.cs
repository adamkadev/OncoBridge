using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using OncoBridge.Api.Tests.Hosting;

namespace OncoBridge.Api.Tests.OpenApi;

public sealed class ScalarSmokeTests
{
    [Fact]
    public async Task Scalar_serves_the_API_reference_in_Development()
    {
        await using OncoBridgeApiFactory factory = OncoBridgeApiFactory.WithoutDatabase();
        using HttpClient client =
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });
        using HttpResponseMessage response = await client.GetAsync("/scalar");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Neither_Scalar_nor_the_OpenAPI_document_is_mapped_outside_Development()
    {
        await using OncoBridgeApiFactory factory =
            OncoBridgeApiFactory.WithoutDatabase(OncoBridgeApiFactory.Production);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage scalar = await client.GetAsync("/scalar/");
        using HttpResponseMessage document = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.NotFound, scalar.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, document.StatusCode);
    }
}
