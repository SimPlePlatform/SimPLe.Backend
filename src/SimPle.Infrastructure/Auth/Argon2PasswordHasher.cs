using System.Security.Cryptography;
using Konscious.Security.Cryptography;
using SimPle.Application.Common.Interfaces;

namespace SimPle.Infrastructure.Auth;

// Argon2id is the OWASP-recommended algorithm for password hashing (as of 2024).
// Parameters chosen: 64 MiB memory, 3 iterations, 1 thread of parallelism.
// This matches the OWASP "stronger" profile and takes ~300ms on typical hardware.
// Adjust if login latency becomes a problem under load — but measure first.
public sealed class Argon2PasswordHasher : IPasswordHashingService
{
    private const int MemorySize = 65536;   // 64 MiB in KB
    private const int Iterations = 3;
    private const int Parallelism = 1;
    private const int HashLength = 32;
    private const int SaltLength = 16;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var hash = ComputeHash(password, salt, MemorySize, Iterations, Parallelism);

        // Encode as a recognizable PHC-style string so we can detect and upgrade parameters later.
        return BuildHashString(salt, hash, MemorySize, Iterations, Parallelism);
    }

    public bool Verify(string password, string hash)
    {
        if (!TryParseHashString(hash, out var salt, out var expectedHash, out var m, out var t, out var p))
            return false;

        var actualHash = ComputeHash(password, salt, m, t, p);

        // Constant-time comparison prevents timing attacks that could reveal valid passwords.
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    public bool NeedsRehash(string hash)
    {
        if (!TryParseHashString(hash, out _, out _, out var m, out var t, out var p))
            return true;

        return m != MemorySize || t != Iterations || p != Parallelism;
    }

    private static byte[] ComputeHash(string password, byte[] salt, int m, int t, int p)
    {
        using var argon2 = new Argon2id(System.Text.Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = m,
            Iterations = t,
            DegreeOfParallelism = p,
        };

        return argon2.GetBytes(HashLength);
    }

    private static string BuildHashString(byte[] salt, byte[] hash, int m, int t, int p)
    {
        var saltB64 = Convert.ToBase64String(salt).TrimEnd('=');
        var hashB64 = Convert.ToBase64String(hash).TrimEnd('=');
        return $"$argon2id$v=19$m={m},t={t},p={p}${saltB64}${hashB64}";
    }

    private static bool TryParseHashString(
        string encoded,
        out byte[] salt,
        out byte[] hash,
        out int m, out int t, out int p)
    {
        salt = Array.Empty<byte>();
        hash = Array.Empty<byte>();
        m = t = p = 0;

        // Expected format: $argon2id$v=19$m=65536,t=3,p=1$<salt_b64>$<hash_b64>
        var parts = encoded.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5 || parts[0] != "argon2id")
            return false;

        var paramParts = parts[2].Split(',');
        if (paramParts.Length != 3)
            return false;

        try
        {
            m = int.Parse(paramParts[0].Split('=')[1]);
            t = int.Parse(paramParts[1].Split('=')[1]);
            p = int.Parse(paramParts[2].Split('=')[1]);

            salt = Convert.FromBase64String(PadBase64(parts[3]));
            hash = Convert.FromBase64String(PadBase64(parts[4]));
        }
        catch
        {
            return false;
        }

        return true;
    }

    private static string PadBase64(string s)
    {
        int mod = s.Length % 4;
        return mod == 0 ? s : s + new string('=', 4 - mod);
    }
}
