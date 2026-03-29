using System.Security.Cryptography;
using System.Text;
using VpnPortal.Application.Interfaces;

namespace VpnPortal.Infrastructure.Security;

public sealed class Sha256TokenProtector : ITokenProtector
{
    public string GenerateRawToken(int sizeInBytes = 32)
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(sizeInBytes))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    public string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
