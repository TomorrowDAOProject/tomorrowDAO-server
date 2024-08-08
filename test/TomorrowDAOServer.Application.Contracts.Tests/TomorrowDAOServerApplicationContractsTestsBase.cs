using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using TomorrowDAOServer.Options;
using Xunit.Abstractions;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Application.Contracts.Tests;

public class
    TomorrowDaoServerApplicationContractsTestsBase : TomorrowDAOServerTestBase<
        TomorrowDaoServerApplicationContractsTestsModule>
{
    public TomorrowDaoServerApplicationContractsTestsBase(ITestOutputHelper output) : base(output)
    {
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockChainOptions());
    }
    
    private IOptionsMonitor<ChainOptions> MockChainOptions()
    {
        var mock = new Mock<IOptionsMonitor<ChainOptions>>();
        mock.Setup(e => e.CurrentValue).Returns(new ChainOptions
        {
            PrivateKeyForCallTx = PrivateKey1,
            ChainInfos = new Dictionary<string, ChainOptions.ChainInfo>()
            {
                {
                    ChainIdAELF, new ChainOptions.ChainInfo
                    {
                        BaseUrl = "https://test-node.io",
                        IsMainChain = true,
                        ContractAddress = new Dictionary<string, string>()
                        {
                            { "CaAddress", "CAContractAddress" },
                            { "AElf.ContractNames.Treasury", "AElfTreasuryContractAddress" }
                        }
                    }
                },
                {
                    ChainIdtDVW, new ChainOptions.ChainInfo
                    {
                        BaseUrl = "https://test-tdvv-node.io",
                        IsMainChain = false,
                        ContractAddress = new Dictionary<string, string>()
                        {
                            { "CaAddress", "CAContractAddress" },
                            { "TreasuryContractAddress", TreasuryContractAddress }
                        }
                    }
                }
            },
            TokenImageRefreshDelaySeconds = 300
        });
        return mock.Object;
    }
}