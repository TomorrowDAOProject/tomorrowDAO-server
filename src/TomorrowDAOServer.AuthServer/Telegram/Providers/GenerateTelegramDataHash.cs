using System.Security.Cryptography;
using System.Text;
using AElf;
using TomorrowDAOServer.Auth.Constants;

namespace TomorrowDAOServer.Auth.Telegram.Providers;

public static class GenerateTelegramDataHash
{
    public static string AuthDataHash(string token, string dataCheckString)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var dataCheckStringBytes = Encoding.UTF8.GetBytes(dataCheckString);
        var computeFrom = HashHelper.ComputeFrom(tokenBytes).ToByteArray();

        using var hmac = new HMACSHA256(computeFrom);
        var hashBytes = hmac.ComputeHash(dataCheckStringBytes);
        return hashBytes.ToHex();
    }

    public static string TgBotDataHash(string token, string dataCheckString)
    {
        var webAppDataBytes = Encoding.UTF8.GetBytes(CommonConstants.Hmacsha256Key);
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        using var hmacWebAppData = new HMACSHA256(webAppDataBytes);
        using var hmacToken = new HMACSHA256(hmacWebAppData.ComputeHash(tokenBytes));
        var hashBytes = hmacToken.ComputeHash(Encoding.UTF8.GetBytes(dataCheckString));
        return hashBytes.ToHex();
    }
}