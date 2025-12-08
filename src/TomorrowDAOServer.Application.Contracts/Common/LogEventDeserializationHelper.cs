using System.Collections.Generic;
using System.Linq;
using AElf.Client.Dto;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using Google.Protobuf;
using TomorrowDAO.Contracts.Vote;

namespace TomorrowDAOServer.Common;

public static class LogEventDeserializationHelper
{
    public static T DeserializeLogEvent<T>(LogEventDto logEvent) where T : IEvent<T>, new()
    {
        var indexedList = logEvent.Indexed.ToList();
        var nonIndexed = logEvent.NonIndexed;

        var @event = new AElf.Types.LogEvent
        {
            Indexed = { indexedList.Select(ByteString.FromBase64) },
            NonIndexed = ByteString.FromBase64(nonIndexed)
        };

        var message = new T();
        message.MergeFrom(@event);
        return message;
    }

    
}