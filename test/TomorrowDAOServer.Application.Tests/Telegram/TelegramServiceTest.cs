using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Runtime.Internal.Transform;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Enums;
using TomorrowDAOServer.Options;
using TomorrowDAOServer.Telegram.Dto;
using TomorrowDAOServer.Telegram.Provider;
using Volo.Abp;
using Volo.Abp.Validation;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Telegram;

public partial class TelegramServiceTest : TomorrowDaoServerApplicationTestBase
{
    private readonly ITelegramService _telegramService;
    private readonly ITelegramAppsProvider _telegramAppsProvider;
    private TelegramOptions _telegramOptions;

    public TelegramServiceTest(ITestOutputHelper output) : base(output)
    {
        _telegramService = ServiceProvider.GetRequiredService<ITelegramService>();
        _telegramAppsProvider = ServiceProvider.GetRequiredService<ITelegramAppsProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockTelegramOptions(out TelegramOptions telegramOptions));
        _telegramOptions = telegramOptions;
        services.AddSingleton(MockTelegramAppIndexRepository());
    }

    [Fact]
    public async Task SaveTelegramAppsAsyncTest()
    {
        await _telegramService.SaveTelegramAppsAsync(new List<TelegramAppDto>());

        await _telegramService.SaveTelegramAppsAsync(new List<TelegramAppDto>
        {
            new TelegramAppDto
            {
                Id = Guid.NewGuid().ToString(),
                Alias = "Alias",
                Title = "Title",
                Icon = "Icon",
                Description = "Description",
                EditorChoice = false
            }
        });
    }

    [Fact]
    public async Task SaveTelegramAppAsyncTest()
    {
        var exception = await Assert.ThrowsAsync<Volo.Abp.Validation.AbpValidationException>(async () =>
        {
            await _telegramService.SaveTelegramAppAsync(new BatchSaveAppsInput());
        });
        exception.Message.ShouldContain("Method arguments are not valid");


        Login(Guid.NewGuid(), Address2);
        await _telegramService.SaveTelegramAppAsync(new BatchSaveAppsInput
        {
            ChainId = ChainIdAELF,
            Apps = new List<SaveTelegramAppsInput>()
            {
                new SaveTelegramAppsInput
                {
                    Title = "Title",
                    Icon = "Icon",
                    Description = "Description",
                    SourceType = SourceType.Telegram
                }
            },
        });
    }

    [Fact]
    public async Task SaveTelegramAppAsyncTest_AccessDenied()
    {
        Login(Guid.NewGuid(), Address1);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(async () =>
        {
            await _telegramService.SaveTelegramAppAsync(new BatchSaveAppsInput()
            {
                ChainId = ChainIdAELF,
                Apps = new List<SaveTelegramAppsInput>()
                {
                    new SaveTelegramAppsInput
                    {
                        Title = "Title",
                        Icon = "Icon",
                        Description = "Description",
                        SourceType = SourceType.Telegram
                    }
                },
            });
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldNotBeNull();
        exception.Message.ShouldBe("Nft Not enough.");
    }

    [Fact]
    public async Task SetCategoryAsyncTest()
    {
        var address = Base58Encoder.GenerateRandomBase58String(50);
        Login(Guid.NewGuid(), Address2);

        await _telegramService.SetCategoryAsync(ChainIdAELF);

        _telegramOptions.Types = "Alias:Game+Earn";
        await _telegramService.SetCategoryAsync(ChainIdAELF);
        _telegramOptions.Types = null;
    }

    [Fact]
    public async Task SaveNewTelegramAppsAsyncTest()
    {
        await _telegramService.SaveNewTelegramAppsAsync(new List<TelegramAppDto>());

        await _telegramService.SaveNewTelegramAppsAsync(new List<TelegramAppDto>()
        {
            new TelegramAppDto
            {
                Id = "Id1",
                Alias = "Alias1",
                Title = "Title1",
                Icon = "Icon1",
                Description = "Description1",
                EditorChoice = true,
                Url = "Ur1l",
                LongDescription = "LongDescription1",
                Screenshots = new List<string>() { "Screenshots1" },
                Categories = new List<TelegramAppCategory>() { TelegramAppCategory.Earn, TelegramAppCategory.Game },
                CreateTime = DateTime.UtcNow.AddDays(-5),
                UpdateTime = DateTime.UtcNow.AddDays(-5),
                SourceType = SourceType.Telegram,
                Creator = Address1,
                LoadTime = DateTime.UtcNow.AddDays(-5),
                BackIcon = "BackIcon1",
                BackScreenshots = new List<string>() { "BackScreenshots1" },
                TotalPoints = 10,
                TotalVotes = 10,
                TotalLikes = 10,
            },
            new TelegramAppDto
            {
                Id = "Id",
                Alias = "Alias",
                Title = "Title",
                Icon = "Icon",
                Description = "Description",
                EditorChoice = true,
                Url = "Url",
                LongDescription = "LongDescription",
                Screenshots = new List<string>() { "Screenshots" },
                Categories = new List<TelegramAppCategory>() { TelegramAppCategory.Earn, TelegramAppCategory.Game },
                CreateTime = DateTime.UtcNow.AddDays(-5),
                UpdateTime = DateTime.UtcNow.AddDays(-5),
                SourceType = SourceType.Telegram,
                Creator = Address1,
                LoadTime = DateTime.UtcNow.AddDays(-5),
                BackIcon = "BackIcon",
                BackScreenshots = new List<string>() { "BackScreenshots" }
            }
        });
    }

    // [Fact]
    // public async Task GetTelegramAppAsyncTest()
    // {
    //     var telegramAppDtos = await _telegramService.GetTelegramAppAsync(new QueryTelegramAppsInput());
    //     telegramAppDtos.ShouldBeEmpty();
    //
    //     telegramAppDtos = await _telegramService.GetTelegramAppAsync(new QueryTelegramAppsInput
    //     {
    //         Aliases = new List<string>() { "Aliases" }
    //     });
    //     telegramAppDtos.ShouldNotBeNull();
    //     telegramAppDtos.Count.ShouldBe(1);
    // }

    [Fact]
    public async Task SaveTelegramAppDetailAsyncTest()
    {
        var dictionary =
            await _telegramService.SaveTelegramAppDetailAsync(new Dictionary<string, TelegramAppDetailDto>());
        dictionary.ShouldBeEmpty();

        dictionary = new Dictionary<string, TelegramAppDetailDto>()
        {
            {
                "Title", new TelegramAppDetailDto
                {
                    Data = new List<TelegramAppDetailData>()
                    {
                        new TelegramAppDetailData
                        {
                            Id = 1,
                            Attributes = new TelegramAppDetailDataAttr
                            {
                                Title = "Title",
                                Description = "Description",
                                Url = "Url",
                                Path = "Path",
                                CreatedAt = "CreatedAt",
                                UpdatedAt = "CreatedAt",
                                PublishedAt = "PublishedAt",
                                Locale = null,
                                EditorsChoice = null,
                                WebappUrl = null,
                                CommunityUrl = null,
                                Long_description = null,
                                StartParam = null,
                                Ecosystem = null,
                                Ios = false,
                                AnalyticsId = null,
                                Screenshots = new TelegramAppScreenshots
                                {
                                    Data = new List<TelegramAppScreenshotsItem>()
                                    {
                                        new TelegramAppScreenshotsItem
                                        {
                                            Id = "Id",
                                            Attributes = new TelegramAppImageAttributes
                                            {
                                                Name = "Name",
                                                AlternativeText = "AlternativeText",
                                                Caption = "Caption",
                                                Width = 0,
                                                Height = 0,
                                                Hash = null,
                                                Ext = null,
                                                Mime = null,
                                                Size = 0,
                                                Url = null,
                                                PreviewUrl = null,
                                                Provider = null,
                                                ProviderMetadata = null,
                                                CreatedAt = null,
                                                UpdatedAt = null
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    Meta = new TelegramAppDetailMeta
                    {
                        Pagination = new TelegramAppDetailMetaPagination
                        {
                            Start = 0,
                            Limit = 0,
                            Total = 0
                        }
                    }
                }
            }
        };
        dictionary = await _telegramService.SaveTelegramAppDetailAsync(dictionary);
        dictionary.ShouldNotBeNull();
        dictionary.Count.ShouldBe(1);

        dictionary["Alias"] = dictionary["Title"];
        dictionary.Remove("Title");
        dictionary = await _telegramService.SaveTelegramAppDetailAsync(dictionary);
        dictionary.ShouldNotBeNull();
        dictionary.Count.ShouldBe(0);
    }

    [Fact]
    public async Task SearchAppAsyncTest()
    {
        var pageResultDto = await _telegramService.SearchAppAsync("Title");
        pageResultDto.ShouldNotBeNull();
        pageResultDto.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task AddAppAsyncTest()
    {
        var address = Base58Encoder.GenerateRandomBase58String(50);
        Login(Guid.NewGuid(), Address2);
        var result = await _telegramService.AddAppAsync(new AddAppInput
        {
            ChainId = ChainIdAELF,
            Title = "Title3",
            Icon = "Icon3",
            Description = "Description3",
            Url = "Url3",
            LongDescription = "LongDescription3",
            Screenshots = new List<string>()
            {
                "Screenshots3"
            },
            Categories = new List<TelegramAppCategory>() { TelegramAppCategory.Earn }
        });
        result.ShouldBeTrue();
    } 
}