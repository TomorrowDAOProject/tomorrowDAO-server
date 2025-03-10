namespace TomorrowDAOServer.Common;

public static class CommonConstant
{
    public const int MaxResultCount = 500;
    public const string DateFormat = "yyyy-MM-dd HH:mm:ss";
    
    public const long LongError = -1;
    public const string Comma = ",";
    public const string Add = "+";
    public const string Underline = "_";
    public const string Middleline = "-";
    public const string Colon = ":";
    public const string Space = " ";
    public const char SpaceChar = ' ';
    public const string EmptyString = "";
    public const string LeftParenthesis = "{";
    public const string RightParenthesis = "}";
    public const string ELF = "ELF";
    public const string USDT = "USDT";
    public const string USD = "USD";
    public const string MainChainId = "AELF";
    public const string TestNetSideChainId = "tDVW";
    public const string MainNetSideChainId = "tDVV";
    
    public const int AbstractVoteTotal = 10000;

    public const string CaContractAddressName = "CaAddress";
    public const string VoteContractAddressName = "VoteContractAddress";
    public const string GovernanceContractAddress = "GovernanceContractAddress";
    public const string TreasuryContractAddressName = "TreasuryContractAddress";
    public const string ProxyAccountContractAddressName = "AElf.Contracts.ProxyAccountContract";

    public const string CaMethodGetHolderInfo = "GetHolderInfo";
    public const string ElectionMethodGetVotedCandidates = "GetVotedCandidates";
    public const string ElectionMethodGetCandidateVote = "GetCandidateVote";
    public const string TreasuryMethodGetTreasuryAccountAddress = "GetTreasuryAccountAddress";
    public const string TokenMethodGetBalance = "GetBalance";
    public const string TokenMethodTransfer = "Transfer";
    public const string TokenMethodGetTokenInfo = "GetTokenInfo";
    public const string TokenMethodIssue = "Issue";
    public const string GovernanceMethodCreateProposal = "CreateProposal";
    public const string ProxyAccountMethodGetProxyAccountByAddress = "GetProxyAccountByProxyAccountAddress";
    public const string Acs3MethodGetProposal = "GetProposal";
    public const string OrganizationMethodGetOrganization = "GetOrganization";
    public const string OrganizationMethodGetProposerWhiteList = "GetProposerWhiteList";
    
    public const string TransactionStateMined = "MINED";
    public const string TransactionStatePending = "PENDING";
    public const string TransactionStateNotExisted = "NOTEXISTED";
    public const string TransactionStateFailed = "FAILED";
    public const string TransactionStateNodeValidationFailed = "NODEVALIDATIONFAILED";

    public const string RootParentId = "root";
    
    //LogEvent
    public const string VoteEventVoted = "Voted";
    public const string MemoPattern = @"##GameRanking\s*:\s*\{([^}]+)\}";
    public const string DescriptionBegin = "##GameRanking:";
    public const string DescriptionTMARankingBegin = "##GameRanking:TMARanking";
    public const string DescriptionIconBegin = "#B:";

    public const string OldDescriptionPattern = @"^##GameRanking\s*:\s*([a-zA-Z0-9&'’\-]+(?:\s*,\s*[a-zA-Z0-9&'’\-]+)*)\s*$";
    public const string NewDescriptionPattern = @"^##GameRanking[\s]*:[\s]*((?:\{[^{}]+\}[,]?)+)(?:#B[\s]*:[\s]*(?:\{([^{}]*)?\})?)?$";
    public const string TMADescriptionPattern = @"^##GameRanking:TMARanking(?:#B[\s]*:[\s]*(?:\{([^{}]*)?\})?)?$";
    public const string NewDescriptionAliasPattern =  @"\{([^{}]+)\}";
    public const string DayFormatString = "yyyyMMdd";
    public const long TenMinutes = 10 * 60 * 1000;
    public const long OneDay = 24 * 60 * 60 * 1000;

    // Hub
    public const string ReceivePointsProduce = "ReceivePointsProduce";
    public const string RequestPointsProduce = "RequestPointsProduce";
    public const string RequestUserBalanceProduce = "RequestUserBalanceProduce";
    public const string ReceiveUserBalanceProduce = "ReceiveUserBalanceProduce";
    
    // Referral
    public const string CreateAccountMethodName = "CreateCAHolder";
    public const string ProjectCode = "13027";
    public const string OrganicTraffic = "OrganicTraffic";
    public const string OrganicTrafficBeforeProjectCode = "OrganicTrafficBeforeProjectCode";
    
    // Votigram
    public const string VotigramCollectionSymbolTestNet = "TOMORROWPASSTEST-1"; 
    public const string VotigramCollectionSymbolMainNet = "TOMORROWPASS-1"; 
    
    public static string GetVotigramSymbol(string chainId)
    {
        return chainId switch
        {
            MainNetSideChainId => VotigramCollectionSymbolMainNet,
            MainChainId => string.Empty,
            TestNetSideChainId => VotigramCollectionSymbolTestNet,
            _ => string.Empty
        };
    }
    
    // Information
    public const string ProposalId = "ProposalId";
    public const string ProposalTitle = "ProposalTitle";
    public const string Alias = "Alias";
    public const string CycleStartTime = "CycleStartTime";
    public const string CycleEndTime = "CycleEndTime";
    public const string InviteCount = "InviteCount";
    public const string Rank = "Rank";
    public const string Inviter = "Inviter";
    public const string Invitee = "Invitee";
    public const string AdPlatform = "AdPlatform";
    public const string AdTime = "AdTime";
    public const string ProposalDescription = "ProposalDescription"; 
    
    
    // Discover
    public const string Recommend = "Recommend";
    public const string New = "New";
    public const string ForYou = "ForYou";
    public const string Accumulative = "Accumulative";
    public const string Current = "Current";
    public const string Trending = "Trending";
    public const double InterestedPercent = 0.75;
    
    // App
    public const string System = "System";
    public const string ConnectionProposalIdMap = "ConnectionProposalIdMap";
    
    //GrainId
    public const string GrainIdTelegramAppSequence = "TelegramAppSequence";
    
    //Findmini
    public const string FindminiUrlPrefix = "https://www.findmini.app";
    public const string FindminiCategoryPrefix = "https://www.findmini.app/category/";
    
    //Upload
    public const string DownloadFail = "DownloadFail";
    public const string ConvertFail = "ConvertFail";
    public const string UploadFail = "UploadFail";
}