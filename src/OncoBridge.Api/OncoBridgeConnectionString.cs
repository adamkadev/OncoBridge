namespace OncoBridge.Api;

internal static class OncoBridgeConnectionString
{
    internal const string Name = "OncoBridge";

    internal static string Read(IConfiguration configuration)
    {
        string? value = configuration.GetConnectionString(Name);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"The connection string 'ConnectionStrings:{Name}' is not configured. The OncoBridge "
                    + "host requires an explicit PostgreSQL connection string and never invents one.");
        }

        return value;
    }

    internal static void RequireConfigured(IConfiguration configuration) => _ = Read(configuration);
}
