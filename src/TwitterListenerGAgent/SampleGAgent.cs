using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace AevatarTemplate.GAgents;

[GenerateSerializer]
public class SampleGAgentState : StateBase
{
    
}

[GenerateSerializer]
public class SampleStateLogEvent : StateLogEventBase<SampleStateLogEvent>
{
    
}

public class SampleGAgent : GAgentBase<SampleGAgentState, SampleStateLogEvent>
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a GAgent for sampling.");
    }

    public SampleGAgent(ILogger logger) : base(logger)
    {
    }
}