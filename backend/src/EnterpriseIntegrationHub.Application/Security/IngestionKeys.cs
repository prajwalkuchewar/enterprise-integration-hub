using System.Security.Cryptography;
using System.Text;

namespace EnterpriseIntegrationHub.Application.Security;

public static class IngestionKeys
{
    private const string Prefix = "eih_";

    public static string Generate() =>
        Prefix + Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    public static string Hash(string key) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key.Trim()))).ToLowerInvariant();
}
