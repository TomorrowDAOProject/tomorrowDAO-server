using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TomorrowDAOServer.Ranking.Dto;
using TomorrowDAOServer.Ranking.Eto;
using Volo.Abp.EventBus.Distributed;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.MQ;

public partial class MessagePublisherServiceTest
{
    private IDistributedEventBus MockDistributedEventBus()
    {
        var mock = new Mock<IDistributedEventBus>();

        mock.Setup(m =>
            m.PublishAsync<VoteAndLikeMessageEto>(It.IsAny<VoteAndLikeMessageEto>(), It.IsAny<bool>(),
                It.IsAny<bool>())).Returns((VoteAndLikeMessageEto eventData,
            bool onUnitOfWorkComplete,
            bool useOutbox) =>
        {
            if (eventData.Alias.IndexOf("ThrowException") != -1 || eventData.Address.IndexOf("ThrowException") != -1)
            {
                throw new SystemException("Distributed Event Bus Exception");
            }
            return Task.CompletedTask;
        });
        
        return mock.Object;
    }
}