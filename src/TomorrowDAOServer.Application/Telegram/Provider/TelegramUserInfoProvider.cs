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
    Task<TelegramUserInfoIndex> GetByTelegramIdAsync(string telegramId);
    Task AddOrUpdateAsync(TelegramUserInfoIndex index);
    Task<List<TelegramUserInfoIndex>> GetByAddressListAsync(List<string> addressList);
    Task<TelegramUserInfoIndex> GetByAddressAsync(string address);
}

public class TelegramUserInfoProvider : ITelegramUserInfoProvider, ISingletonDependency
{
    private readonly INESTRepository<TelegramUserInfoIndex, string> _telegramUserInfoRepository;

    public TelegramUserInfoProvider(INESTRepository<TelegramUserInfoIndex, string> telegramUserInfoRepository)
    {
        _telegramUserInfoRepository = telegramUserInfoRepository;
    }

    public Task<TelegramUserInfoIndex> GetByTelegramIdAsync(string telegramId)
    {
        throw new NotImplementedException();
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

    public async Task<TelegramUserInfoIndex> GetByAddressAsync(string address)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TelegramUserInfoIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.Address).Value(address)) 
        };
        QueryContainer Filter(QueryContainerDescriptor<TelegramUserInfoIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _telegramUserInfoRepository.GetAsync(Filter);
    }
}