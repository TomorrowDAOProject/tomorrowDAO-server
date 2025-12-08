using System.Reflection;
using Orleans;
using TomorrowDAOServer.Monitor.Common;

namespace TomorrowDAOServer.Monitor.Orleans.Filters;

public static class GrainMethodFormatter
{
    public delegate string GrainMethodFormatterDelegate(IGrainCallContext callContext);

    public static string MethodFormatter(IGrainCallContext callContext)
    {
        MethodInfo grainMethod;
        if (callContext is IIncomingGrainCallContext)
        {
            grainMethod = ((IIncomingGrainCallContext)callContext).ImplementationMethod;
        }
        else
        {
            grainMethod = callContext.InterfaceMethod;
        }

        var typeFullName = grainMethod?.DeclaringType?.FullName;
        var methodName = grainMethod?.Name ?? MonitorConstant.Unknown;
        return $"{typeFullName}.{methodName}";
    }
}