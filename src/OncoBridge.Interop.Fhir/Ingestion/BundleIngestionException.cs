namespace OncoBridge.Interop.Fhir.Ingestion;

public sealed class BundleIngestionException : Exception
{
    public BundleIngestionException(string message)
        : base(message)
    {
    }

    public BundleIngestionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
