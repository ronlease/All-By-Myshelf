// Feature: BGG credential validation (ABM-064, ABM-065)
//
// Scenario: TryStartSync returns TokenNotConfigured when ApiToken is missing
//   Given BoardGameGeekOptions has ApiToken empty
//   When TryStartSync is called
//   Then SyncStartResult.TokenNotConfigured is returned
//
// Scenario: TryStartSync returns TokenNotConfigured when Username is missing
//   Given BoardGameGeekOptions has Username empty but ApiToken configured
//   When TryStartSync is called
//   Then SyncStartResult.TokenNotConfigured is returned
//
// Scenario: TryStartSync returns TokenNotConfigured when both are missing
//   Given BoardGameGeekOptions has empty ApiToken and Username
//   When TryStartSync is called
//   Then SyncStartResult.TokenNotConfigured is returned
//
// Scenario: TryStartSync returns Started when both ApiToken and Username are configured
//   Given BoardGameGeekOptions has both ApiToken and Username configured
//   When TryStartSync is called
//   Then SyncStartResult.Started is returned
//
// Feature: Background sync of BoardGameGeek collection (ABM-064)
//
// Scenario: Sync fetches the collection and upserts mapped board games
//   Given BoardGameGeek returns a collection of owned games
//   And thing details are available for those games
//   When the background loop runs a sync
//   Then UpsertCollectionAsync is called with the mapped board games
//
// Scenario: Thing details are requested in batches of twenty
//   Given the collection contains 21 games
//   When the background loop runs a sync
//   Then thing details are requested twice
//
// Scenario: A game with no matching thing detail still syncs
//   Given the collection contains a game with no thing detail
//   When the background loop runs a sync
//   Then the board game is stored with no description and no designers
//
// Scenario: Sync completes and clears the running flag
//   Given a BoardGameGeek sync is running
//   When the sync completes
//   Then IsSyncRunning becomes false
//
// Scenario: Sync failure is swallowed and the running flag is cleared
//   Given the repository throws while persisting the collection
//   When the background loop runs a sync
//   Then the exception does not escape the loop
//   And IsSyncRunning becomes false
//
// Scenario: Sync cancelled by application shutdown is handled
//   Given a sync is in flight
//   When the host stops the background service
//   Then the cancellation is handled without faulting the service

//
// Feature: Hot-reload API configuration after settings change (ABM-075)
//
// Scenario: Credentials saved after startup take effect without a restart
//   Given the service started with no credentials configured
//   And TryStartSync reports the token is not configured
//   When credentials are saved and configuration reloads
//   Then the same running service reports the sync can start
//   And the API does not need to be restarted
//
// Scenario: Credentials cleared after startup stop taking effect
//   Given the service started with credentials configured
//   When the credentials are cleared and configuration reloads
//   Then the same running service reports the token is not configured
using System.Net;
using System.Text;
using AllByMyshelf.Api.Common;
using AllByMyshelf.Api.Features.BoardGameGeek;
using AllByMyshelf.Api.Models.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using AllByMyshelf.Unit.TestDoubles;

namespace AllByMyshelf.Unit.Services;

public class BoardGamesSyncServiceTests
{
    // ── TryStartSync — missing ApiToken ───────────────────────────────────────

