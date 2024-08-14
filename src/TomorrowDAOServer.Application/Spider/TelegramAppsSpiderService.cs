using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using AElf;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TomorrowDAOServer.Common.HttpClient;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Telegram.Dto;
using TomorrowDAOServer.User.Provider;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Users;

namespace TomorrowDAOServer.Spider;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class TelegramAppsSpiderService : TomorrowDAOServerAppService, ITelegramAppsSpiderService
{
    private readonly ILogger<TelegramAppsSpiderService> _logger;
    private readonly IHttpProvider _httpProvider;
    private readonly IDaoAliasProvider _daoAliasProvider;
    private readonly IOptionsMonitor<TelegramOptions> _telegramOptions;
    private readonly IUserProvider _userProvider;

    public TelegramAppsSpiderService(ILogger<TelegramAppsSpiderService> logger, IHttpProvider httpProvider,
        IDaoAliasProvider daoAliasProvider, IOptionsMonitor<TelegramOptions> telegramOptions,
        IUserProvider userProvider)
    {
        _logger = logger;
        _httpProvider = httpProvider;
        _daoAliasProvider = daoAliasProvider;
        _telegramOptions = telegramOptions;
        _userProvider = userProvider;
    }

    public async Task<List<TelegramAppDto>> LoadTelegramAppsAsync(LoadTelegramAppsInput input)
    {
        if (input == null || input.Url.IsNullOrWhiteSpace() || input.ChainId.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException("Invalid input.");
        }

        var address = await _userProvider.GetAndValidateUserAddressAsync(CurrentUser.GetId(), input.ChainId);
        if (!_telegramOptions.CurrentValue.AllowedCrawlUsers.Contains(address))
        {
            throw new UserFriendlyException("Access denied.");
        }

        try
        {
            return input.ContentType switch
            {
                ContentType.Body => await AnalyzePageBodyByHtmlAgilityPackAsync(input.Url),
                ContentType.Script => await AnalyzePageScriptByHtmlAgilityPackAsync(input.Url),
                _ => throw new UserFriendlyException("Unsupported ContentType.")
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "exec LoadTelegramAppsAsync error, {0}", JsonConvert.SerializeObject(input));
            throw;
        }
    }

    private async Task<List<TelegramAppDto>> AnalyzePageBodyByHtmlAgilityPackAsync(string url)
    {
        var web = new HtmlWeb();
        var doc = web.Load(url);

        var tabDivNodes =
            doc.DocumentNode.SelectNodes(
                "//div[contains(@class, 'styles_root__oo9K5') and contains(@class, 'styles_noFocus__C9TVO')]");

        var dtos = new List<TelegramAppDto>();
        if (tabDivNodes.IsNullOrEmpty())
        {
            return dtos;
        }

        foreach (var tabDivNode in tabDivNodes)
        {
            var telegramAppDto = new TelegramAppDto();
            telegramAppDto.Icon = AnalyzeImageDiv(tabDivNode);
            var title = telegramAppDto.Title = AnalyzeTitle(tabDivNode);
            if (title.IsNullOrWhiteSpace())
            {
                continue;
            }

            telegramAppDto.Description = AnalyzeDescription(tabDivNode);
            telegramAppDto.EditorChoice = AnalyzeEditorChoice(tabDivNode);
            telegramAppDto.Alias = await _daoAliasProvider.GenerateDaoAliasAsync(telegramAppDto.Title);
            telegramAppDto.Id = HashHelper.ComputeFrom(telegramAppDto.Title).ToHex();
            dtos.Add(telegramAppDto);
        }

        return dtos;
    }

    private string AnalyzeImageDiv(HtmlNode tabDivNode)
    {
        var imageDivNode = tabDivNode.ChildNodes.FirstOrDefault(n =>
            n.Name == "div" && n.Attributes["class"]?.Value.Contains("styles_imageContainer__ci_hN") == true);
        if (imageDivNode == null)
        {
            return string.Empty;
        }

        var imageNode = imageDivNode.SelectSingleNode(".//img");
        var imageSrc = imageNode?.GetAttributeValue("src", string.Empty);
        if (imageSrc.IsNullOrWhiteSpace())
        {
            return string.Empty;
        }

        var match = Regex.Match(imageSrc, @"url=([^&]*)");
        if (!match.Success)
        {
            return string.Empty;
        }

        var encodedUrl = match.Groups[1].Value;
        var decodedUrl = HttpUtility.UrlDecode(encodedUrl);
        //var startIndex = decodedUrl.IndexOf("https");
        //if (startIndex >= 0)
        //{
        //    var endIndex = decodedUrl.LastIndexOf(".jpg") + 4;
        //    return decodedUrl.Substring(startIndex, endIndex - startIndex);
        //}
        return decodedUrl;
    }

    private string AnalyzeTitle(HtmlNode tabDivNode)
    {
        var contentNode = tabDivNode.ChildNodes.FirstOrDefault(n =>
            n.Name == "div" && n.Attributes["class"]?.Value.Contains("styles_body__n8tvE") == true);
        if (contentNode == null)
        {
            return string.Empty;
        }

        var titleDivNode = contentNode.ChildNodes.FirstOrDefault(n =>
            n.Name == "div" && n.Attributes["class"]?.Value.Contains("styles_content__siHnh") == true);
        var titleNode = titleDivNode?.ChildNodes.FirstOrDefault(n => n.Name?.ToUpper() == "P");
        return titleNode != null ? titleNode.InnerText : string.Empty;
    }

    private bool AnalyzeEditorChoice(HtmlNode tabDivNode)
    {
        var contentNode = tabDivNode.ChildNodes.FirstOrDefault(n =>
            n.Name == "div" && n.Attributes["class"]?.Value.Contains("styles_body__n8tvE") == true);
        if (contentNode == null)
        {
            return false;
        }

        var choiceDivNode = contentNode.ChildNodes.FirstOrDefault(n =>
            n.Name == "div" && n.Attributes["class"]?.Value.Contains("styles_content__siHnh") == true);
        var titleNode = choiceDivNode?.ChildNodes.FirstOrDefault(n =>
            n.Name?.ToLower() == "div" &&
            n.Attributes["class"]?.Value.Contains("styles_additionInfoContainer__of5py") == true);
        return titleNode != null ? !titleNode.ChildNodes.IsNullOrEmpty() : false;
    }

    private string AnalyzeDescription(HtmlNode tabDivNode)
    {
        var contentNode = tabDivNode.ChildNodes.FirstOrDefault(n =>
            n.Name == "div" && n.Attributes["class"]?.Value.Contains("styles_body__n8tvE") == true);
        if (contentNode == null)
        {
            return string.Empty;
        }

        var descriptionDivNode = contentNode.ChildNodes.FirstOrDefault(n =>
            n.Name == "div" && n.Attributes["class"]?.Value.Contains("styles_content__siHnh") == true);
        var descriptionNode = descriptionDivNode.ChildNodes.FirstOrDefault(n => n.Name == "span");
        return descriptionNode != null ? descriptionNode.InnerText : string.Empty;
    }


    private async Task<List<TelegramAppDto>> AnalyzePageScriptByHtmlAgilityPackAsync(string url)
    {
        throw new UserFriendlyException("Analyze script is not supported yet.");
        // var web = new HtmlWeb();
        // var doc = web.Load(url);
        // var scriptNodes = doc.DocumentNode.SelectNodes("//script[contains(text(), 'self.__next_f.push')]");
    }
}