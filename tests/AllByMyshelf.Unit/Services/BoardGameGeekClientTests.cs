// Feature: BGG API Token Authentication (ABM-063)
//
// Scenario: BoardGameGeekClient sends Authorization header when API token is configured
//   Given the BoardGameGeekOptions has a non-empty ApiToken
//   When GetCollectionAsync is called
//   Then the HTTP request includes "Authorization: Bearer {token}"
//
// Scenario: BoardGameGeekClient sends Authorization header when calling GetThingDetailsAsync
//   Given the BoardGameGeekOptions has a non-empty ApiToken
//   When GetThingDetailsAsync is called
//   Then the HTTP request includes "Authorization: Bearer {token}"
//
// Scenario: BoardGameGeekClient does not send Authorization header when token is null
//   Given the BoardGameGeekOptions has a null ApiToken
//   When GetCollectionAsync is called
//   Then the HTTP request does not include an Authorization header
//
// Scenario: BoardGameGeekClient does not send Authorization header when token is empty
//   Given the BoardGameGeekOptions has an empty ApiToken
//   When GetCollectionAsync is called
//   Then the HTTP request does not include an Authorization header
//
// Scenario: BoardGameGeekClient does not send Authorization header when token is whitespace
//   Given the BoardGameGeekOptions has a whitespace-only ApiToken
//   When GetCollectionAsync is called
//   Then the HTTP request does not include an Authorization header

using System.Net;
using System.Text.Json;
using AllByMyshelf.Api.Features.BoardGameGeek;
using AllByMyshelf.Unit.TestDoubles;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace AllByMyshelf.Unit.Services;

public class BoardGameGeekClientTests
{
    // ── Authorization header — token configured ───────────────────────────────

    [Fact]
    public async Task GetCollectionAsync_TokenConfigured_SendsBearerAuthorizationHeader()
    {
        // Arrange
        var capturingHandler = new CapturingHandler(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = MakeBggCollectionXml() });
        var sut = CreateClient(capturingHandler, apiToken: "my-secret-token");

        // Act
        await sut.GetCollectionAsync("testuser", CancellationToken.None);

