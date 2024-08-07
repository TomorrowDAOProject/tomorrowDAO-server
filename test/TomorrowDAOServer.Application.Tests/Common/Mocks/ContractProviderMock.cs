using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AElf;
using AElf.Client;
using AElf.Client.Dto;
using AElf.Contracts.Election;
using AElf.Types;
using Google.Protobuf;
using Moq;
using NSubstitute;
using TomorrowDAOServer.Common.AElfSdk;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Common.Mocks;

public class ContractProviderMock
{
    private static readonly AElfClient AelfClient = Substitute.For<AElfClient>();

    public static IContractProvider MockContractProvider()
    {
        var mock = new Mock<IContractProvider>();

        MockCreateCallTransactionAsync(mock);
        MockCallTransactionAsync<PubkeyList>(mock);
        MockCallTransactionAsync<CandidateVote>(mock);
        MockGetTreasuryAddressAsync(mock);

        return mock.Object;
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
        mock.Setup(o => o.GetTreasuryAddressAsync(It.IsAny<string>(),It.IsAny<string>())).ReturnsAsync(Address1);
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