using Aevatar.Core.Abstractions;

namespace AevatarTemplate.GAgents.Options;

public class TwitterListenerOptions : InitializationEventBase
{
    public List<string> KOLs { get; set; }
}