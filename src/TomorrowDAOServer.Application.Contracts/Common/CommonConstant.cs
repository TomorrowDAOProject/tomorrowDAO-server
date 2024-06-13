namespace TomorrowDAOServer.Common;

public static class CommonConstant
{
    public const long LongError = -1;
    public const string Comma = ",";
    public const string Underline = "_";

    public const string EmptyString = "";
    public const string ELF = "ELF";
    public const string USDT = "USDT";
    public const string MainChainId = "AELF";
    
    //todo temporary count to make hc proposal not approved, real query when next version
    public const int HCCount = 3;
    public const int AbstractVoteTotal = 10000;

    public const string TreasuryContractAddressName = "TreasuryContractAddress";
        
    public const string ElectionMethodGetVotedCandidates = "GetVotedCandidates";
    public const string ElectionMethodGetCandidateVote = "GetCandidateVote";
    public const string TreasuryMethodGetTreasuryAccountAddress = "GetTreasuryAccountAddress";
}