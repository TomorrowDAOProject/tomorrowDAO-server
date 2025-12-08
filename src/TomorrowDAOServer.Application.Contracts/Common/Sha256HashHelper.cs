using System.Security.Cryptography;
using System.Text;

namespace TomorrowDAOServer.Common;

public class Sha256HashHelper
{
    public static string ComputeSha256Hash(string rawData)
    {
        var builder = new StringBuilder();
        using var sha256Hash = SHA256.Create();
        var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }
}