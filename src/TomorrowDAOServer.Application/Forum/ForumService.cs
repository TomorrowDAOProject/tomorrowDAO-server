using System;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using TomorrowDAOServer.Forum.Dto;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace TomorrowDAOServer.Forum;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class ForumService : TomorrowDAOServerAppService, IForumService
{
    private readonly ILogger<ForumService> _logger;

    public ForumService(ILogger<ForumService> logger)
    {
        _logger = logger;
    }

    public async Task<LinkPreviewDto> LinkPreviewAsync(LinkPreviewInput input)
    {
        if (input == null || input.ForumUrl.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException("Invalid input.");
        }

        try
        {
            return await AnalyzePageByHtmlAgilityPack(input.ForumUrl);
            //return await AnalyzePageBySeleniumWebDriver(input.ForumUrl);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "exec LinkPreviewAsync error, {0}", JsonConvert.SerializeObject(input));
            return await Task.FromResult(new LinkPreviewDto());
        }
    }

    private Task<LinkPreviewDto> AnalyzePageByHtmlAgilityPack(string url)
    {
        var web = new HtmlWeb();
        var doc = web.Load(url);

        var title = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']")
            ?.GetAttributeValue("content", "");
        var description = doc.DocumentNode.SelectSingleNode("//meta[@property='og:description']")
            ?.GetAttributeValue("content", "");
        var faviconUrl = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']")
            ?.GetAttributeValue("content", "");

        if (title.IsNullOrWhiteSpace())
        {
            title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText;
        }

        if (description.IsNullOrWhiteSpace())
        {
            description = doc.DocumentNode.SelectSingleNode("//meta[@name='description']")
                ?.GetAttributeValue("content", "");
        }

        if (faviconUrl.IsNullOrWhiteSpace())
        {
            var faviconNode = doc.DocumentNode.SelectSingleNode("//link[@rel='icon' or @rel='shortcut icon']");
            if (faviconNode != null)
            {
                var relativeUrl = faviconNode.GetAttributeValue("href", "");
                if (Uri.IsWellFormedUriString(relativeUrl, UriKind.Absolute))
                {
                    faviconUrl = relativeUrl;
                }
                else
                {
                    var baseUri = new Uri(url);
                    var faviconUri = new Uri(baseUri, relativeUrl);
                    faviconUrl = faviconUri.AbsoluteUri;
                }
            }

            if (string.IsNullOrEmpty(faviconUrl))
            {
                var baseUri = new Uri(url);
                faviconUrl = new Uri(baseUri, "/favicon.ico").AbsoluteUri;
            }
        }

        return Task.FromResult(new LinkPreviewDto
        {
            Title = title,
            Description = description,
            Favicon = faviconUrl
        });
    }

    private Task<LinkPreviewDto> AnalyzePageBySeleniumWebDriver(string url)
    {
        ChromeOptions options = new ChromeOptions();
        options.AddArgument("--headless");

        using (var driver = new ChromeDriver(options))
        {
            driver.Navigate().GoToUrl(url);

            var title = driver.FindElement(By.CssSelector("meta[property='og:title']"))?.GetAttribute("content");
            var description = driver.FindElement(By.CssSelector("meta[property='og:description']"))
                ?.GetAttribute("content");
            var faviconUrl = driver.FindElement(By.CssSelector("meta[property='og:image']"))?.GetAttribute("content");

            if (title.IsNullOrWhiteSpace())
            {
                title = driver.Title;
            }

            if (description.IsNullOrWhiteSpace())
            {
                var descriptionElement = driver.FindElement(By.CssSelector("meta[name='description']"));
                description = descriptionElement?.GetAttribute("content");
            }

            if (faviconUrl.IsNullOrWhiteSpace())
            {
                var faviconElement = driver.FindElement(By.CssSelector("link[rel='icon'], link[rel='shortcut icon']"));
                if (faviconElement != null)
                {
                    var relativeUrl = faviconElement.GetAttribute("href");
                    if (Uri.IsWellFormedUriString(relativeUrl, UriKind.Absolute))
                    {
                        faviconUrl = relativeUrl;
                    }
                    else
                    {
                        var baseUri = new Uri(url);
                        var faviconUri = new Uri(baseUri, relativeUrl);
                        faviconUrl = faviconUri.AbsoluteUri;
                    }
                }

                if (string.IsNullOrEmpty(faviconUrl))
                {
                    var baseUri = new Uri(url);
                    faviconUrl = new Uri(baseUri, "/favicon.ico").AbsoluteUri;
                }
            }

            return Task.FromResult(new LinkPreviewDto
            {
                Title = title,
                Description = description,
                Favicon = faviconUrl
            });
        }
    }
}