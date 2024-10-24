using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Entities;
using TomorrowDAOServer.ResourceToken.Dtos;
using TomorrowDAOServer.ResourceToken.Provider;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;

namespace TomorrowDAOServer.ResourceToken;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class ResourceTokenService : TomorrowDAOServerAppService, IResourceTokenService
{
    private readonly IResourceTokenProvider _resourceTokenProvider;
    private readonly IObjectMapper _objectMapper;

    public ResourceTokenService(IResourceTokenProvider resourceTokenProvider, IObjectMapper objectMapper)
    {
        _resourceTokenProvider = resourceTokenProvider;
        _objectMapper = objectMapper;
    }

    public async Task<RealtimeRecordsDto> GetRealtimeRecordsAsync(int limit, string type)
    {
        var buyList = await _resourceTokenProvider.GetLatestAsync(limit, type, CommonConstant.BuyMethod);
        var sellList = await _resourceTokenProvider.GetLatestAsync(limit, type, CommonConstant.SellMethod);
        return new RealtimeRecordsDto
        {
            BuyRecords = _objectMapper.Map<List<ResourceTokenIndex>, List<RecordDto>>(buyList),
            SoldRecords = _objectMapper.Map<List<ResourceTokenIndex>, List<RecordDto>>(sellList)
        };
    }

    public async Task<List<TurnoverDto>> GetTurnoverAsync(GetTurnoverInput input)
    {
        var intervals = GetIntervals(input.Interval, input.Range);
        var allRecords = await _resourceTokenProvider.GetAllByPeriodAsync(intervals.Min(), intervals.Max(), input.Type);
        var groupedResults = intervals.Select(_ => new List<ResourceTokenIndex>()).ToList();
        foreach (var record in allRecords)
        {
            for (var i = 0; i < intervals.Count - 1; i++)
            {
                if (record.OperateTime < intervals[i + 1] || record.OperateTime >= intervals[i])
                {
                    continue;
                }

                groupedResults[i].Add(record);
                break;
            }
        }

        var results = new List<TurnoverDto>();
        var latestList = await _resourceTokenProvider.GetLatestAsync(1, input.Type, string.Empty);
        var lastPrice = latestList.IsNullOrEmpty() ? "0" : (latestList[0].BaseAmount / Math.Pow(10, 8)).ToString(CultureInfo.InvariantCulture);
        for (var i = 0; i < intervals.Count - 1; i++)
        {
            var volume = groupedResults[i].Sum(item => item.BaseAmount);
            var prices = groupedResults[i]
                .OrderByDescending(item => item.OperateTime)
                .Select(item => item.BaseAmount.ToString())
                .ToList();

            if (!prices.Any())
            {
                prices.Add(lastPrice);
            }
            lastPrice = prices.Last();

            results.Add(new TurnoverDto
            {
                Date = intervals[i], Volume = volume.ToString(), Prices = prices
            });
        }

        return results;
    }

    public async Task<RecordPageDto> GetRecordsAsync(GetRecordsInput input)
    {
        var skipCount = input.Page * input.Limit;
        var (count, list) = await _resourceTokenProvider.GetPageListAsync(skipCount, input.Limit, input.Order, input.Address);
        return new RecordPageDto
        {
            Total = count, Records = _objectMapper.Map<List<ResourceTokenIndex>, List<RecordDto>>(list)
        };
    }

    private List<DateTime> GetIntervals(int interval, int range)
    {
        var currentTime = DateTime.UtcNow;
        var intervalSpan = TimeSpan.FromMilliseconds(interval);
        var nearestEndTime = AdjustToNearestInterval(currentTime, interval);
        var intervals = Enumerable.Range(0, range)
            .Select(i => nearestEndTime - i * intervalSpan)
            .Reverse()
            .Concat(new[] { currentTime })
            .ToList();
        return intervals;
    }

    private DateTime AdjustToNearestInterval(DateTime currentTime, int interval)
    {
        switch (interval)
        {
            case 300000: 
            case 1800000: 
            case 3600000: 
                return new DateTime(
                    currentTime.Year,
                    currentTime.Month,
                    currentTime.Day,
                    currentTime.Hour,
                    currentTime.Minute / (interval / 60000) * (interval / 60000),
                    0, DateTimeKind.Utc);
            
            case 14400000:
                return new DateTime(
                    currentTime.Year,
                    currentTime.Month,
                    currentTime.Day,
                    currentTime.Hour / 4 * 4,
                    0, 0, DateTimeKind.Utc);

            case 86400000:
                return new DateTime(
                    currentTime.Year,
                    currentTime.Month,
                    currentTime.Day,
                    0, 0, 0, DateTimeKind.Utc);

            case 604800000:
                var daysToSubtract = (int)currentTime.DayOfWeek;
                var startOfWeek = currentTime.AddDays(-daysToSubtract);
                return new DateTime(
                    startOfWeek.Year,
                    startOfWeek.Month,
                    startOfWeek.Day,
                    0, 0, 0, DateTimeKind.Utc);
            default:
                throw new UserFriendlyException($"Invalid interval : {interval}");
        }
    }
}