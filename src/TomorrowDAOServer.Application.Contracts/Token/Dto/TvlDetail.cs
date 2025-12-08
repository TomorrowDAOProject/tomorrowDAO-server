using System.Collections.Generic;

namespace TomorrowDAOServer.Token.Dto;

public class TvlDetail
{
    public double Tvl { get; set; }
    public List<TokenTvl> Detail { get; set; }
}

public class TokenTvl
{
    public double Tvl { get; set; }
    public string Symbol { get; set; }
}