        // Assert
        capturingHandler.LastRequest.Should().NotBeNull();
        var authHeader = capturingHandler.LastRequest!.Headers.Authorization;
        authHeader.Should().NotBeNull();
        authHeader!.Scheme.Should().Be("Bearer");
        authHeader.Parameter.Should().Be("my-secret-token");
    }

    [Fact]
    public async Task GetThingDetailsAsync_TokenConfigured_SendsBearerAuthorizationHeader()
    {
        // Arrange
        var capturingHandler = new CapturingHandler(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = MakeBggThingXml(1) });
        var sut = CreateClient(capturingHandler, apiToken: "another-token-123");

        // Act
        await sut.GetThingDetailsAsync(new[] { 1 }, CancellationToken.None);

        // Assert
        capturingHandler.LastRequest.Should().NotBeNull();
        var authHeader = capturingHandler.LastRequest!.Headers.Authorization;
        authHeader.Should().NotBeNull();
        authHeader!.Scheme.Should().Be("Bearer");
        authHeader.Parameter.Should().Be("another-token-123");
    }

    // ── Authorization header — token not configured ───────────────────────────

    [Fact]
    public async Task GetCollectionAsync_TokenEmpty_DoesNotSendAuthorizationHeader()
    {
        // Arrange
        var capturingHandler = new CapturingHandler(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = MakeBggCollectionXml() });
        var sut = CreateClient(capturingHandler, apiToken: string.Empty);

        // Act
        await sut.GetCollectionAsync("testuser", CancellationToken.None);

        // Assert
        capturingHandler.LastRequest.Should().NotBeNull();
        var authHeader = capturingHandler.LastRequest!.Headers.Authorization;
        authHeader.Should().BeNull();
    }

    [Fact]
    public async Task GetCollectionAsync_TokenNull_DoesNotSendAuthorizationHeader()
    {
        // Arrange
        var capturingHandler = new CapturingHandler(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = MakeBggCollectionXml() });
        var sut = CreateClient(capturingHandler, apiToken: null);

        // Act
        await sut.GetCollectionAsync("testuser", CancellationToken.None);

        // Assert
        capturingHandler.LastRequest.Should().NotBeNull();
        var authHeader = capturingHandler.LastRequest!.Headers.Authorization;
        authHeader.Should().BeNull();
    }

    [Fact]
    public async Task GetCollectionAsync_TokenWhitespace_DoesNotSendAuthorizationHeader()
    {
        // Arrange
        var capturingHandler = new CapturingHandler(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = MakeBggCollectionXml() });
        var sut = CreateClient(capturingHandler, apiToken: "   ");

        // Act
        await sut.GetCollectionAsync("testuser", CancellationToken.None);

        // Assert
        capturingHandler.LastRequest.Should().NotBeNull();
        var authHeader = capturingHandler.LastRequest!.Headers.Authorization;
        authHeader.Should().BeNull();
    }

    // ── Collection parsing ──────────────────────────────────────────────────

    [Fact]
    public async Task GetCollectionAsync_ParsesMultipleItemsWithStats()
    {
        // Arrange
        var xml = new StringContent(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <items totalitems="2">
              <item objecttype="thing" objectid="174430" subtype="boardgame">
                <name>Gloomhaven</name>
                <yearpublished>2017</yearpublished>
                <image>https://cf.geekdo-images.com/gloom.jpg</image>
                <thumbnail>https://cf.geekdo-images.com/gloom_t.jpg</thumbnail>
                <stats minplayers="1" maxplayers="4" minplaytime="60" maxplaytime="120" />
              </item>
              <item objecttype="thing" objectid="167791" subtype="boardgame">
                <name>Terraforming Mars</name>
                <yearpublished>2016</yearpublished>
                <image>https://cf.geekdo-images.com/terra.jpg</image>
                <thumbnail>https://cf.geekdo-images.com/terra_t.jpg</thumbnail>
                <stats minplayers="1" maxplayers="5" minplaytime="120" maxplaytime="120" />
              </item>
            </items>
            """,
            System.Text.Encoding.UTF8, "application/xml");

        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = xml });
        var sut = CreateClient(handler, apiToken: "token");

        // Act
        var result = await sut.GetCollectionAsync("testuser", CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].BoardGameGeekId.Should().Be(174430);
        result[0].Name.Should().Be("Gloomhaven");
        result[0].YearPublished.Should().Be(2017);
        result[0].CoverImageUrl.Should().Be("https://cf.geekdo-images.com/gloom.jpg");
        result[0].ThumbnailUrl.Should().Be("https://cf.geekdo-images.com/gloom_t.jpg");
        result[0].MinPlayers.Should().Be(1);
        result[0].MaxPlayers.Should().Be(4);
        result[0].MinPlaytime.Should().Be(60);
        result[0].MaxPlaytime.Should().Be(120);

        result[1].BoardGameGeekId.Should().Be(167791);
        result[1].Name.Should().Be("Terraforming Mars");
    }

    [Fact]
    public async Task GetCollectionAsync_SkipsItemsWithZeroBoardGameGeekId()
    {
        // Arrange
        var xml = new StringContent(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <items totalitems="2">
              <item objecttype="thing" objectid="0" subtype="boardgame">
                <name>Invalid Game</name>
              </item>
              <item objecttype="thing" objectid="12345" subtype="boardgame">
                <name>Valid Game</name>
              </item>
            </items>
            """,
            System.Text.Encoding.UTF8, "application/xml");

        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = xml });
        var sut = CreateClient(handler, apiToken: "token");

        // Act
        var result = await sut.GetCollectionAsync("testuser", CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].BoardGameGeekId.Should().Be(12345);
    }

    [Fact]
    public async Task GetCollectionAsync_HandlesItemsWithMissingOptionalFields()
    {
        // Arrange
        var xml = new StringContent(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <items totalitems="1">
              <item objecttype="thing" objectid="99999" subtype="boardgame">
                <name>Minimal Game</name>
              </item>
            </items>
            """,
            System.Text.Encoding.UTF8, "application/xml");

        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = xml });
        var sut = CreateClient(handler, apiToken: "token");

        // Act
        var result = await sut.GetCollectionAsync("testuser", CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].BoardGameGeekId.Should().Be(99999);
        result[0].YearPublished.Should().BeNull();
        result[0].CoverImageUrl.Should().BeNull();
        result[0].ThumbnailUrl.Should().BeNull();
        result[0].MinPlayers.Should().BeNull();
        result[0].MaxPlayers.Should().BeNull();
        result[0].MinPlaytime.Should().BeNull();
        result[0].MaxPlaytime.Should().BeNull();
    }

    // ── Thing details parsing ────────────────────────────────────────────────

    [Fact]
    public async Task GetThingDetailsAsync_ParsesDesignersAndCategory()
    {
        // Arrange
        var xml = new StringContent(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <items>
              <item type="boardgame" id="174430">
                <name type="primary" sortindex="1" value="Gloomhaven" />
                <description>A tactical combat game</description>
                <link type="boardgamedesigner" id="69802" value="Isaac Childres" />
                <link type="boardgamecategory" id="1022" value="Adventure" />
                <link type="boardgamemechanic" id="2023" value="Cooperative" />
              </item>
            </items>
            """,
            System.Text.Encoding.UTF8, "application/xml");

        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = xml });
        var sut = CreateClient(handler, apiToken: "token");

        // Act
        var result = await sut.GetThingDetailsAsync([174430], CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(174430);
        result[0].Description.Should().Be("A tactical combat game");
        result[0].Designers.Should().ContainSingle().Which.Should().Be("Isaac Childres");
        result[0].Category.Should().Be("Adventure");
    }

    [Fact]
    public async Task GetThingDetailsAsync_ParsesMultipleDesigners()
    {
        // Arrange
        var xml = new StringContent(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <items>
              <item type="boardgame" id="12333">
                <name type="primary" sortindex="1" value="Twilight Struggle" />
                <description>Cold War card game</description>
                <link type="boardgamedesigner" id="3876" value="Ananda Gupta" />
                <link type="boardgamedesigner" id="3877" value="Jason Matthews" />
                <link type="boardgamecategory" id="1001" value="Political" />
              </item>
            </items>
            """,
            System.Text.Encoding.UTF8, "application/xml");

        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = xml });
        var sut = CreateClient(handler, apiToken: "token");

        // Act
        var result = await sut.GetThingDetailsAsync([12333], CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Designers.Should().HaveCount(2);
        result[0].Designers.Should().Contain("Ananda Gupta");
        result[0].Designers.Should().Contain("Jason Matthews");
    }

    [Fact]
    public async Task GetThingDetailsAsync_SkipsItemsWithZeroId()
    {
        // Arrange
        var xml = new StringContent(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <items>
              <item type="boardgame" id="0">
                <description>Invalid</description>
              </item>
              <item type="boardgame" id="555">
                <description>Valid</description>
              </item>
            </items>
            """,
            System.Text.Encoding.UTF8, "application/xml");

        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = xml });
        var sut = CreateClient(handler, apiToken: "token");

        // Act
        var result = await sut.GetThingDetailsAsync([0, 555], CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(555);
    }

    [Fact]
    public async Task GetThingDetailsAsync_HandlesNoCategoryOrDesigners()
    {
        // Arrange
        var xml = new StringContent(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <items>
              <item type="boardgame" id="999">
                <description>Bare-bones game</description>
              </item>
            </items>
            """,
            System.Text.Encoding.UTF8, "application/xml");

        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = xml });
        var sut = CreateClient(handler, apiToken: "token");

        // Act
        var result = await sut.GetThingDetailsAsync([999], CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Designers.Should().BeEmpty();
        result[0].Category.Should().BeNull();
    }

    // ── Retry logic ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCollectionAsync_RetriesOn202ThenSucceeds()
    {
        // Arrange
        var responses = new Queue<HttpResponseMessage>();
        responses.Enqueue(new HttpResponseMessage(HttpStatusCode.Accepted));
        responses.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                <?xml version="1.0" encoding="utf-8"?>
                <items totalitems="1">
                  <item objecttype="thing" objectid="1" subtype="boardgame">
                    <name>Retry Game</name>
                  </item>
                </items>
                """,
                System.Text.Encoding.UTF8, "application/xml")
        });

        var handler = new QueuedResponseHandler(responses);
        var sut = CreateClient(handler, apiToken: "token");

        // Act
        var result = await sut.GetCollectionAsync("testuser", CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Retry Game");
        handler.CallCount.Should().Be(2);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static BoardGameGeekClient CreateClient(HttpMessageHandler handler, string? apiToken, string username = "testuser")
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://boardgamegeek.com")
        };

        var options = new Mock<IOptions<BoardGameGeekOptions>>();
        options.Setup(o => o.Value).Returns(new BoardGameGeekOptions
        {
            ApiToken = apiToken ?? string.Empty,
            Username = username
        });

        return new BoardGameGeekClient(httpClient, options.Object, NullLogger<BoardGameGeekClient>.Instance);
    }

    private static StringContent MakeBggCollectionXml() =>
        new StringContent(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <items totalitems="0" termsofuse="https://boardgamegeek.com/xmlapi/termsofuse" pubdate="Mon, 16 Mar 2026 12:00:00 +0000">
            </items>
            """,
            System.Text.Encoding.UTF8,
            "application/xml");

    private static StringContent MakeBggThingXml(int id) =>
        new StringContent(
            $"""
            <?xml version="1.0" encoding="utf-8"?>
            <items termsofuse="https://boardgamegeek.com/xmlapi/termsofuse">
              <item type="boardgame" id="{id}">
                <name type="primary" sortindex="1" value="Test Game" />
                <description>Test description</description>
              </item>
            </items>
            """,
            System.Text.Encoding.UTF8,
            "application/xml");
}

