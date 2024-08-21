using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Enum;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Token.Dto;
using Volo.Abp;
using Volo.Abp.Caching;
using Volo.Abp.DistributedLocking;
using Xunit;
using Xunit.Abstractions;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Token;

public partial class TransferTokenServiceTest
{
    private IOptionsMonitor<TransferTokenOption> MockTransferTokenOption()
    {
        var mock = new Mock<IOptionsMonitor<TransferTokenOption>>();

        mock.Setup(o => o.CurrentValue).Returns(new TransferTokenOption
        {
            SenderAccount = PrivateKey1,
            SupportedSymbol = new HashSet<string>() { ELF },
            LockUserTimeout = 60000,
            TransferTimeout = 60000,
            SymbolDecimal = new Dictionary<string, int>() { { ELF, 8 } },
            RetryTimes = 20,
            RetryDelay = 3000
        });

        return mock.Object;
    }

    private IAbpDistributedLock MockAbpDistributedLock()
    {
        var mockLockProvider = new Mock<IAbpDistributedLock>();
        mockLockProvider
            .Setup(x => x.TryAcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns<string, TimeSpan, CancellationToken>((name, timeSpan, cancellationToken) =>
                Task.FromResult<IAbpDistributedLockHandle>(new LocalAbpDistributedLockHandle(new SemaphoreSlim(0))));
        return mockLockProvider.Object;
    }

    private IDistributedCache<string> MockIDistributedCache()
    {
        var mock = new Mock<IDistributedCache<string>>();

        mock.Setup(o =>
                o.GetAsync(It.IsAny<string>(), null, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, bool? hideErrors, bool considerUow, CancellationToken token) =>
            {
                if (key.IndexOf(TransferTokenStatus.AlreadyClaimed.ToString()) != -1)
                {
                    return JsonConvert.SerializeObject(new TokenClaimRecord
                    {
                        TransactionId = TransactionHash.ToHex(),
                        ClaimTime = TimeHelper.ToUtcString(DateTime.Now),
                        Status = TransferTokenStatus.AlreadyClaimed,
                        IsClaimedInSystem = false
                    });
                } else if (key.IndexOf(TransferTokenStatus.TransferInProgress.ToString()) != -1)
                {
                    return JsonConvert.SerializeObject(new TokenClaimRecord
                    {
                        TransactionId = TransactionHash.ToHex(),
                        ClaimTime = TimeHelper.ToUtcString(DateTime.Now),
                        Status = TransferTokenStatus.TransferInProgress,
                        IsClaimedInSystem = false
                    });
                }

                return null;
            });

        return mock.Object;
    }
}