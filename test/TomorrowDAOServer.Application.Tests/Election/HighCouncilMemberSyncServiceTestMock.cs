using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using Google.Protobuf;
using GraphQL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Orleans;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.GraphQL;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.Contract;
using TomorrowDAOServer.Contract.Dto;
using TomorrowDAOServer.Election.Dto;
using TomorrowDAOServer.Grains.Grain.Election;
using TomorrowDAOServer.Options;

namespace TomorrowDAOServer.Election;

public partial class HighCouncilMemberSyncServiceTest
{
    private static ITransactionService MockTransactionService()
    {
        var mock = new Mock<ITransactionService>();
        //Task<T> CallTransactionAsync<T>(string chainId, string privateKey, string toAddress, string methodName, IMessage txParam) where T : class;
        mock.Setup(m => m.CallTransactionAsync<GetVictoriesDto>(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IMessage>())).ReturnsAsync(
            new GetVictoriesDto
            {
                Value = new List<string>() { "address1" }
            });
        return mock.Object;
    }

    // private static IClusterClient MockIClusterClient()
    // {
    //     var clusterClientMock = new Mock<IClusterClient>();
    //     clusterClientMock.Setup(x => x.GetGrain<IHighCouncilMembersGrain>(It.IsAny<string>(), null))
    //         .Returns();
    //     return clusterClientMock.Object;
    // }

    private static IGraphQLProvider MockGraphQLProvider()
    {
        var mock = new Mock<IGraphQLProvider>();
        mock.Setup(m => m.SetHighCouncilMembersAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns(Task.CompletedTask);
        return mock.Object;
    }

    private static IHighCouncilMembersGrain MockHighCouncilMembersGrain()
    {
        var mock = new Mock<IHighCouncilMembersGrain>();
        mock.Setup(m => m.SaveHighCouncilMembersAsync(It.IsAny<List<string>>())).Returns(Task.CompletedTask);
        return mock.Object;
    }
}