using System.Collections.Generic;
using TomorrowDAOServer.Enums;

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
        
    public const string ElectionMethodGetVotedCandidates = "GetVotedCandidates";
    public const string ElectionMethodGetCandidateVote = "GetCandidateVote";
    public const string TreasuryMethodGetTreasuryAccountAddress = "GetTreasuryAccountAddress";
    public const string TokenMethodGetBalance = "GetBalance";
    public const string TokenMethodTransfer = "Transfer";
    public const string TokenMethodGetTokenInfo = "GetTokenInfo";
    public const string TokenMethodIssue = "Issue";
    public const string GovernanceMethodCreateProposal = "CreateProposal";
    public const string ProxyAccountMethodGetProxyAccountByAddress = "GetProxyAccountByProxyAccountAddress";
    
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
    public const string DescriptionIconBegin = "#B:";

    public const string OldDescriptionPattern = @"^##GameRanking\s*:\s*([a-zA-Z0-9&'’\-]+(?:\s*,\s*[a-zA-Z0-9&'’\-]+)*)\s*$";
    public const string NewDescriptionPattern = @"^##GameRanking[\s]*:[\s]*((?:\{[^{}]+\}[,]?)+)(?:#B[\s]*:[\s]*(?:\{([^{}]*)?\})?)?$";
    public const string NewDescriptionAliasPattern =  @"\{([^{}]+)\}";
    public const string DayFormatString = "yyyyMMdd";
    public const long TenMinutes = 10 * 60 * 1000;
    public const long OneDay = 24 * 60 * 60 * 1000;

    public static readonly Dictionary<string, Dictionary<string, VoteMechanism>> VoteSchemeDic = new()
    {
        {TestNetSideChainId, new Dictionary<string, VoteMechanism>
        {
            { "82493f7880cd1d2db09ba90b85e5d5605c40db550572586185e763f75f5ede11", VoteMechanism.UNIQUE_VOTE },
            { "934d1295190d97e81bc6c2265f74e589750285aacc2c906c7c4c3c32bd996a64", VoteMechanism.TOKEN_BALLOT }
        }},
        {MainNetSideChainId, new Dictionary<string, VoteMechanism>
        {
            { "75cf106c00988681e21f44b87215fe827b96fb45bb48c7b08772ca535fdbefb7", VoteMechanism.UNIQUE_VOTE },
            { "b39d3b9a1cea1ff57735520fdaa414bf9c1fc05f5d00cf41326809051882f2ac", VoteMechanism.TOKEN_BALLOT }
        }}
    };
    
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
    public const double InterestedPercent = 0.75;
    
    // App
    public const string System = "System";
    public const string ConnectionProposalIdMap = "ConnectionProposalIdMap";
    
    //GrainId
    public const string GrainIdTelegramAppSequence = "TelegramAppSequence";
    
    //Findmini
    public const string FindminiUrlPrefix = "https://www.findmini.app";
    public const string FindminiCategoryPrefix = "https://www.findmini.app/category/";
}