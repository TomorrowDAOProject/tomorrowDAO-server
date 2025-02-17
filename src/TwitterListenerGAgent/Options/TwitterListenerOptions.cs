using Aevatar.Core.Abstractions;

namespace AevatarTemplate.GAgents.Options;

public class TwitterListenerOptions : InitializationEventBase
{
    public string KOL { get; set; }
    public string BearerToken { get; set; }
    public string AccessToken { get; set; }
    public string AccessTokenSecret { get; set; }
    
    public string ConsumerKey { get; set; }
    public string ConsumerSecret { get; set; }
    public string EncryptionPassword { get; set; }
    
    public int ReplyLimit { get; set; }
}