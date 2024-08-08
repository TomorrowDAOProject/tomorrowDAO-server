using System.Collections.Generic;
using TomorrowDAOServer.DAO;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Contract.Dto;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Indexer;
using TomorrowDAOServer.Discussion;
using TomorrowDAOServer.Discussion.Dto;
using TomorrowDAOServer.Dtos;
using TomorrowDAOServer.Dtos.Explorer;
using TomorrowDAOServer.Dtos.NetworkDao;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.Governance.Dto;
using TomorrowDAOServer.NetworkDao.Dto;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Proposal;
using TomorrowDAOServer.Proposal.Dto;
using TomorrowDAOServer.Proposal.Index;
using TomorrowDAOServer.Token;
using TomorrowDAOServer.Token.Index;
using TomorrowDAOServer.Treasury.Dto;
using TomorrowDAOServer.User.Dtos;
using TomorrowDAOServer.Vote;
using TomorrowDAOServer.Vote.Dto;
using TomorrowDAOServer.Vote.Index;

namespace TomorrowDAOServer;

public class TomorrowDAOServerApplicationAutoMapperProfile : MapperBase
{
    public TomorrowDAOServerApplicationAutoMapperProfile()
    {
        CreateMap<TreasuryBalanceResponse.BalanceItem, TreasuryAssetsDto>()
            .ForPath(des => des.ChainId, opt
                => opt.MapFrom(source => CommonConstant.MainChainId))
            .ForPath(des => des.Symbol, opt
                => opt.MapFrom(source => source.Token.Symbol))
            .ForMember(des => des.Amount, opt
                => opt.MapFrom(source => MapAmount(source.TotalCount, source.Token.Decimals)))
            .ForPath(des => des.Decimal, opt
                => opt.MapFrom(source => source.Token.Decimals))
            .ForMember(des => des.UsdValue, opt
                => opt.MapFrom(source => source.DollarValue))
            ;
        CreateMap<UserIndex, UserDto>().ReverseMap();
        CreateMap<AddressInfo, UserAddressInfo>().ReverseMap();
        CreateMap<IndexerUserToken, UserTokenDto>();
        CreateMap<IndexerProposal, ProposalIndex>();
        CreateMap<ExecuteTransactionDto, ExecuteTransaction>()
            .ForMember(des => des.Params, opt
                => opt.MapFrom(source => MapTransactionParams(source.Params)))
            ;
        CreateMap<ProposalIndex, ProposalDto>()
            .ForMember(des => des.RealProposalStatus, opt
                => opt.MapFrom(source => source.ProposalStatus))
            .ForMember(des => des.ProposalStatus, opt
                => opt.MapFrom(source => MapHelper.MapProposalStatusString(source.ProposalStatus)))
            .ForMember(des => des.ProposalStage, opt
                => opt.MapFrom(source => MapHelper.MapProposalStageString(source.ProposalStage)))
            .ForMember(des => des.ProposalType, opt
                => opt.MapFrom(source => source.ProposalType.ToString()))
            .ForMember(des => des.ProposalStage, opt
                => opt.MapFrom(source => source.ProposalStage.ToString()))
            .ForMember(des => des.GovernanceMechanism, opt
                => opt.MapFrom(source => source.GovernanceMechanism.ToString()))
            ;
        CreateMap<ProposalIndex, ProposalBasicDto>()
            .ForMember(des => des.RealProposalStatus, opt
                => opt.MapFrom(source => source.ProposalStatus))
            .ForMember(des => des.ProposalStatus, opt
                => opt.MapFrom(source => MapHelper.MapProposalStatusString(source.ProposalStatus)))
            .ForMember(des => des.ProposalStage, opt
                => opt.MapFrom(source => MapHelper.MapProposalStageString(source.ProposalStage)))
            .ForMember(des => des.ProposalType, opt
                => opt.MapFrom(source => source.ProposalType.ToString()))
            .ForMember(des => des.ProposalStage, opt
                => opt.MapFrom(source => source.ProposalStage.ToString()))
            .ForMember(des => des.GovernanceMechanism, opt
                => opt.MapFrom(source => source.GovernanceMechanism.ToString()))
            ;
        CreateMap<ProposalIndex, ProposalDetailDto>()
            .ForMember(des => des.RealProposalStatus, opt
                => opt.MapFrom(source => source.ProposalStatus))
            .ForMember(des => des.ProposalStatus, opt
                => opt.MapFrom(source => MapHelper.MapProposalStatusString(source.ProposalStatus)))
            .ForMember(des => des.ProposalStage, opt
                => opt.MapFrom(source => MapHelper.MapProposalStageString(source.ProposalStage)))
            ;
        CreateMap<ProposalIndex, MyProposalDto>();
        CreateMap<IndexerVote, ProposalDto>();
        CreateMap<IndexerVote, ProposalDetailDto>();
        CreateMap<VoteRecordIndex, VoteRecordDto>();
        CreateMap<IndexerVoteRecord, VoteRecordIndex>()
            .ForMember(des => des.IsWithdraw, opt
                => opt.MapFrom(source => false))
            ;
        CreateMap<IndexerVoteRecord, VoteRecordDto>()
            .ForMember(des => des.Option, opt
                => opt.MapFrom(source => source.Option.ToString()))
            ;

        CreateMap<DAOIndex, DAOInfoDto>()
            .ForMember(des => des.GovernanceMechanism, opt
                => opt.MapFrom(src => MapGovernanceMechanism(src.GovernanceToken)))
            .ReverseMap();
        CreateMap<IndexerDAOInfo, HighCouncilConfig>()
            .ForMember(des => des.MaxHighCouncilCandidateCount, opt
                => opt.MapFrom(src => src.MaxHighCouncilCandidateCount))
            .ForMember(des => des.MaxHighCouncilMemberCount, opt
                => opt.MapFrom(src => src.MaxHighCouncilMemberCount))
            .ForMember(des => des.ElectionPeriod, opt
                => opt.MapFrom(src => src.ElectionPeriod))
            .ForMember(des => des.StakingAmount, opt
                => opt.MapFrom(src => src.StakingAmount))
            ;
        CreateMap<IndexerDAOInfo, DAOIndex>()
            .ForMember(des => des.FileInfoList, opt
                => opt.MapFrom(src => MapHelper.MapJsonConvert<List<FileInfo>>(src.FileInfoList)));
        CreateMap<Metadata, MetadataDto>().ReverseMap();
        CreateMap<IndexerMetadata, Metadata>()
            .ForMember(des => des.SocialMedia, opt
                => opt.MapFrom(src => MapHelper.MapJsonConvert<Dictionary<string, string>>(src.SocialMedia)));

        CreateMap<GovernanceSchemeThreshold, GovernanceSchemeThresholdDto>().ReverseMap();
        CreateMap<HighCouncilConfig, HighCouncilConfigDto>().ReverseMap();
        CreateMap<FileInfo, FileInfoDto>().ReverseMap();
        CreateMap<File, FileDto>().ReverseMap();
        CreateMap<PermissionInfo, PermissionInfoDto>().ReverseMap();

        CreateMap<ExplorerProposalResult, ProposalListResponse>()
            .ForMember(des => des.DeployTime,
                opt => opt.MapFrom(src => src.ReleasedTime.DefaultIfEmpty(src.ReleasedTime).ToUtcMilliSeconds()))
            .ForMember(des => des.GovernanceType, opt => opt.MapFrom(src => src.ProposalType))
            .ForMember(des => des.ProposalType, opt => opt.MapFrom(src => src.ProposalType))
            .ForMember(des => des.ProposalStatus, opt => opt.MapFrom(src => src.Status))
            .ForMember(des => des.StartTime, opt => opt.MapFrom(src => src.CreateAt.ToUtcMilliSeconds()))
            .ForMember(des => des.ExpiredTime, opt => opt.MapFrom(src => src.ExpiredTime.ToUtcMilliSeconds()))
            .ForMember(des => des.EndTime,
                opt => opt.MapFrom(src => src.ReleasedTime.DefaultIfEmpty(src.ExpiredTime).ToUtcMilliSeconds()))
            .ForMember(des => des.ApprovedCount, opt => opt.MapFrom(src => src.Approvals))
            .ForMember(des => des.RejectionCount, opt => opt.MapFrom(src => src.Rejections))
            .ForMember(des => des.AbstentionCount, opt => opt.MapFrom(src => src.Abstentions))
            .ForMember(des => des.TotalVoteCount,
                opt => opt.MapFrom(src => src.Approvals + src.Rejections + src.Abstentions))
            .ForMember(des => des.MinimalRequiredThreshold,
                opt => opt.MapFrom(src => src.OrganizationInfo.ReleaseThreshold.MinimalApprovalThreshold))
            .ForMember(des => des.MinimalApproveThreshold,
                opt => opt.MapFrom(src => src.OrganizationInfo.ReleaseThreshold.MinimalApprovalThreshold))
            .ForMember(des => des.MinimalVoteThreshold,
                opt => opt.MapFrom(src => src.OrganizationInfo.ReleaseThreshold.MinimalVoteThreshold))
            .ForMember(des => des.MaximalRejectionThreshold,
                opt => opt.MapFrom(src => src.OrganizationInfo.ReleaseThreshold.MaximalRejectionThreshold))
            .ForMember(des => des.MaximalAbstentionThreshold,
                opt => opt.MapFrom(src => src.OrganizationInfo.ReleaseThreshold.MaximalAbstentionThreshold))
            .ForMember(des => des.Transaction, opt => opt.MapFrom(src => new ProposalListResponse.TransactionDto
            {
                ContractMethodName = src.ContractMethod,
                ToAddress = src.ContractAddress
            }))
            .ReverseMap();

        CreateMap<TreasuryFundDto, TreasuryAssetsDto>()
            .ForMember(des => des.Amount, opt => opt.MapFrom(src => src.AvailableFunds));

        CreateMap<ExplorerTransactionResponse, TreasuryTransactionDto>()
            .ForMember(des => des.TransactionHash, opt => opt.MapFrom(src => src.TxId))
            .ForMember(des => des.MethodName, opt => opt.MapFrom(src => src.Method))
            .ReverseMap();

        CreateMap<ExplorerTransferResult, TreasuryTransactionDto>()
            .ForMember(des => des.TransactionHash, opt => opt.MapFrom(src => src.TxId))
            .ForMember(des => des.MethodName, opt => opt.MapFrom(src => src.Method))
            .ForMember(des => des.From, opt => opt.MapFrom(src => src.AddressFrom))
            .ForMember(des => des.To, opt => opt.MapFrom(src => src.AddressTo))
            .ForMember(des => des.TransactionTime, opt => opt.MapFrom(src => src.Time))
            .ReverseMap();

        CreateMap<DAOIndex, DAOListDto>()
            .ForMember(des => des.DaoId, opt => opt.MapFrom(src => src.Id))
            .ForMember(des => des.Logo, opt => opt.MapFrom(src => src.Metadata.LogoUrl))
            .ForMember(des => des.Name, opt => opt.MapFrom(src => src.Metadata.Name))
            .ForMember(des => des.Description, opt => opt.MapFrom(src => src.Metadata.Description))
            .ForMember(des => des.Creator, opt => opt.MapFrom(src => src.Creator))
            .ForMember(des => des.Symbol, opt => opt.MapFrom(src => src.GovernanceToken))
            .ForMember(dec => dec.VotersNum, opt => opt.MapFrom(source => source.VoterCount));
        CreateMap<ContractInfo, ContractInfoDto>();
        CreateMap<IndexerVoteSchemeInfo, VoteSchemeInfoDto>()
            .ForMember(des => des.VoteMechanismName, opt => opt.MapFrom(src => src.VoteMechanism.ToString()))
            ;
        CreateMap<ExplorerTokenInfoResponse, TokenDto>().ReverseMap();
        CreateMap<IndexerGovernanceSchemeDto, GovernanceSchemeDto>();
        CreateMap<IndexerGovernanceScheme, GovernanceScheme>()
            .ForMember(des => des.GovernanceMechanism, opt
                => opt.MapFrom(source => source.GovernanceMechanism.ToString()))
            ;

        CreateMap<IndexerVoteRecord, IndexerVoteHistoryDto>()
            .ForMember(des => des.TimeStamp, opt
                => opt.MapFrom(source => source.VoteTime))
            .ForMember(des => des.ProposalId, opt
                => opt.MapFrom(source => source.VotingItemId))
            .ForMember(des => des.MyOption, opt
                => opt.MapFrom(source => source.Option))
            .ForMember(des => des.VoteNum, opt
                => opt.MapFrom(source => source.Amount))
            .ForMember(des => des.TransactionId, opt
                => opt.MapFrom(source => source.TransactionId))
            ;

        CreateMap<ExplorerTokenInfoResponse, TokenInfoDto>();
        CreateMap<ProposalIndex, CommentIndex>();
        CreateMap<NewCommentInput, CommentIndex>();
        CreateMap<CommentIndex, CommentDto>();
    }
}