namespace TomorrowDAOServer.Options;

public class ExchangeOptions
{
    public BinanceOptions Binance {get; set; }
    
    public OkxOptions Okx { get; set; }
    public AwakenOptions Awaken { get; set; }
    
    public int DataExpireSeconds { get; set; } = 60;
}


public class BinanceOptions
{
    public string BaseUrl { get; set; }
    public int Block429Seconds { get; set; } = 300;
}

public class OkxOptions
{
    public string BaseUrl { get; set; }
}

public class AwakenOptions
{
    public string BaseUrl { get; set; }
}