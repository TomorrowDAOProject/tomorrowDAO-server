using System.Net;

namespace SOLWallet;

public class GMGNProvider
{
    public async Task GetSmartWalletInfoAsync()
    {
        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        HttpClientHandler handler = new HttpClientHandler
        {
            AllowAutoRedirect = true
        };
        
        var url =
            "https://gmgn.ai/defi/quotation/v1/smartmoney/sol/walletNew/DfMxre4cKmvogbLrPigxmibVTTQDuzjdXojWzjCXXhzj?device_id=563044a0-9389-4cac-a99c-2ccc41433f97&client_id=gmgn_web_2025.0219.165723&from_app=gmgn&app_ver=2025.0219.165723&tz_name=Asia%2FShanghai&tz_offset=28800&app_lang=%22en-US%22&period=7d";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("accept", "application/json, text/plain, */*");
        client.DefaultRequestHeaders.Add("accept-language", "zh-CN,zh;q=0.9");
        client.DefaultRequestHeaders.Add("referer",
            "https://gmgn.ai/sol/address/th7mQwWY_DfMxre4cKmvogbLrPigxmibVTTQDuzjdXojWzjCXXhzj");
        client.DefaultRequestHeaders.Add("Cookie",
            "__cf_bm=0kEqz3q8mIxx4y8QYWZm3ynN9KabhwUcARIXXIykRno-1739961255-1.0.1.1-Bz8F5aor4w6bG0t507IiiL_sMveSLfEQ6r5DAazXVGtslNkH3bJz9DvA2s4rYTVwpJO2GBVyCgbDjmPHJRKTSw");
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        client.DefaultRequestHeaders.Add("Host", "gmgn.ai");

        try
        {
            var response = await client.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine("Response received successfully:");
            Console.WriteLine(responseBody);
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("\nException Caught!");
            Console.WriteLine("Message :{0} ", e.Message);
        }
    }
}