// Feature: BGG API Token Authentication (ABM-063)
//
// Scenario: BggClient sends Authorization header when API token is configured
//   Given the BggOptions has a non-empty ApiToken
//   When GetCollectionAsync is called
//   Then the HTTP request includes "Authorization: Bearer {token}"
//
// Scenario: BggClient sends Authorization header when calling GetThingDetailsAsync
//   Given the BggOptions has a non-empty ApiToken
//   When GetThingDetailsAsync is called
//   Then the HTTP request includes "Authorization: Bearer {token}"
//
// Scenario: BggClient does not send Authorization header when token is null
//   Given the BggOptions has a null ApiToken
//   When GetCollectionAsync is called
//   Then the HTTP request does not include an Authorization header
//
// Scenario: BggClient does not send Authorization header when token is empty
//   Given the BggOptions has an empty ApiToken
//   When GetCollectionAsync is called
//   Then the HTTP request does not include an Authorization header
//
// Scenario: BggClient does not send Authorization header when token is whitespace
//   Given the BggOptions has a whitespace-only ApiToken
//   When GetCollectionAsync is called
//   Then the HTTP request does not include an Authorization header

using System.Net;
using System.Text.Json;
using AllByMyshelf.Api.Features.Bgg;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace AllByMyshelf.Unit.Services;

public class BggClientTests
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

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static BggClient CreateClient(HttpMessageHandler handler, string? apiToken, string username = "testuser")
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://boardgamegeek.com")
        };

        var options = new Mock<IOptions<BggOptions>>();
        options.Setup(o => o.Value).Returns(new BggOptions
        {
            ApiToken = apiToken ?? string.Empty,
            Username = username
        });

        return new BggClient(httpClient, options.Object, NullLogger<BggClient>.Instance);
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

// ── Test doubles ──────────────────────────────────────────────────────────────

/// <summary>Captures the last request and returns a fixed response.</summary>
internal sealed class CapturingHandler(HttpResponseMessage response) : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(response);
    }
}
