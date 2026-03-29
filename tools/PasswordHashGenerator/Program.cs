using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: dotnet run --project tools/PasswordHashGenerator -- <password> [memoryKb] [iterations] [parallelism]");
    Environment.Exit(1);
}

var password = args[0];
var memoryKb = ParseOrDefault(args, 1, 65536);
var iterations = ParseOrDefault(args, 2, 3);
var parallelism = ParseOrDefault(args, 3, 1);

if (string.IsNullOrWhiteSpace(password))
{
    Console.Error.WriteLine("Password must not be empty.");
    Environment.Exit(1);
}

if (memoryKb < 8192 || iterations < 1 || parallelism < 1)
{
    Console.Error.WriteLine("Invalid Argon2 parameters.");
    Environment.Exit(1);
}

var salt = RandomNumberGenerator.GetBytes(16);
var passwordBytes = Encoding.UTF8.GetBytes(password);

var argon2 = new Argon2id(passwordBytes)
{
    Salt = salt,
    DegreeOfParallelism = parallelism,
    Iterations = iterations,
    MemorySize = memoryKb
};

var hash = argon2.GetBytes(32);
var phc = $"$argon2id$v=19$m={memoryKb},t={iterations},p={parallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";

Console.WriteLine("Argon2id password hash:");
Console.WriteLine(phc);

static int ParseOrDefault(string[] arguments, int index, int fallback)
{
    return arguments.Length > index && int.TryParse(arguments[index], out var value)
        ? value
        : fallback;
}
