using System.Security.Cryptography;
using System.Text;

namespace TomorrowDAOServer.Common;

public class Base58Encoder
{
    private static readonly char[] _base58Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz".ToCharArray();

    public static string GenerateRandomBase58String(int length)
    {
        byte[] randomBytes = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        return EncodeBase58(randomBytes);
    }

    private static string EncodeBase58(byte[] input)
    {
        if (input == null || input.Length == 0)
            return string.Empty;

        var intData = new System.Numerics.BigInteger(input);
        if (intData.Sign < 0)
            intData = -intData;

        var sb = new StringBuilder();
        while (intData > 0)
        {
            int remainder = (int)(intData % 58);
            intData /= 58;
            sb.Insert(0, _base58Alphabet[remainder]);
        }

        foreach (var b in input)
        {
            if (b == 0)
                sb.Insert(0, _base58Alphabet[0]);
            else
                break;
        }

        return sb.ToString();
    }
}