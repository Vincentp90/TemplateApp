using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Application;

/// <summary>
/// Shared password hashing utilities used by auth use cases.
/// </summary>
public static class PasswordHelper
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    public static void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
    {
        salt = RandomNumberGenerator.GetBytes(SaltSize);
        hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
    }

    public static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
    {
        var computed = Rfc2898DeriveBytes.Pbkdf2(password, storedSalt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return CryptographicOperations.FixedTimeEquals(computed, storedHash);
    }
}
