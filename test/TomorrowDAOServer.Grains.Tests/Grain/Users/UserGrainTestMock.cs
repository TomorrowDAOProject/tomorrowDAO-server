using System.Threading.Tasks;
using Moq;
using TomorrowDAOServer.User;
using TomorrowDAOServer.User.Dtos;
using TomorrowDAOServer.User.Provider;

namespace TomorrowDAOServer.Grain.Users;

public partial class UserGrainTest
{
    private IUserAppService MockUserAppService()
    {
        var mock = new Mock<IUserAppService>();
        mock.Setup(o => o.CreateUserAsync(It.IsAny<UserDto>())).Returns(Task.CompletedTask);
        return mock.Object;
    }
    
    private IUserProvider MockUserProvider()
    {
        var mock = new Mock<IUserProvider>();
        return mock.Object;
    }
}