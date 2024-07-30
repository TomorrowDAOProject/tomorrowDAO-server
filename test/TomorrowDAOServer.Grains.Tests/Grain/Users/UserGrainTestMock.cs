using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Grains.Grain.Users;
using TomorrowDAOServer.User;
using TomorrowDAOServer.User.Dtos;
using Xunit;
using Xunit.Abstractions;
using static TomorrowDAOServer.Common.TestConstant;

namespace TomorrowDAOServer.Grain.Users;

public partial class UserGrainTest
{
    private IUserAppService MockUserAppService()
    {
        var mock = new Mock<IUserAppService>();

        mock.Setup(o => o.CreateUserAsync(It.IsAny<UserDto>())).Returns(Task.CompletedTask);

        return mock.Object;
    }
}