using System;

namespace TomorrowDAOServer.Enums;

public enum WorkerBusinessType
{
    ProposalSync,
    [Obsolete]
    ProposalExpired,
    DAOSync,
    BPInfoUpdate,
    ProposalNewUpdate,
    HighCouncilMemberSync,
    VoteRecordSync,
    VoteWithdrawSync,
    TokenPriceUpdate,
    ProposalNumUpdate,
    ReferralSync,
    UserBalanceSync,
    TopInviterGenerate,
    ProposalRedisUpdate,
    TopProposalGenerate,
    TelegramAppsSync,
    FindminiAppsSync,
    TonGiftTaskGenerate,
    TonGiftTaskComplete,
    LuckyboxTaskComplete,
    ResourceTokenSync,
    ResourceTokenParse,
    NetworkDaoMainChainSync,
    NetworkDaoSideChainSync,
    NetworkDaoMainChainOrgSync,
    NetworkDaoSideChainOrgSync,
    AppUrlUpload,
}