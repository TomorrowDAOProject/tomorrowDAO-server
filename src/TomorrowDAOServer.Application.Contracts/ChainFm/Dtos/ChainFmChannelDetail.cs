using System.Collections.Generic;

namespace TomorrowDAOServer.ChainFm.Dtos;

public class ChainFmChannelDetailResponse
{
    public ChainFmChannelDetailPageProps PageProps { get; set; }
    
}

public class ChainFmChannelDetailPageProps
{
    public string Id { get; set; }
    public ChainFmChannelDetailInitData InitialData { get; set; }
}

public class ChainFmChannelDetailInitData
{
    public ChainFmChannelDetailInitDataChannel Channel { get; set; }
}

public class ChainFmChannelDetailInitDataChannel
{
    public string Id { get; set; }
    public string User { get; set; }
    public string Icon { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> Addresses { get; set; }
    public List<ChainFmChannelDetailInitDataChannelEvent> Events { get; set; }
    public long Updated_At { get; set; }
    public long Created_At { get; set; }
    public bool Is_Private { get; set; }
    public long Last_Active_At { get; set; }
    public int Follow_Count { get; set; }
    public List<ChainFmChannelDetailInitDataChannelChatItems> Chat_Items { get; set; }
    public int Address_Count { get; set; }
    public List<string> Tx_Ids { get; set; }
}

public class ChainFmChannelDetailInitDataChannelEvent
{
    public List<string> Filter_Expressions { get; set; }
    public string Event { get; set; }
}

public class ChainFmChannelDetailInitDataChannelChatItems
{
    public string King { get; set; }
    public string link { get; set; }
}