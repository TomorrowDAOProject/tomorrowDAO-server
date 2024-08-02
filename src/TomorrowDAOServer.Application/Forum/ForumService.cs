using System;
using System.IO;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using PuppeteerSharp;
using PuppeteerSharp.BrowserData;
using TomorrowDAOServer.Common.Enum;
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
            return input.AnalyzerType switch
            {
                AnalyzerType.SeleniumWebDriver => await AnalyzePageBySeleniumWebDriverAsync(input.ForumUrl),
                AnalyzerType.PuppeteerSharp => await AnalyzePageByPuppeteerSharpAsync(input.ForumUrl),
                _ => await AnalyzePageByHtmlAgilityPackAsync(input.ForumUrl)
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "exec LinkPreviewAsync error, {0}", JsonConvert.SerializeObject(input));
            return await Task.FromResult(new LinkPreviewDto());
        }
    }

    private Task<LinkPreviewDto> AnalyzePageByHtmlAgilityPackAsync(string url)
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

    private Task<LinkPreviewDto> AnalyzePageBySeleniumWebDriverAsync(string url)
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

    public async Task<LinkPreviewDto> AnalyzePageByPuppeteerSharpAsync(string url, bool download = true)
    {
        var browserFetcher = new BrowserFetcher(new BrowserFetcherOptions
        {
            Browser = SupportedBrowser.Chromium
        });
        _logger.LogInformation(
            "Puppeteer: Browser={Browser}, CacheDir={CacheDir}, Platform={Platform}",
            browserFetcher.Browser, browserFetcher.CacheDir, browserFetcher.Platform);

        if (download)
        {
            await browserFetcher.DownloadAsync();
        }

        var launchOptions = new LaunchOptions { Headless = true };
        await using var browser = await Puppeteer.LaunchAsync(launchOptions);
        await using var page = await browser.NewPageAsync();
        await page.GoToAsync(url);

        var title = await page.EvaluateExpressionAsync<string>(
            "document.querySelector('meta[property=\"og:title\"]') ? document.querySelector('meta[property=\"og:title\"]').content : ''");
        var description = await page.EvaluateExpressionAsync<string>(
            "document.querySelector('meta[property=\"og:description\"]') ? document.querySelector('meta[property=\"og:description\"]').content : ''");
        var faviconUrl = await page.EvaluateExpressionAsync<string>(
            "document.querySelector('meta[property=\"og:image\"]') ? document.querySelector('meta[property=\"og:image\"]').content : ''");

        if (title.IsNullOrWhiteSpace())
        {
            title = await page.GetTitleAsync();
        }

        if (description.IsNullOrWhiteSpace())
        {
            description = await page.EvaluateExpressionAsync<string>(
                "document.querySelector('meta[name=\"description\"]') ? document.querySelector('meta[name=\"description\"]').content : ''");
        }

        if (faviconUrl.IsNullOrWhiteSpace())
        {
            var relativeUrl = await page.EvaluateExpressionAsync<string>(
                "document.querySelector('link[rel=\"icon\"]') ? document.querySelector('link[rel=\"icon\"]').href : ''");
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

            if (string.IsNullOrEmpty(faviconUrl))
            {
                var baseUri = new Uri(url);
                faviconUrl = new Uri(baseUri, "/favicon.ico").AbsoluteUri;
            }
        }

        return new LinkPreviewDto
        {
            Title = title,
            Description = description,
            Favicon = faviconUrl
        };
    }
}