using System.Security.Cryptography;

namespace OncoBridge.Domain.Provenance;

/// <summary>
/// A SHA-256 digest over an exact byte sequence, held as lowercase hex.
/// </summary>
/// <remarks>
/// <para>
/// The hash is always computed over <b>exact bytes</b> — never over a parsed, re-serialised or
/// otherwise reconstructed representation. Re-serialising JSON can reorder keys, change whitespace,
/// alter number formatting and rewrite escape sequences, any of which changes the digest while
/// preserving meaning. A hash over reconstructed content therefore proves nothing about what was
/// received (ADR-0003, ADR-0006).
/// </para>
/// <para>
/// This is why the type accepts raw bytes and nothing else: there is no overload taking a string
/// or an object, so the wrong thing cannot accidentally be hashed.
/// </para>
/// </remarks>
public readonly record struct ContentHash
{
    private const int Sha256HexLength = 64;

    private ContentHash(string value) => Value = value;

    /// <summary>The digest as 64 lowercase hex characters.</summary>
    public string Value { get; }

    /// <summary>The algorithm used, recorded so stored hashes remain interpretable.</summary>
    public static string Algorithm => "SHA-256";

    /// <summary>Computes the digest over the exact bytes supplied.</summary>
    public static ContentHash ComputeSha256(ReadOnlySpan<byte> content)
    {
        Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(content, digest);
        return new ContentHash(Convert.ToHexStringLower(digest));
    }

    /// <summary>Parses a previously computed digest.</summary>
    /// <exception cref="ArgumentException">The value is not 64 lowercase hex characters.</exception>
    public static ContentHash Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length != Sha256HexLength)
        {
            throw new ArgumentException(
                $"A SHA-256 digest must be {Sha256HexLength} hex characters; got {value.Length}.",
                nameof(value));
        }

        foreach (char c in value)
        {
            bool isLowerHex = c is >= '0' and <= '9' or >= 'a' and <= 'f';
            if (!isLowerHex)
            {
                throw new ArgumentException(
                    "A SHA-256 digest must be lowercase hexadecimal.", nameof(value));
            }
        }

        return new ContentHash(value);
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
