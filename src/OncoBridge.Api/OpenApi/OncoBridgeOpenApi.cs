using Microsoft.AspNetCore.OpenApi;

namespace OncoBridge.Api.OpenApi;

internal static class OncoBridgeOpenApi
{
    internal static void Describe(OpenApiOptions options) =>
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Info.Title = ApiMetadata.Title;
            document.Info.Version = ApiMetadata.Version;
            document.Info.Description = ApiMetadata.Description;

            return Task.CompletedTask;
        });
}
