using System.Threading.Tasks;
using Moq;
using TomorrowDAOServer.User;
using TomorrowDAOServer.User.Dtos;

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