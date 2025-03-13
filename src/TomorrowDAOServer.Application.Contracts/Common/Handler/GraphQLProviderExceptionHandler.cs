using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Newtonsoft.Json;
using Serilog;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Common.Handler;

public partial class TmrwDaoExceptionHandler
{
    public static async Task<FlowBehavior> HandleSetTokenInfoAsync(Exception e, TokenInfoDto tokenInfo)
    {
        Log.Error(e, "SetTokenInfoAsync Exception chainId {chainId} symbol {symbol}", tokenInfo.ChainId,
            tokenInfo.Symbol);

        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return
        };
    }

    public static async Task<FlowBehavior> HandleGetTokenInfoAsync(Exception e, string chainId, string symbol)
    {
        Log.Error(e, "GetTokenInfoAsync Exception chainId {chainId} symbol {symbol}", chainId, symbol);

        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return
        };
    }

    public static async Task<FlowBehavior> HandleSetBPAsync(Exception e, string chainId, List<string> addressList,
        long round)
    {
        Log.Error(e, "SetBPAsync Exception chainId {chainId}", chainId);

        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return
        };
    }

    public static async Task<FlowBehavior> HandleGetLastEndHeightAsync(Exception e, string chainId,
        WorkerBusinessType queryChainType)
    {
        Log.Error(e, "GetIndexBlockHeight on chain {id} error", chainId);

        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = CommonConstant.LongError
        };
    }

    public static async Task<FlowBehavior> HandleSetLastEndHeightAsync(Exception e, string chainId,
        WorkerBusinessType queryChainType, long height)
    {
        Log.Error(e, "SetIndexBlockHeight on chain {id} error", chainId);

        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
        };
    }

    public static async Task<FlowBehavior> HandleGetHoldersAsync(Exception e, List<string> symbols, string chainId,
        int skipCount,
        int maxResultCount)
    {
        Log.Error(e, "GetHoldersAsyncException chainId={chainId}, symbol={symbol}", chainId,
            JsonConvert.SerializeObject(symbols));
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = new Dictionary<string, long>()
        };
    }

    public static async Task<FlowBehavior> HandleSetHighCouncilMembersAsync(Exception e, string chainId, string daoId,
        List<string> addressList)
    {
        Log.Error(e, "SetHighCouncilMembersAsync error: chain={id},DaoId={daoId}", chainId, daoId);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return
        };
    }
}