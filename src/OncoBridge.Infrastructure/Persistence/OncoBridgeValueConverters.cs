using System.Globalization;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Provenance;

namespace OncoBridge.Infrastructure.Persistence;

internal static class OncoBridgeValueConverters
{
    internal static readonly ValueConverter<ImportBatchId, Guid> ImportBatchId =
        new(id => id.Value, value => new ImportBatchId(value));

    internal static readonly ValueConverter<SourceResourceId, Guid> SourceResourceId =
        new(id => id.Value, value => new SourceResourceId(value));

    internal static readonly ValueConverter<ContentHash, string> ContentHash =
        new(hash => hash.Value, value => Domain.Provenance.ContentHash.Parse(value));

    internal static readonly ValueConverter<ReadOnlyMemory<byte>, byte[]> RawPayload =
        new(payload => payload.ToArray(), bytes => new ReadOnlyMemory<byte>(bytes));

    internal static readonly ValueComparer<ReadOnlyMemory<byte>> RawPayloadComparer =
        new(
            (left, right) => left.ToArray().SequenceEqual(right.ToArray()),
            payload => payload.Length,
            payload => payload);

    internal static readonly ValueConverter<DateTimeOffset, DateTimeOffset> UtcInstant =
        new(value => value.ToUniversalTime(), value => value);

    internal static readonly ValueConverter<PatientId, Guid> PatientId =
        new(id => id.Value, value => new PatientId(value));

    internal static readonly ValueConverter<PrimaryCancerDiagnosisId, Guid> PrimaryCancerDiagnosisId =
        new(id => id.Value, value => new PrimaryCancerDiagnosisId(value));

    internal static readonly ValueConverter<DateTimeOffset, string> StatedInstant =
        new(
            instant => instant.ToString("O", CultureInfo.InvariantCulture),
            text => DateTimeOffset.ParseExact(
                text, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
}
