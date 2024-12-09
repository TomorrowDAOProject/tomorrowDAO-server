using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Moq;
using Nest;
using TomorrowDAOServer.Discussion;

namespace TomorrowDAOServer.Discover;

public partial class DiscoverServiceTest
{
    public INESTRepository<CommentIndex, string> MockCommentIndexRepository()
    {
        var mock = new Mock<INESTRepository<CommentIndex, string>>();

        mock.Setup(o => o.SearchAsync(It.IsAny<SearchDescriptor<CommentIndex>>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
            It.IsAny<string[]>())).ReturnsAsync(MockCommentIndexSearchResponse());

        return mock.Object;
    }

    public ISearchResponse<CommentIndex> MockCommentIndexSearchResponse()
    {
        var mock = new Mock<ISearchResponse<CommentIndex>>();
        mock.Setup(o => o.Aggregations).Returns(AggregateDictionary.Default);
        return mock.Object;
    }
}