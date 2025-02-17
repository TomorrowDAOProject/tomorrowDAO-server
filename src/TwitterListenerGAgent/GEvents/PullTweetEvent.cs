using System.ComponentModel;
using Aevatar.Core.Abstractions;

namespace TwitterListenerGAgent.GAgents.GEvents;

[Description("Fetch the latest tweets from KOL")]
[GenerateSerializer]
public class PullTweetEvent : EventBase
{
}