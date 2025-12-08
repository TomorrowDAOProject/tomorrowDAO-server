using System.Collections.Generic;

namespace TomorrowDAOServer.Common;

public class BlockChainTokenInfo
{
    public ExternalInfo ExternalInfo { get; set; } = new();
}

public class ExternalInfo
{ 
    public List<ExternalInfoDic> Value { get; set; } = new();
}

public class ExternalInfoDic
{ 
    public string Key { get; set; }
    public string Value { get; set; }
}