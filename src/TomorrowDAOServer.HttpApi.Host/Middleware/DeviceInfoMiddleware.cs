using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Common.Handler;

namespace TomorrowDAOServer.Middleware;

public class DeviceInfoMiddleware
{
    private readonly ILogger<DeviceInfoMiddleware> _logger;
    private readonly RequestDelegate _next;

    public DeviceInfoMiddleware(RequestDelegate next, ILogger<DeviceInfoMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        DeviceInfoContext.CurrentDeviceInfo = await ExtractDeviceInfoAsync(context);

        try
        {
            await _next(context);
        }
        finally
        {
            DeviceInfoContext.Clear();
        }
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = nameof(TmrwDaoExceptionHandler.HandleExceptionAndReturn), ReturnDefault = ReturnDefault.Default, Message = "Decode device info error")]
    public virtual async Task<DeviceInfo> ExtractDeviceInfoAsync(HttpContext context)
    {
        var headers = context.Request.Headers;
        if (headers.IsNullOrEmpty()) return null;

        var clientTypeExists = headers.TryGetValue("ClientType", out var clientType);
        var clientVersionExists = headers.TryGetValue("Version", out var clientVersion);
        if (!clientTypeExists && !clientVersionExists) return null;

        return new DeviceInfo
        {
            ClientType = clientType.ToString().ToUpper(),
            Version = clientVersion.ToString().ToUpper()
        };
    }
}