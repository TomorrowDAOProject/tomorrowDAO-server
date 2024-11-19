using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Entities;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.Telegram.Provider;

public interface ITelegramUserInfoProvider
{
    Task AddOrUpdateAsync(TelegramUserInfoIndex index);
    Task<List<TelegramUserInfoIndex>> GetByAddressListAsync(List<string> addressList);
}

public class TelegramUserInfoProvider : ITelegramUserInfoProvider, ISingletonDependency
{
    private readonly INESTRepository<TelegramUserInfoIndex, string> _telegramUserInfoRepository;

    public TelegramUserInfoProvider(INESTRepository<TelegramUserInfoIndex, string> telegramUserInfoRepository)
    {
        _telegramUserInfoRepository = telegramUserInfoRepository;
    }

    public async Task AddOrUpdateAsync(TelegramUserInfoIndex index)
    {
        await _telegramUserInfoRepository.AddOrUpdateAsync(index);
    }

    public async Task<List<TelegramUserInfoIndex>> GetByAddressListAsync(List<string> addressList)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TelegramUserInfoIndex>, QueryContainer>>
        {
            q => q.Terms(i => i.Field(f => f.Address).Terms(addressList)) 
        };
        QueryContainer Filter(QueryContainerDescriptor<TelegramUserInfoIndex> f) => f.Bool(b => b.Must(mustQuery));
        return (await _telegramUserInfoRepository.GetListAsync(Filter)).Item2;
    }
}