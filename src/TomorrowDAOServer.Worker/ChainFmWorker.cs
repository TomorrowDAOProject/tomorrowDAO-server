using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TomorrowDAOServer.ChainFm;
using TomorrowDAOServer.ChainFm.Dtos;
using TomorrowDAOServer.ChainFm.Index;
using TomorrowDAOServer.ChainFm.Provider;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.HttpClient;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Work;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace TomorrowDAOServer.Worker;

public class ChainFmWorker : TomorrowDAOServerWorkBase
{
    private readonly ILogger<ScheduleSyncDataContext> _logger;
    private readonly IOptionsMonitor<WorkerLastHeightOptions> _workerLastHeightOptions;
    private readonly IOptionsMonitor<WorkerOptions> _workerOptions;
    private readonly IHttpProvider _httpProvider;
    private readonly IChainFmChannelProvider _chainFmChannelProvider;
    private readonly ISmartMoneyProvider _smartMoneyProvider;

    private const string ChannelListDomain = "https://chain.fm/api/trpc/channel.list?";

    //https://chain.fm/_next/data/hhAxz4G7w9ffNqg8B2p3X/channel/1305397292697653436.json?id=1305397292697653436
    private const string ChannelDetailDomain =
        "https://chain.fm/_next/data/hhAxz4G7w9ffNqg8B2p3X/channel/{0}.json?";

    protected override WorkerBusinessType BusinessType => WorkerBusinessType.ChainFm;

    public ChainFmWorker(ILogger<ScheduleSyncDataContext> logger,
        AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IScheduleSyncDataContext scheduleSyncDataContext,
        IOptionsMonitor<WorkerOptions> optionsMonitor,
        IOptionsMonitor<WorkerLastHeightOptions> workerLastHeightOptions, IHttpProvider httpProvider,
        IChainFmChannelProvider chainFmChannelProvider, ISmartMoneyProvider smartMoneyProvider) :
        base(logger, timer, serviceScopeFactory, scheduleSyncDataContext, optionsMonitor, workerLastHeightOptions)
    {
        _logger = logger;
        _workerOptions = optionsMonitor;
        _httpProvider = httpProvider;
        _chainFmChannelProvider = chainFmChannelProvider;
        _smartMoneyProvider = smartMoneyProvider;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var workerSetting = _workerOptions.CurrentValue.GetWorkerSettings(BusinessType);
        if (workerSetting is { OpenSwitch: false })
        {
            return;
        }

        var input = JsonConvert.SerializeObject(new ChainFmChannelListInput().input, JsonSettingsBuilder.New()
            .WithCamelCasePropertyNamesResolver()
            .IgnoreNullValue()
            .Build());
        var encode = Uri.EscapeDataString(input);
        var responses =
            await _httpProvider.InvokeAsync<List<ChainFmChannelListResponse>>(ChannelListDomain,
                new ApiInfo(HttpMethod.Get, $"batch=1&input={encode}"));

        var channelIndices = new List<ChainFmChannelIndex>();
        if (!responses.IsNullOrEmpty())
        {
            foreach (var response in responses)
            {
                var items = response.Result?.Data?.Json?.Items ??
                            new List<ChainFmChannelListResponseResultDataJsonItem>();
                if (!items.IsNullOrEmpty())
                {
                    foreach (var item in items)
                    {
                        channelIndices.Add(new ChainFmChannelIndex
                        {
                            Id = item.Channel.Id,
                            Name = item.Channel.Name,
                            User = item.Channel.User,
                            Icon = item.Channel.Icon,
                            Description = item.Channel.Description,
                            Updated_At = item.Channel.Updated_At,
                            Created_At = item.Channel.Created_At,
                            Is_Private = item.Channel.Is_Private,
                            Last_Active_At = item.Channel.Last_Active_At,
                            Follow_Count = item.Channel.Follow_Count,
                            Address_Count = item.Channel.Address_Count
                        });
                    }
                }
            }
        }

        if (!channelIndices.IsNullOrEmpty())
        {
            await _chainFmChannelProvider.BulkAddOrUpdateAsync(channelIndices);
        }

        await Task.Delay(TimeSpan.FromSeconds(10000));

        var channelList = await _chainFmChannelProvider.GetTopFollowerChannelListAsync(10);
        if (channelList.IsNullOrEmpty())
        {
            return;
        }


        await GetSmartMoneyByChannelsAsync(channelList);
    }

