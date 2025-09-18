using System.Security.Cryptography;

namespace ChemGa.Database.Utils;

public static class UuidV7
{
    public static string NewString()
    {
        // UUIDv7 layout: 48-bit unix_ms timestamp, 12 bits for version/variant and randomness.
        var unixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Span<byte> bytes = stackalloc byte[16];

        // Timestamp -> first 6 bytes (big-endian)
        bytes[0] = (byte)((unixMs >> 40) & 0xFF);
        bytes[1] = (byte)((unixMs >> 32) & 0xFF);
        bytes[2] = (byte)((unixMs >> 24) & 0xFF);
        bytes[3] = (byte)((unixMs >> 16) & 0xFF);
        bytes[4] = (byte)((unixMs >> 8) & 0xFF);
        bytes[5] = (byte)(unixMs & 0xFF);

        // Fill remaining 10 bytes with cryptographic randomness
        Span<byte> rand = stackalloc byte[10];
        RandomNumberGenerator.Fill(rand);
        for (int i = 0; i < 10; i++) bytes[6 + i] = rand[i];

        // Set version to 7 (4 bits) in byte 6 (the top 4 bits of bytes[6])
        bytes[6] = (byte)((bytes[6] & 0x0F) | (7 << 4));
        // Set RFC 4122 variant (10xx) in byte 8 (top 2 bits)
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

        // Format as standard UUID string
        return new Guid(bytes.ToArray()).ToString();
    }
}