    [Fact]
    public void TryStartSync_ApiTokenEmpty_ReturnsTokenNotConfigured()
    {
        // Arrange
        var sut = CreateService(apiToken: string.Empty, username: "myuser");

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.TokenNotConfigured);
    }

    [Fact]
    public void TryStartSync_ApiTokenNull_ReturnsTokenNotConfigured()
    {
        // Arrange
        var sut = CreateService(apiToken: null, username: "myuser");

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.TokenNotConfigured);
    }

    [Fact]
    public void TryStartSync_ApiTokenWhitespace_ReturnsTokenNotConfigured()
    {
        // Arrange
        var sut = CreateService(apiToken: "   ", username: "myuser");

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.TokenNotConfigured);
    }

    // ── TryStartSync — missing Username ─────────────────────────────────────

    [Fact]
    public void TryStartSync_UsernameEmpty_ReturnsTokenNotConfigured()
    {
        // Arrange
        var sut = CreateService(apiToken: "my-token", username: string.Empty);

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.TokenNotConfigured);
    }

    [Fact]
    public void TryStartSync_UsernameNull_ReturnsTokenNotConfigured()
    {
        // Arrange
        var sut = CreateService(apiToken: "my-token", username: null);

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.TokenNotConfigured);
    }

    [Fact]
    public void TryStartSync_UsernameWhitespace_ReturnsTokenNotConfigured()
    {
        // Arrange
        var sut = CreateService(apiToken: "my-token", username: "   ");

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.TokenNotConfigured);
    }

    // ── TryStartSync — both missing ───────────────────────────────────────────

    [Fact]
    public void TryStartSync_BothApiTokenAndUsernameEmpty_ReturnsTokenNotConfigured()
    {
        // Arrange
        var sut = CreateService(apiToken: string.Empty, username: string.Empty);

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.TokenNotConfigured);
    }

    // ── TryStartSync — both configured ──────────────────────────────────────

    [Fact]
    public void TryStartSync_BothApiTokenAndUsernameConfigured_ReturnsStarted()
    {
        // Arrange
        var sut = CreateService(apiToken: "my-token", username: "myuser");

        // Act
        var result = sut.TryStartSync();

        // Assert
        result.Should().Be(SyncStartResult.Started);
    }

    // ── Hot-reload of credentials (ABM-075) ──────────────────────────────────

    [Fact]
    public void TryStartSync_UsernameSavedAfterStartup_StartsWithoutRestart()
    {
        // Arrange — mirrors a fresh install: token in user-secrets, username not yet saved
        var options = new TestOptionsMonitor<BoardGameGeekOptions>(new BoardGameGeekOptions
        {
            ApiToken = "my-token",
            Username = string.Empty
        });
        var sut = CreateService(options);
        sut.TryStartSync().Should().Be(SyncStartResult.TokenNotConfigured);

        // Act — the Settings page saves the username and configuration reloads
        options.Set(new BoardGameGeekOptions { ApiToken = "my-token", Username = "myuser" });

        // Assert — the same instance picks it up, no restart required
        sut.TryStartSync().Should().Be(SyncStartResult.Started);
    }

    [Fact]
    public void TryStartSync_CredentialsClearedAfterStartup_StopsReportingConfigured()
    {
        // Arrange
        var options = new TestOptionsMonitor<BoardGameGeekOptions>(new BoardGameGeekOptions
        {
            ApiToken = "my-token",
            Username = "myuser"
        });
        var sut = CreateService(options);

        // Act
        options.Set(new BoardGameGeekOptions { ApiToken = string.Empty, Username = string.Empty });

        // Assert
        sut.TryStartSync().Should().Be(SyncStartResult.TokenNotConfigured);
    }

    // ── RunSyncAsync — collection fetch and mapping ───────────────────────────

    [Fact]
    public async Task RunSyncAsync_CollectionReturned_UpsertsMappedBoardGames()
    {
        // Arrange
        var handler = new BoardGameGeekHandler(
            BuildCollectionXml((10, "Gloomhaven"), (20, "Wingspan")),
            BuildThingXml((10, "Dungeon crawler.", "Isaac Childres", "Adventure"),
                          (20, "Bird engine builder.", "Elizabeth Hargrave", "Animals")));
        var (sut, repository) = CreateServiceWithScope(handler);
        var upsert = CaptureUpsert(repository);

        // Act
        var captured = await RunSyncAndCaptureAsync(sut, upsert);

        // Assert
        captured.Should().HaveCount(2);
        captured.Select(b => b.BoardGameGeekId).Should().Equal(10, 20);
        captured.Select(b => b.Title).Should().Equal("Gloomhaven", "Wingspan");
    }

    [Fact]
    public async Task RunSyncAsync_ThingDetailAvailable_MapsEnrichmentFields()
    {
        // Arrange
        var handler = new BoardGameGeekHandler(
            BuildCollectionXml((10, "Gloomhaven")),
            BuildThingXml((10, "Dungeon crawler.", "Isaac Childres", "Adventure")));
        var (sut, repository) = CreateServiceWithScope(handler);
        var upsert = CaptureUpsert(repository);

        // Act
        var captured = await RunSyncAndCaptureAsync(sut, upsert);

        // Assert
        var game = captured.Single();
        game.Description.Should().Be("Dungeon crawler.");
        game.Designers.Should().Equal("Isaac Childres");
        game.Genre.Should().Be("Adventure");
    }

    [Fact]
    public async Task RunSyncAsync_CollectionItemFields_AreMappedOntoEntity()
    {
        // Arrange
        var handler = new BoardGameGeekHandler(
            BuildCollectionXml((10, "Gloomhaven")),
            BuildThingXml((10, "Dungeon crawler.", "Isaac Childres", "Adventure")));
        var (sut, repository) = CreateServiceWithScope(handler);
        var upsert = CaptureUpsert(repository);

        // Act
        var captured = await RunSyncAndCaptureAsync(sut, upsert);

        // Assert — values come from BuildCollectionXml
        var game = captured.Single();
        game.CoverImageUrl.Should().Be("https://example.test/image-10.jpg");
        game.ThumbnailUrl.Should().Be("https://example.test/thumb-10.jpg");
        game.MinPlayers.Should().Be(1);
        game.MaxPlayers.Should().Be(4);
        game.MinPlaytime.Should().Be(30);
        game.MaxPlaytime.Should().Be(120);
        game.YearPublished.Should().Be(2017);
        game.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RunSyncAsync_NoThingDetailForGame_MapsNullDescriptionAndEmptyDesigners()
    {
        // Arrange — the thing endpoint returns details for a different game
        var handler = new BoardGameGeekHandler(
            BuildCollectionXml((10, "Gloomhaven")),
            BuildThingXml((99, "Unrelated.", "Someone Else", "Party")));
        var (sut, repository) = CreateServiceWithScope(handler);
        var upsert = CaptureUpsert(repository);

        // Act
        var captured = await RunSyncAndCaptureAsync(sut, upsert);

        // Assert
        var game = captured.Single();
        game.Description.Should().BeNull();
        game.Designers.Should().BeEmpty();
        game.Genre.Should().BeNull();
    }

    // ── RunSyncAsync — batching ───────────────────────────────────────────────

    [Fact]
    public async Task RunSyncAsync_MoreThanTwentyGames_RequestsThingDetailsInTwoBatches()
    {
        // Arrange — 21 games forces a second batch and the inter-batch delay
        var games = Enumerable.Range(1, 21).Select(i => (i, $"Game {i}")).ToArray();
        var handler = new BoardGameGeekHandler(BuildCollectionXml(games), BuildThingXml());
        var (sut, repository) = CreateServiceWithScope(handler);
        var upsert = CaptureUpsert(repository);

        // Act
        var captured = await RunSyncAndCaptureAsync(sut, upsert);

        // Assert
        captured.Should().HaveCount(21);
        handler.ThingRequestCount.Should().Be(2);
    }

    // ── RunSyncAsync — lifecycle ──────────────────────────────────────────────

    [Fact]
    public async Task RunSyncAsync_SyncCompletes_IsSyncRunningBecomesFalse()
    {
        // Arrange
        var handler = new BoardGameGeekHandler(
            BuildCollectionXml((10, "Gloomhaven")),
            BuildThingXml((10, "Dungeon crawler.", "Isaac Childres", "Adventure")));
        var (sut, repository) = CreateServiceWithScope(handler);
        var upsert = CaptureUpsert(repository);

        // Act
        await RunSyncAndCaptureAsync(sut, upsert);

        // Assert — the finally block in SyncServiceBase clears the flag
        await WaitUntilAsync(() => !sut.IsSyncRunning);
        sut.IsSyncRunning.Should().BeFalse();
    }

    [Fact]
    public async Task RunSyncAsync_RepositoryThrows_ExceptionIsSwallowedAndFlagCleared()
    {
        // Arrange
        var handler = new BoardGameGeekHandler(
            BuildCollectionXml((10, "Gloomhaven")),
            BuildThingXml((10, "Dungeon crawler.", "Isaac Childres", "Adventure")));
        var (sut, repository) = CreateServiceWithScope(handler);

        var attempted = new TaskCompletionSource();
        repository
            .Setup(r => r.UpsertCollectionAsync(
                It.IsAny<IEnumerable<BoardGame>>(), It.IsAny<CancellationToken>()))
            .Callback(() => attempted.TrySetResult())
            .ThrowsAsync(new InvalidOperationException("database unavailable"));

        // Act
        await sut.StartAsync(CancellationToken.None);
        sut.TryStartSync();
        await attempted.Task.WaitAsync(TimeSpan.FromSeconds(30));

        // Assert — the loop logs and carries on rather than faulting
        await WaitUntilAsync(() => !sut.IsSyncRunning);
        sut.IsSyncRunning.Should().BeFalse();

        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task RunSyncAsync_CancelledByShutdown_DoesNotFaultTheService()
    {
        // Arrange — the collection request blocks until the stopping token fires
        var requestStarted = new TaskCompletionSource();
        var handler = new BlockingHandler(requestStarted);
        var (sut, _) = CreateServiceWithScope(handler);

        // Act
        await sut.StartAsync(CancellationToken.None);
        sut.TryStartSync();
        await requestStarted.Task.WaitAsync(TimeSpan.FromSeconds(30));

        // Assert — StopAsync cancels the in-flight sync and completes cleanly
        var stop = async () => await sut.StopAsync(CancellationToken.None);
        await stop.Should().NotThrowAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Routes BoardGameGeek collection and thing requests to fixed XML payloads.</summary>
    private sealed class BoardGameGeekHandler(string collectionXml, string thingXml)
        : HttpMessageHandler
    {
        public int ThingRequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var isThingRequest =
                request.RequestUri!.AbsolutePath.Contains("/thing", StringComparison.Ordinal);

            if (isThingRequest)
            {
                ThingRequestCount++;
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    isThingRequest ? thingXml : collectionXml, Encoding.UTF8, "application/xml")
            });
        }
    }

    /// <summary>Blocks until the request is cancelled, simulating a sync in flight.</summary>
    private sealed class BlockingHandler(TaskCompletionSource requestStarted) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            requestStarted.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private static string BuildCollectionXml(params (int Id, string Name)[] games)
    {
        var items = games.Select(g =>
            $"""
               <item objectid="{g.Id}">
                 <name>{g.Name}</name>
                 <yearpublished>2017</yearpublished>
                 <thumbnail>https://example.test/thumb-{g.Id}.jpg</thumbnail>
                 <image>https://example.test/image-{g.Id}.jpg</image>
                 <stats minplayers="1" maxplayers="4" minplaytime="30" maxplaytime="120" />
               </item>
             """);

        return $"<items>{string.Join(Environment.NewLine, items)}</items>";
    }

    private static string BuildThingXml(
        params (int Id, string Description, string Designer, string Category)[] details)
    {
        var items = details.Select(d =>
            $"""
               <item id="{d.Id}">
                 <description>{d.Description}</description>
                 <link type="boardgamedesigner" value="{d.Designer}" />
                 <link type="boardgamecategory" value="{d.Category}" />
               </item>
             """);

        return $"<items>{string.Join(Environment.NewLine, items)}</items>";
    }

    /// <summary>Signals when UpsertCollectionAsync runs and records what it received.</summary>
    private static TaskCompletionSource<List<BoardGame>> CaptureUpsert(
        Mock<IBoardGamesRepository> repository)
    {
        var completion = new TaskCompletionSource<List<BoardGame>>();

        repository
            .Setup(r => r.UpsertCollectionAsync(
                It.IsAny<IEnumerable<BoardGame>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<BoardGame>, CancellationToken>(
                (games, _) => completion.TrySetResult(games.ToList()))
            .Returns(Task.CompletedTask);

        return completion;
    }

    private static async Task<List<BoardGame>> RunSyncAndCaptureAsync(
        BoardGamesSyncService sut, TaskCompletionSource<List<BoardGame>> upsert)
    {
        await sut.StartAsync(CancellationToken.None);
        sut.TryStartSync();

        var captured = await upsert.Task.WaitAsync(TimeSpan.FromSeconds(30));

        await sut.StopAsync(CancellationToken.None);
        return captured;
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 100 && !condition(); attempt++)
        {
            await Task.Delay(20);
        }
    }

    private static (BoardGamesSyncService Service, Mock<IBoardGamesRepository> Repository)
        CreateServiceWithScope(HttpMessageHandler handler)
    {
        var options = new TestOptionsMonitor<BoardGameGeekOptions>(new BoardGameGeekOptions
        {
            ApiToken = "test-token",
            Username = "myuser"
        });

        var repository = new Mock<IBoardGamesRepository>();

        var services = new ServiceCollection();
        services.AddScoped(_ => new BoardGameGeekClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://boardgamegeek.test") },
            options,
            NullLogger<BoardGameGeekClient>.Instance));
        services.AddScoped(_ => repository.Object);

        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        var service = new BoardGamesSyncService(
            options, scopeFactory, NullLogger<BoardGamesSyncService>.Instance);

        return (service, repository);
    }

    private static BoardGamesSyncService CreateService(string? apiToken, string? username) =>
        CreateService(new TestOptionsMonitor<BoardGameGeekOptions>(new BoardGameGeekOptions
        {
            ApiToken = apiToken ?? string.Empty,
            Username = username ?? string.Empty
        }));

    private static BoardGamesSyncService CreateService(
        TestOptionsMonitor<BoardGameGeekOptions> options)
    {
        // Create a minimal service provider with no actual services registered.
        // TryStartSync does not execute the sync loop, so we don't need a real scope factory.
        var serviceCollection = new ServiceCollection();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        return new BoardGamesSyncService(
            options,
            scopeFactory,
            NullLogger<BoardGamesSyncService>.Instance);
    }
}