    private async Task GetSmartMoneyByChannelsAsync(List<ChainFmChannelIndex> channelList)
    {
        if (channelList.IsNullOrEmpty())
        {
            return;
        }

        var rand = new Random();

        foreach (var channelIndex in channelList)
        {
            await GetSmartMoneyByChannelAsync(channelIndex);
            var delaySeconds = rand.Next(10, 31);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }
    }

    private async Task GetSmartMoneyByChannelAsync(ChainFmChannelIndex channelIndex)
    {
        try
        {
            var responses =
                await _httpProvider.InvokeAsync<ChainFmChannelDetailResponse>(string.Format(ChannelDetailDomain, channelIndex.Id),
                    new ApiInfo(HttpMethod.Get, $"id={channelIndex.Id}"));

            var channel = responses.PageProps?.InitialData?.Channel;
            if (channel == null || channel.Addresses.IsNullOrEmpty())
            {
                return;
            }

            var smartMoneyIndices = new List<SmartMoneyIndex>();
            foreach (var channelAddress in channel.Addresses)
            {
                smartMoneyIndices.Add(new SmartMoneyIndex
                {
                    Id = channelAddress,
                    Address = channelAddress,
                    Source = SmartMoneySourceEnum.ChainFm
                });
            }

            await _smartMoneyProvider.BulkAddOrUpdateAsync(smartMoneyIndices);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetSmartMoneyByChannelAsync error");
        }
    }
}

public class ChainFmChannelDetailResponse
{
    public ChainFmChannelDetailPageProps PageProps { get; set; }
    
}

public class ChainFmChannelDetailPageProps
{
    public string Id { get; set; }
    public ChainFmChannelDetailInitData InitialData { get; set; }
}

public class ChainFmChannelDetailInitData
{
    public ChainFmChannelDetailInitDataChannel Channel { get; set; }
}

public class ChainFmChannelDetailInitDataChannel
{
    public string Id { get; set; }
    public string User { get; set; }
    public string Icon { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> Addresses { get; set; }
    public List<ChainFmChannelDetailInitDataChannelEvent> Events { get; set; }
    public long Updated_At { get; set; }
    public long Created_At { get; set; }
    public bool Is_Private { get; set; }
    public long Last_Active_At { get; set; }
    public int Follow_Count { get; set; }
    public List<ChainFmChannelDetailInitDataChannelChatItems> Chat_Items { get; set; }
    public int Address_Count { get; set; }
    public List<string> Tx_Ids { get; set; }
}

public class ChainFmChannelDetailInitDataChannelEvent
{
    public List<string> Filter_Expressions { get; set; }
    public string Event { get; set; }
}

public class ChainFmChannelDetailInitDataChannelChatItems
{
    public string King { get; set; }
    public string link { get; set; }
}

public class ChainFmChannelListInput
{
    public Dictionary<string, Json0> input { get; set; } = new Dictionary<string, Json0>()
    {
        { "0", new Json0() }
    };
}

public class Json0
{
    public Json0Json Json { get; set; } = new Json0Json();
}

public class Json0Json
{
    public string Kind { get; set; } = "trending";
    public Json0JsonPagination Pagination { get; set; } = new Json0JsonPagination();
    public bool ShowSpam { get; set; } = false;

    public List<string> IncludeFields { get; set; } = new List<string>()
    {
        "recentFollowers", "owner", "meta"
    };
}

public class Json0JsonPagination
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 24;
}

public class ChainFmChannelListResponse
{
    public ChainFmChannelListResponseResult Result { get; set; }
}

public class ChainFmChannelListResponseResult
{
    public ChainFmChannelListResponseResultData Data { get; set; }
}

public class ChainFmChannelListResponseResultData
{
    public ChainFmChannelListResponseResultDataJson Json { get; set; }
}

public class ChainFmChannelListResponseResultDataJson
{
    public List<ChainFmChannelListResponseResultDataJsonItem> Items { get; set; }
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class ChainFmChannelListResponseResultDataJsonItem
{
    public ChainFmChannelListResponseResultDataJsonItemChannel Channel { get; set; }
}

public class ChainFmChannelListResponseResultDataJsonItemChannel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string User { get; set; }
    public string Icon { get; set; }
    public string Description { get; set; }
    public long Updated_At { get; set; }
    public long Created_At { get; set; }
    public bool Is_Private { get; set; }
    public long Last_Active_At { get; set; }
    public int Follow_Count { get; set; }
    public int Address_Count { get; set; }
}