using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Launchify.Application.DTOs;
using Launchify.Application.Features.LaunchifyPages.Commands.CreateLandingPage;
using Launchify.Application.Features.Users.Commands.SyncUser;
using Launchify.Application.Interfaces;
using Launchify.Domain.Entities;
using Launchify.WebAPI.Authentication;
using Launchify.WebAPI.Controllers;
using Launchify.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Threading.RateLimiting;

namespace Launchify.Tests.Security;

public class ApiAuthorizationTests : IAsyncLifetime
{
    private WebApplication app = null!;
    private HttpClient client = null!;
    private readonly Mock<ILandingPageRepository> pages = new();
    private readonly Mock<IUserRepository> users = new();
    private readonly Mock<IWaitlistRepository> waitlist = new();
    private readonly Mock<IMediator> mediator = new();
    private readonly Mock<IAiGeneratorService> ai = new();
    private const string PageId = "11111111-1111-1111-1111-111111111111";

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddLogging();
        builder.Services.AddSingleton(pages.Object);
        builder.Services.AddSingleton(users.Object);
        builder.Services.AddSingleton(waitlist.Object);
        builder.Services.AddSingleton(mediator.Object);
        builder.Services.AddSingleton(ai.Object);
        builder.Services.AddSingleton<AnalyticsQueueService>();
        builder.Services.AddSingleton<IAuthService, TestAuthService>();
        builder.Services.AddAuthentication(FirebaseAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, FirebaseAuthenticationHandler>(
                FirebaseAuthenticationHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddMemoryCache();
        builder.Services.AddSignalR();
        builder.Services.AddRateLimiter(options => options.AddPolicy("AiCreationLimit",
            _ => RateLimitPartition.GetNoLimiter("tests")));
        builder.Services.AddControllers().AddApplicationPart(typeof(LandingPagesController).Assembly);

        app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        app.MapControllers();
        app.MapHub<LaunchifyBackend.Hubs.GenerationHub>("/generationHub");
        await app.StartAsync();
        var url = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        client = new HttpClient { BaseAddress = new Uri(url) };
        pages.Setup(x => x.GetByIdAsync(PageId)).ReturnsAsync(new LandingPage
        {
            Id = Guid.Parse(PageId), UserId = "owner", Slug = "test-page", AiConfig = new()
        });
    }

    public async Task DisposeAsync()
    {
        client.Dispose();
        await app.DisposeAsync();
    }

    [Theory]
    [InlineData("POST", "/api/LandingPages/create")]
    [InlineData("PUT", "/api/LandingPages/11111111-1111-1111-1111-111111111111")]
    [InlineData("DELETE", "/api/LandingPages/11111111-1111-1111-1111-111111111111")]
    [InlineData("GET", "/api/LandingPages/user/owner")]
    [InlineData("POST", "/api/LandingPages/ai-assist")]
    [InlineData("POST", "/api/User/sync")]
    [InlineData("GET", "/api/User/owner")]
    [InlineData("GET", "/api/Waitlist/11111111-1111-1111-1111-111111111111")]
    [InlineData("POST", "/generationHub/negotiate?negotiateVersion=1")]
    public async Task PrivateEndpoints_RejectAnonymousRequests(string method, string path)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path)
        { Content = JsonContent.Create(new { }) };
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        pages.Verify(x => x.UpdateAsync(It.IsAny<LandingPage>()), Times.Never);
    }

    [Fact]
    public async Task InvalidToken_IsRejected()
    {
        client.DefaultRequestHeaders.Authorization = new("Bearer", "invalid");
        var response = await client.GetAsync("/api/User/owner");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        users.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("/api/User/owner")]
    [InlineData("/api/LandingPages/user/owner")]
    [InlineData("/api/Waitlist/11111111-1111-1111-1111-111111111111")]
    public async Task OtherUser_CannotReadPrivateData(string path)
    {
        client.DefaultRequestHeaders.Authorization = new("Bearer", "other");
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        waitlist.Verify(x => x.GetByPageIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OtherUser_CannotUpdateOrDeleteProjectEvenWithForgedUid()
    {
        client.DefaultRequestHeaders.Authorization = new("Bearer", "other");
        var update = await client.PutAsJsonAsync("/api/LandingPages/" + PageId,
            new { productName = "stolen", templateType = "aurora", heroTitle = "title",
                marketingCopy = "copy", callToActionText = "join", accentColor = "#6366F1" });
        var delete = await client.DeleteAsync("/api/LandingPages/" + PageId + "?userId=owner");
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        pages.Verify(x => x.UpdateAsync(It.IsAny<LandingPage>()), Times.Never);
        mediator.Verify(x => x.Send(It.IsAny<IRequest<object>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_UsesVerifiedUidInsteadOfBody()
    {
        client.DefaultRequestHeaders.Authorization = new("Bearer", "owner");
        mediator.Setup(x => x.Send(It.IsAny<CreateLandingPageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateLandingResponse { IsSuccess = true, GeneratedPageId = PageId, Slug = "test", Message = "ok" });
        var response = await client.PostAsJsonAsync("/api/LandingPages/create", new {
            userId = "forged", productName = "Test", themeType = "aurora",
            templateType = "aurora", contactEmail = "a@example.com", demoLink = "https://example.com",
            productDescription = "Example", connectionId = ""
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        mediator.Verify(x => x.Send(It.Is<CreateLandingPageCommand>(c => c.UserId == "owner"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Sync_UsesVerifiedUidAndEmail()
    {
        client.DefaultRequestHeaders.Authorization = new("Bearer", "owner");
        mediator.Setup(x => x.Send(It.IsAny<SyncUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncUserResponse { IsSuccess = true, Message = "ok", RemainingCredits = 3 });
        var response = await client.PostAsJsonAsync("/api/User/sync",
            new { uid = "forged", email = "forged@example.com" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        mediator.Verify(x => x.Send(It.Is<SyncUserCommand>(c =>
            c.Uid == "owner" && c.Email == "owner@example.com"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Owner_CanReadWaitlistAndUpdateOwnProject()
    {
        client.DefaultRequestHeaders.Authorization = new("Bearer", "owner");
        waitlist.Setup(x => x.GetByPageIdAsync(PageId)).ReturnsAsync(new List<WaitlistEntry>());
        var list = await client.GetAsync("/api/Waitlist/" + PageId);
        var update = await client.PutAsJsonAsync("/api/LandingPages/" + PageId,
            new { productName = "Updated", templateType = "aurora", heroTitle = "title",
                marketingCopy = "copy", callToActionText = "join", accentColor = "#6366F1" });
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        pages.Verify(x => x.UpdateAsync(It.Is<LandingPage>(p => p.ProductName == "Updated")), Times.Once);
    }

    [Fact]
    public async Task PublicPageAndWaitlistSignup_RemainAnonymous()
    {
        pages.Setup(x => x.GetBySlugAsync("public")).ReturnsAsync(new LandingPage
        { Id = Guid.Parse(PageId), Slug = "public", AiConfig = new() });
        var page = await client.GetAsync("/api/LandingPages/public");
        var signup = await client.PostAsJsonAsync("/api/Waitlist", new { pageId = PageId, email = "visitor@example.com" });
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Equal(HttpStatusCode.OK, signup.StatusCode);
        waitlist.Verify(x => x.AddAsync(It.IsAny<WaitlistEntry>()), Times.Once);
    }

    private sealed class TestAuthService : IAuthService
    {
        public Task<ClaimsPrincipal> VerifyFirebaseTokenAsync(string token)
        {
            if (token == "invalid") throw new UnauthorizedAccessException();
            return Task.FromResult(new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, token),
                new Claim(ClaimTypes.Email, token + "@example.com")
            }, "Firebase")));
        }
    }
}
