using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AElf;
using AElf.Client;
using AElf.Client.Dto;
using AElf.Contracts.Election;
using AElf.Contracts.ProxyAccountContract;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Moq;
using NSubstitute;
using TomorrowDAOServer.Common.AElfSdk;
using static TomorrowDAOServer.Common.TestConstant;
using TokenInfo = AElf.Contracts.MultiToken.TokenInfo;

namespace TomorrowDAOServer.Common.Mocks;

public class ContractProviderMock
{
    private static readonly AElfClient AelfClient = Substitute.For<AElfClient>();

    public static IContractProvider MockContractProvider()
    {
        var mock = new Mock<IContractProvider>();

        MockCreateCallTransactionAsync(mock);
        MockCreateTransactionAsync(mock);
        MockCallTransactionAsync<PubkeyList>(mock);
        MockCallTransactionAsync<CandidateVote>(mock);
        MockCallTransactionAsync<TokenInfo>(mock);
        MockCallTransactionAsync<ProxyAccount>(mock);
        MockGetTreasuryAddressAsync(mock);
        MockSendTransactionAsync(mock);
        MockContractAddress(mock);
        MockQueryTransactionResultAsync(mock);

        return mock.Object;
    }

    private static void MockQueryTransactionResultAsync(Mock<IContractProvider> mock)
    {
        mock.Setup(o => o.QueryTransactionResultAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string transactionId, string chainId) =>
            {
                return new TransactionResultDto
                {
                    TransactionId = transactionId,
                    Status = CommonConstant.TransactionStateMined,
                    Logs = new LogEventDto[]
                    {
                        new LogEventDto
                        {
                            Address = null,
                            Name = CommonConstant.VoteEventVoted,
                            Indexed = new string[]
                            {
                            },
                            NonIndexed = null
                        },
                    },
                    Bloom = null,
                    BlockNumber = 0,
                    BlockHash = null,
                    Transaction = null,
                    ReturnValue = null,
                    Error = null
                };
            });
    }

    private static void MockContractAddress(Mock<IContractProvider> mock)
    {
        mock.Setup(o => o.ContractAddress(It.IsAny<string>(), It.IsAny<string>())).Returns(
            (string chainId, string contractName) =>
            {
                if (contractName == CommonConstant.CaContractAddressName)
                {
                    return Address1;
                }
                else if (contractName == CommonConstant.VoteContractAddressName)
                {
                    return Address2;
                }

                return Address1;
            });
    }

    private static void MockSendTransactionAsync(Mock<IContractProvider> mock)
    {
        mock.Setup(o => o.SendTransactionAsync(It.IsAny<string>(), It.IsAny<Transaction>()))
            .ReturnsAsync(new SendTransactionOutput
            {
                TransactionId = TransactionHash.ToHex()
            });
    }

    private static void MockCreateCallTransactionAsync(Mock<IContractProvider> mock)
    {
        mock.Setup(e =>
                e.CreateCallTransactionAsync(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<IMessage>()))
            .ReturnsAsync((string chainId, string contractName, string methodName, IMessage param) =>
            {
                var transaction = new Transaction();
                transaction.MethodName = methodName;
                switch (methodName)
                {
                    //Return Task<(Hash transactionId, Transaction transaction)>
                    case CommonConstant.ElectionMethodGetVotedCandidates:
                    case CommonConstant.ElectionMethodGetCandidateVote:
                        return new(TransactionHash, transaction);
                        break;
                    default:
                        return new(TransactionHash, transaction);
                        break;
                }
                // throw new Exception("Not support method" + methodName);
            });
    }

    private static void MockCreateTransactionAsync(Mock<IContractProvider> mock)
    {
        mock.Setup(e =>
                e.CreateTransactionAsync(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IMessage>()))
            .ReturnsAsync((string chainId, string senderPublicKey, string contractName, string methodName,
                IMessage param) =>
            {
                var transaction = new Transaction();
                transaction.MethodName = methodName;
                transaction.From = Address.FromBase58(Address1);
                transaction.To = Address.FromBase58(Address2);
                transaction.RefBlockNumber = 100;
                transaction.MethodName = methodName;
                switch (methodName)
                {
                    //Return Task<(Hash transactionId, Transaction transaction)>
                    case CommonConstant.ElectionMethodGetVotedCandidates:
                    case CommonConstant.ElectionMethodGetCandidateVote:
                        return new(TransactionHash, transaction);
                        break;
                    default:
                        return new(TransactionHash, transaction);
                        break;
                }
                // throw new Exception("Not support method" + methodName);
            });
    }

    private static void MockCallTransactionAsync<T>(Mock<IContractProvider> mock) where T : class
    {
        Func<string, Transaction, Task<T>> factory = async (chainId, transaction) =>
        {
            if (typeof(T) == typeof(PubkeyList) &&
                transaction.MethodName == CommonConstant.ElectionMethodGetVotedCandidates)
            {
                return await Task.FromResult<T>((T)(object)new PubkeyList()
                {
                    Value =
                    {
                        new[] { ByteStringHelper.FromHexString(PublicKey1), ByteStringHelper.FromHexString(PublicKey2) }
                    }
                });
            }
            else if (typeof(T) == typeof(CandidateVote) &&
                     transaction.MethodName == CommonConstant.ElectionMethodGetCandidateVote)
            {
                return await Task.FromResult<T>((T)(object)new CandidateVote
                {
                    ObtainedActiveVotedVotesAmount = 10,
                    AllObtainedVotedVotesAmount = 200,
                    Pubkey = ByteStringHelper.FromHexString(PublicKey1)
                });
            }
            else if (typeof(T) == typeof(TokenInfo))
            {
                return await Task.FromResult<T>((T)(object)new TokenInfo
                {
                    Symbol = ELF,
                    TokenName = "aelf",
                    Supply = 10,
                    TotalSupply = 100,
                    Decimals = 1,
                    Issuer = Address.FromBase58(Address1),
                    IsBurnable = true,
                    IssueChainId = ChainHelper.ConvertBase58ToChainId(ChainIdAELF),
                    Issued = 10
                });
            }
            else if (typeof(T) == typeof(ProxyAccount))
            {
                var proxyAccount = new ProxyAccount
                {
                    CreateChainId = ChainHelper.ConvertBase58ToChainId(ChainIdAELF),
                    ProxyAccountHash = TransactionHash
                };
                proxyAccount.ManagementAddresses.Add(new ManagementAddress
                    {
                        Address = Address.FromBase58(Address1)
                    }
                );
                return await Task.FromResult<T>((T)(object)proxyAccount);
            }

            throw new Exception("Not support type.");
            //return await Task.FromResult<T>(Activator.CreateInstance(typeof(T)) as T);
        };

        mock.Setup(e => e.CallTransactionAsync<T>(It.IsAny<string>(), It.IsAny<Transaction>()))
            .Returns((string chainId, Transaction transaction) => factory(chainId, transaction));

        // string localChainId = null;
        // Transaction localTransaction = null;
        // mock.Setup(e => e.CallTransactionAsync<T>(It.IsAny<string>(), It.IsAny<Transaction>()))  
        //     .Callback<string, Transaction>((chainId, transaction) => { localChainId = chainId;
        //         localTransaction = transaction;
        //     })  
        //     .ReturnsAsync(() => factory(localChainId, localTransaction));

        // mock.Setup(e => e.CallTransactionAsync<object>(It.IsAny<string>(), It.IsAny<Transaction>())).ReturnsAsync(
        //     (string chainId, Transaction transaction) =>
        //     {
        //         if (transaction.MethodName == CommonConstant.ElectionMethodGetVotedCandidates)
        //         {
        //             return new PubkeyList()
        //             {
        //                 Value = { new[] { ByteStringHelper.FromHexString(PublicKey1), ByteStringHelper.FromHexString(PublicKey2) } }
        //             };
        //         }
        //         else if (transaction.MethodName == CommonConstant.ElectionMethodGetCandidateVote)
        //         {
        //             return new CandidateVote
        //             {
        //                 ObtainedActiveVotedVotesAmount = 10,
        //                 AllObtainedVotedVotesAmount = 200,
        //                 Pubkey = ByteStringHelper.FromHexString(PublicKey1)
        //             };
        //         }
        //
        //         throw new Exception("Not support method:" + transaction.MethodName);
        //     });
    }

    private static void MockGetTreasuryAddressAsync(Mock<IContractProvider> mock)
    {
        mock.Setup(o => o.GetTreasuryAddressAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Address1);
    }

    public static void MockGetChainStatusAsync()
    {
        AelfClient.GetChainStatusAsync().Returns(new ChainStatusDto
        {
            ChainId = ChainIdAELF,
            Branches = new Dictionary<string, long>(),
            NotLinkedBlocks = new Dictionary<string, string>(),
            LongestChainHeight = LongestChainHeight,
            LongestChainHash = LongestChainHash,
            GenesisBlockHash = GenesisBlockHash,
            GenesisContractAddress = GenesisContractAddress,
            LastIrreversibleBlockHash = LastIrreversibleBlockHash,
            LastIrreversibleBlockHeight = LastIrreversibleBlockHeight,
            BestChainHash = LongestChainHash,
            BestChainHeight = LongestChainHeight
        });
    }

    public static void MockTransaction_blockChain_chainStatus()
    {
        HttpRequestMock.MockHttpByPath(HttpMethod.Get, "api/blockChain/chainStatus", new ChainStatusDto
        {
            ChainId = ChainIdAELF,
            Branches = new Dictionary<string, long>(),
            NotLinkedBlocks = new Dictionary<string, string>(),
            LongestChainHeight = LongestChainHeight,
            LongestChainHash = LongestChainHash,
            GenesisBlockHash = GenesisBlockHash,
            GenesisContractAddress = GenesisContractAddress,
            LastIrreversibleBlockHash = LastIrreversibleBlockHash,
            LastIrreversibleBlockHeight = LastIrreversibleBlockHeight,
            BestChainHash = LongestChainHash,
            BestChainHeight = LongestChainHeight
        });
    }
}