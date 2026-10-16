using Launchify.Application.DTOs;
using Launchify.Application.Features.LaunchifyPages.Commands.CreateLandingPage;
using Launchify.Application.Interfaces;
using Launchify.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Launchify.Tests.Application;

public class CreateLandingPageSafetyTests
{
    private readonly Mock<ILandingPageRepository> pages = new();
    private readonly Mock<IAiGeneratorService> ai = new();
    private readonly Mock<IUserRepository> users = new();

    private CreateLandingPageCommandHandler CreateHandler() =>
        new(pages.Object, ai.Object, users.Object,
            NullLogger<CreateLandingPageCommandHandler>.Instance);

    private static CreateLandingPageCommand Request() => new()
    {
        UserId = "owner",
        ProductName = "Launch Test",
        ProductDescription = "A product for testing.",
        ContactEmail = "owner@example.com",
        DemoLink = "https://example.com",
        ThemeType = "aurora",
        TemplateType = "aurora",
        ConnectionId = ""
    };

    [Fact]
    public async Task NoCredit_DoesNotMintCreditOrGenerateContent()
    {
        users.Setup(x => x.DeductCreditAsync("owner")).ReturnsAsync(false);

        var result = await CreateHandler().Handle(Request(), default);

        Assert.False(result.IsSuccess);
        users.Verify(x => x.DeductCreditAsync("owner"), Times.Once);
        users.Verify(x => x.RefundCreditAsync(It.IsAny<string>()), Times.Never);
        pages.Verify(x => x.GetByUserIdAsync(It.IsAny<string>()), Times.Never);
        ai.Verify(x => x.GenerateContentAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<Func<string, Task>>()), Times.Never);
        pages.Verify(x => x.AddAsync(It.IsAny<LandingPage>()), Times.Never);
    }

    [Fact]
    public async Task GenerationFailure_RefundsCreditAndHidesInternalError()
    {
        users.Setup(x => x.DeductCreditAsync("owner")).ReturnsAsync(true);
        users.Setup(x => x.RefundCreditAsync("owner")).ReturnsAsync(true);
        ai.Setup(x => x.GenerateContentAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<Func<string, Task>>()))
            .ThrowsAsync(new InvalidOperationException("private provider details"));

        var result = await CreateHandler().Handle(Request(), default);

        Assert.False(result.IsSuccess);
        Assert.DoesNotContain("private provider details", result.Message);
        users.Verify(x => x.RefundCreditAsync("owner"), Times.Once);
        pages.Verify(x => x.AddAsync(It.IsAny<LandingPage>()), Times.Never);
    }

    [Fact]
    public async Task SuccessfulCreation_StoresPageWithoutRefundingCredit()
    {
        users.Setup(x => x.DeductCreditAsync("owner")).ReturnsAsync(true);
        ai.Setup(x => x.GenerateContentAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<Func<string, Task>>()))
            .ReturnsAsync(new AiPageConfig());

        var result = await CreateHandler().Handle(Request(), default);

        Assert.True(result.IsSuccess);
        pages.Verify(x => x.AddAsync(It.Is<LandingPage>(p =>
            p.UserId == "owner" && p.Slug == "launch-test")), Times.Once);
        users.Verify(x => x.RefundCreditAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("https://example.com/demo", true)]
    [InlineData("http://example.com", true)]
    [InlineData("", true)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("file:///etc/passwd", false)]
    [InlineData("ftp://example.com", false)]
    [InlineData("/relative-path", false)]
    public async Task DemoLink_OnlyAllowsOptionalHttpLinks(string url, bool valid)
    {
        var request = Request();
        request.DemoLink = url;

        var result = await new CreateLandingPageCommandValidator().ValidateAsync(request);

        Assert.Equal(valid, result.IsValid);
    }
}
