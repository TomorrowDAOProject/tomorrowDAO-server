using Newtonsoft.Json.Serialization;

namespace TomorrowDAOServer.Auth.Http;

public interface IHttpClientService
{
    Task<T> PostAsync<T>(string url, object paramObj, int timeout = 10, IContractResolver resolver = null);
    Task<T> GetAsync<T>(string url, Dictionary<string, string> param, int timeout = 10, IContractResolver resolver = null);
}