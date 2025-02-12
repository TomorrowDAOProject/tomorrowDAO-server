using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Orleans;

namespace SimpleAgent.GEvents;

[Description("say hello")]
[GenerateSerializer]
public class SimpleGEvent : EventBase
{
    [Description("")]
    [Id(0)]
    public string Hello { get; set; }
}