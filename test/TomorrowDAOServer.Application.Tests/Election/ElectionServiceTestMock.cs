using System.Collections.Generic;
using Moq;
using Orleans;
using Shouldly;
using TomorrowDAOServer.Grains.Grain.Election;
using Volo.Abp;

namespace TomorrowDAOServer.Election;

public partial class ElectionServiceTest
{
    private IClusterClient GetIClusterClient()
    {
        var mock = new Mock<IClusterClient>();
        var highCouncilMembersGrainMock = new Mock<IHighCouncilMembersGrain>();
        var highCouncilMembersGrainExceptionMock = new Mock<IHighCouncilMembersGrain>();
        mock.Setup(x => x.GetGrain<IHighCouncilMembersGrain>(It.IsAny<string>(), null))
            .Returns((string primaryKey, string grainClassNamePrefix) =>
            {
                if (primaryKey.IndexOf("ThrowException") != -1)
                {
                    return highCouncilMembersGrainExceptionMock.Object;
                }

                return highCouncilMembersGrainMock.Object;
            });
        highCouncilMembersGrainMock.Setup(m => m.GetHighCouncilMembersAsync()).ReturnsAsync(new List<string>()
        {
            "address1", "address2"
        });
        highCouncilMembersGrainExceptionMock.Setup(m => m.GetHighCouncilMembersAsync())
            .ThrowsAsync(new UserFriendlyException("Exception test"));
        return mock.Object;
    }
}