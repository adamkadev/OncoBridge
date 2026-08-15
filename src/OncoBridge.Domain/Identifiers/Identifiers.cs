namespace OncoBridge.Domain.Identifiers;

// Identity types for the three things referenced ACROSS entity boundaries.
//
// Each is passed as a parameter or stored as a foreign reference, where a bare Guid would let two
// unrelated identities be swapped silently — a real bug the compiler can catch for free.
//
// An entity's OWN identifier stays a plain Guid. It is not passed around, so wrapping it would add
// types without preventing anything. That is a deliberate line, not an inconsistency: strong typing
// is applied where it does work, and nowhere else.

/// <summary>Identifies a patient within OncoBridge.</summary>
/// <remarks>
/// Identity is scoped to a single import in V1 — there is no cross-batch patient matching.
/// </remarks>
public readonly record struct PatientId(Guid Value)
{
    /// <summary>Creates a new unique patient identity.</summary>
    public static PatientId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}

/// <summary>Identifies one ingestion run.</summary>
public readonly record struct ImportBatchId(Guid Value)
{
    /// <summary>Creates a new unique import batch identity.</summary>
    public static ImportBatchId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}

/// <summary>Identifies one source resource as received within an import batch.</summary>
public readonly record struct SourceResourceId(Guid Value)
{
    /// <summary>Creates a new unique source resource identity.</summary>
    public static SourceResourceId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
