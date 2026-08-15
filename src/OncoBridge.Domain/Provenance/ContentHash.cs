using System.Security.Cryptography;

namespace OncoBridge.Domain.Provenance;

public readonly record struct ContentHash
{
    private const int Sha256HexLength = 64;

    private ContentHash(string value) => Value = value;

    public string Value { get; }

    public static string Algorithm => "SHA-256";

    public static ContentHash ComputeSha256(ReadOnlySpan<byte> content)
    {
        Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(content, digest);
        return new ContentHash(Convert.ToHexStringLower(digest));
    }

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

    public override string ToString() => Value;
}
