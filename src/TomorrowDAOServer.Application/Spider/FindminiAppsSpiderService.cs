using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using HtmlAgilityPack;
using TomorrowDAOServer.DAO.Provider;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Telegram.Dto;

namespace TomorrowDAOServer.Spider;

public class FindminiAppsSpiderService : TomorrowDAOServerAppService, IFindminiAppsSpiderService
{
    private readonly IDaoAliasProvider _daoAliasProvider;

    public FindminiAppsSpiderService(IDaoAliasProvider daoAliasProvider)
    {
        _daoAliasProvider = daoAliasProvider;
    }

    public async Task<List<TelegramAppDto>> LoadAsync(string url)
    {
        var web = new HtmlWeb();
        var doc = web.Load(url);
        var dtos = new List<TelegramAppDto>();
        var appNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'flex') and contains(@class, 'items-center') and contains(@class, 'rounded-2xl')]");

        if (appNodes == null)
        {
            return dtos;
        }

        foreach (var appNode in appNodes)
        {
            var anchorNode = appNode.SelectSingleNode(".//a[@class='flex w-full']");
            var imgNode = anchorNode.SelectSingleNode(".//img");
            var descNode = appNode.SelectSingleNode(".//p[contains(@class, 'line-clamp-2')]");
            var descUrl = anchorNode?.GetAttributeValue("href", string.Empty) ?? string.Empty;
            var title = imgNode?.GetAttributeValue("alt", string.Empty) ?? string.Empty;
            if (string.IsNullOrEmpty(title))
            {
                continue;
            }
            var telegramAppDto = new TelegramAppDto
            {
                Id = HashHelper.ComputeFrom(title).ToHex(),
                Alias = await _daoAliasProvider.GenerateDaoAliasAsync(title), Title = title,
                Icon = imgNode?.GetAttributeValue("src", string.Empty) ?? string.Empty,
                Description = descNode?.InnerText.Trim(), SourceType = SourceType.FindMini
            };
            dtos.Add(telegramAppDto);
            if (string.IsNullOrEmpty(descUrl))
            {
                continue;
            }

            descUrl = "https://www.findmini.app/" + descUrl;
            var descWeb = new HtmlWeb();
            var descDoc = descWeb.Load(descUrl);
            var screenImgNodes = descDoc.DocumentNode.SelectNodes("//div[contains(@class, 'flex') and contains(@class, 'snap-x') and contains(@class, 'overflow-x-auto')]//img");
            var spanNode = descDoc.DocumentNode.SelectSingleNode("//h2[contains(text(), 'Description')]/following-sibling::span");
            var buttonNode = descDoc.DocumentNode.SelectSingleNode("//button[contains(@onclick, 'window.open') and contains(@class, 'bg-telegram')]");
            var onClickAttribute = buttonNode?.GetAttributeValue("onclick", string.Empty) ?? string.Empty;
            var startIndex = onClickAttribute.IndexOf("window.open('", StringComparison.Ordinal) + "window.open('".Length;
            var endIndex = onClickAttribute.IndexOf("'", startIndex, StringComparison.Ordinal);
            telegramAppDto.Screenshots = screenImgNodes?
                .Select(screenImgNode => screenImgNode.GetAttributeValue("src", string.Empty))
                .Where(screen => !string.IsNullOrEmpty(screen)).ToList() ?? new List<string>();
            telegramAppDto.Url =  onClickAttribute.Substring(startIndex, endIndex - startIndex);
            telegramAppDto.LongDescription = spanNode?.InnerText.Trim() ?? string.Empty;
        }

        return dtos;
    }
}