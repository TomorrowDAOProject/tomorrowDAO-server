using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Nest;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.Election;
using TomorrowDAOServer.Options;
using Volo.Abp;
using Xunit.Abstractions;

namespace TomorrowDAOServer.DAO.Provider;

public partial class DaoAliasProviderTest
{
    private static IOptionsMonitor<DaoAliasOptions> MockDaoAliasOptions()
    {
        var mock = new Mock<IOptionsMonitor<DaoAliasOptions>>();
        mock.Setup(m => m.CurrentValue).Returns(new DaoAliasOptions
        {
            CharReplacements = new Dictionary<string, string>()
            {
                { " ", "-" },
                { "&", "and" },
                { "@", "at" }
            },
            FilteredChars = new HashSet<string>() { "?", "#" }
        });
        return mock.Object;
    }

    private static IGraphQLProvider MockGraphQlProvider()
    {
        var mock = new Mock<IGraphQLProvider>();
        mock.Setup(m => m.SetDaoAliasInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DaoAliasDto>()))
            .ReturnsAsync((string chainId, string alias, DaoAliasDto daoAliasDto) =>
            {
                if (daoAliasDto.DaoId == "DaoId.Serial")
                {
                    return 1;
                }
                else if (daoAliasDto.DaoId == "DaoId.Exception")
                {
                    throw new UserFriendlyException("aaa");
                }

                return 0;
            });
        return mock.Object;
    }
}