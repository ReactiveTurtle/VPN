using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using VpnPortal.Application.Interfaces;

namespace VpnPortal.Infrastructure.Security;

public sealed class Argon2PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(16);
        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = 1,
            Iterations = 3,
            MemorySize = 65536
        };

        var hash = argon2.GetBytes(32);
        return $"$argon2id$v=19$m=65536,t=3,p=1${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);

        var segments = hash.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 5 || segments[0] != "argon2id")
        {
            return false;
        }

        var parameters = segments[2].Split(',');
        var memory = 65536;
        var iterations = 3;
        var parallelism = 1;

        foreach (var parameter in parameters)
        {
            var pair = parameter.Split('=');
            if (pair.Length != 2)
            {
                continue;
            }

            switch (pair[0])
            {
                case "m":
                    memory = int.Parse(pair[1]);
                    break;
                case "t":
                    iterations = int.Parse(pair[1]);
                    break;
                case "p":
                    parallelism = int.Parse(pair[1]);
                    break;
            }
        }

        var salt = Convert.FromBase64String(segments[3]);
        var expectedHash = Convert.FromBase64String(segments[4]);
        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = parallelism,
            Iterations = iterations,
            MemorySize = memory
        };

        var actualHash = argon2.GetBytes(expectedHash.Length);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
