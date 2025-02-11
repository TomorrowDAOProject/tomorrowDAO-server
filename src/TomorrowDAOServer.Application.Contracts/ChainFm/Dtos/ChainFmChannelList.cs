using System.Collections.Generic;

namespace TomorrowDAOServer.ChainFm.Dtos;

public class ChainFmChannelListInput
{
    public Dictionary<string, Json0> input { get; set; } = new Dictionary<string, Json0>()
    {
        { "0", new Json0() }
    };
}

public class Json0
{
    public Json0Json Json { get; set; } = new Json0Json();
}

public class Json0Json
{
    public string Kind { get; set; } = "trending";
    public Json0JsonPagination Pagination { get; set; } = new Json0JsonPagination();
    public bool ShowSpam { get; set; } = false;

    public List<string> IncludeFields { get; set; } = new List<string>()
    {
        "recentFollowers", "owner", "meta"
    };
}

public class Json0JsonPagination
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 24;
}

public class ChainFmChannelListResponse
{
    public ChainFmChannelListResponseResult Result { get; set; }
}

public class ChainFmChannelListResponseResult
{
    public ChainFmChannelListResponseResultData Data { get; set; }
}

public class ChainFmChannelListResponseResultData
{
    public ChainFmChannelListResponseResultDataJson Json { get; set; }
}

public class ChainFmChannelListResponseResultDataJson
{
    public List<ChainFmChannelListResponseResultDataJsonItem> Items { get; set; }
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class ChainFmChannelListResponseResultDataJsonItem
{
    public ChainFmChannelListResponseResultDataJsonItemChannel Channel { get; set; }
}

public class ChainFmChannelListResponseResultDataJsonItemChannel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string User { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }
    public long Updated_At { get; set; }
    public long Created_At { get; set; }
    public bool Is_Private { get; set; }
    public long Last_Active_At { get; set; }
    public int Follow_Count { get; set; }
    public int Address_Count { get; set; }
}