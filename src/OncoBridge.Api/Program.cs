using Microsoft.EntityFrameworkCore;
using OncoBridge.Api;
using OncoBridge.Api.Endpoints;
using OncoBridge.Api.OpenApi;
using OncoBridge.Application.Imports;
using OncoBridge.Application.Normalization;
using OncoBridge.Application.Quality;
using OncoBridge.Application.Reading;
using OncoBridge.Domain.Quality;
using OncoBridge.Infrastructure.Persistence;
using OncoBridge.Interop.Fhir.Ingestion;
using OncoBridge.Interop.Fhir.Normalization;
using OncoBridge.Interop.Fhir.Quality;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OncoBridgeDbContext>((provider, options) =>
    options.UseNpgsql(
        OncoBridgeConnectionString.Read(provider.GetRequiredService<IConfiguration>())));

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddScoped<IImportPayloadIngestor, FhirBundleIngestor>();
builder.Services.AddScoped<ICanonicalNormalizer, FhirNormalizer>();
builder.Services.AddScoped<ISourceQualityEvaluator, FhirSourceQualityEvaluator>();
builder.Services.AddScoped<DomainQualityEvaluator>();

builder.Services.AddScoped<IImportBatchWriter, ImportBatchStore>();
builder.Services.AddScoped<INormalizationStore, NormalizationStore>();
builder.Services.AddScoped<IQualityStore, QualityStore>();
builder.Services.AddScoped<IOncoBridgeReadStore, OncoBridgeReadStore>();

builder.Services.AddScoped<NormalizeImportBatch>();
builder.Services.AddScoped<AssessImportBatch>();
builder.Services.AddScoped<ImportPayload>();
builder.Services.AddScoped<GetImport>();
builder.Services.AddScoped<GetImportFindings>();
builder.Services.AddScoped<GetPatientRecord>();
builder.Services.AddScoped<GetDomainProvenance>();

builder.Services.AddOpenApi(ApiMetadata.DocumentName, OncoBridgeOpenApi.Describe);

WebApplication app = builder.Build();

OncoBridgeConnectionString.RequireConfigured(app.Configuration);

RouteGroupBuilder v1 = app.MapGroup(ApiMetadata.RoutePrefix);

v1.MapImportEndpoints();
v1.MapPatientEndpoints();
v1.MapProvenanceEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle(ApiMetadata.Title).DisableAgent());
}

app.Run();

public partial class Program
{
}
