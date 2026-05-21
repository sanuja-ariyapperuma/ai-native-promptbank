using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Moq;
using PromptBank.Models;
using PromptBank.Pages.Account;

namespace PromptBank.UnitTests.Pages;

/// <summary>
/// Unit tests for <see cref="ChangePasswordModel"/>.
/// <see cref="UserManager{TUser}"/> and <see cref="SignInManager{TUser}"/> are mocked
/// with Moq so tests run without any database or HTTP pipeline dependencies.
/// </summary>
public class ChangePasswordModelTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a mock <see cref="UserManager{TUser}"/> with the minimal constructor arguments
    /// required to create the Moq proxy.
    /// </summary>
    private static Mock<UserManager<ApplicationUser>> BuildMockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object, null, null, null, null, null, null, null, null);
    }

    /// <summary>
    /// Builds a mock <see cref="SignInManager{TUser}"/> bound to the supplied
    /// <paramref name="userManager"/> mock.
    /// </summary>
    private static Mock<SignInManager<ApplicationUser>> BuildMockSignInManager(
        Mock<UserManager<ApplicationUser>> userManager)
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        return new Mock<SignInManager<ApplicationUser>>(
            userManager.Object,
            contextAccessor.Object,
            claimsPrincipalFactory.Object,
            null, null, null, null);
    }

    /// <summary>
    /// Constructs a <see cref="ChangePasswordModel"/> wired to the supplied mocks
    /// and initialised with an authenticated <see cref="PageContext"/>.
    /// </summary>
    private static ChangePasswordModel BuildPageModel(
        Mock<UserManager<ApplicationUser>> userManager,
        Mock<SignInManager<ApplicationUser>> signInManager)
    {
        var model = new ChangePasswordModel(userManager.Object, signInManager.Object);
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, "user-1") },
                "Test"));
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new PageActionDescriptor(),
            new ModelStateDictionary());
        model.PageContext = new PageContext(actionContext);
        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(u => u.IsLocalUrl(It.IsAny<string>())).Returns(false);
        model.Url = urlHelper.Object;
        return model;
    }

    // ── AC-1 / AC-4 — OnGetAsync ─────────────────────────────────────────────

    /// <summary>
    /// AC-4: When the user's <c>MustChangePassword</c> flag is already <c>false</c>,
    /// <see cref="ChangePasswordModel.OnGetAsync"/> must redirect to <c>/Index</c>
    /// so the user is not shown the form unnecessarily.
    /// </summary>
    [Fact]
    public async Task OnGetAsync_MustChangePasswordFalse_RedirectsToIndex()
    {
        var um = BuildMockUserManager();
        var sm = BuildMockSignInManager(um);
        var user = new ApplicationUser { Id = "user-1", MustChangePassword = false };
        um.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
          .ReturnsAsync(user);

        var model = BuildPageModel(um, sm);
        var result = await model.OnGetAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    /// <summary>
    /// AC-1: When the user's <c>MustChangePassword</c> flag is <c>true</c>,
    /// <see cref="ChangePasswordModel.OnGetAsync"/> must return <see cref="PageResult"/>
    /// so the change-password form is rendered.
    /// </summary>
    [Fact]
    public async Task OnGetAsync_MustChangePasswordTrue_ReturnsPage()
    {
        var um = BuildMockUserManager();
        var sm = BuildMockSignInManager(um);
        var user = new ApplicationUser { Id = "user-1", MustChangePassword = true };
        um.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
          .ReturnsAsync(user);

        var model = BuildPageModel(um, sm);
        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
    }

    // ── AC-3 — OnPostAsync success ────────────────────────────────────────────

    /// <summary>
    /// AC-3: On a successful password change, <see cref="ChangePasswordModel.OnPostAsync"/>
    /// must set <see cref="ApplicationUser.MustChangePassword"/> to <c>false</c> and call
    /// <c>UpdateAsync</c> before redirecting so the flag is persisted to the database.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_SuccessfulChange_ClearsMustChangePasswordFlag()
    {
        var um = BuildMockUserManager();
        var sm = BuildMockSignInManager(um);
        var user = new ApplicationUser { Id = "user-1", MustChangePassword = true };

        um.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
          .ReturnsAsync(user);
        um.Setup(m => m.ChangePasswordAsync(user, "OldPass@1", "NewPass@1"))
          .ReturnsAsync(IdentityResult.Success);
        um.Setup(m => m.UpdateAsync(user))
          .ReturnsAsync(IdentityResult.Success);
        sm.Setup(m => m.SignOutAsync())
          .Returns(Task.CompletedTask);
        sm.Setup(m => m.SignInAsync(user, false, null))
          .Returns(Task.CompletedTask);

        var model = BuildPageModel(um, sm);
        model.Input = new ChangePasswordInputModel
        {
            CurrentPassword = "OldPass@1",
            NewPassword = "NewPass@1",
            ConfirmPassword = "NewPass@1"
        };

        await model.OnPostAsync();

        Assert.False(user.MustChangePassword, "MustChangePassword should be cleared on success.");
        um.Verify(m => m.UpdateAsync(user), Times.Once);
    }

    /// <summary>
    /// AC-3: After a successful password change, the page model must redirect —
    /// not return <see cref="PageResult"/> — so the user proceeds normally.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_SuccessfulChange_RedirectsAwayFromPage()
    {
        var um = BuildMockUserManager();
        var sm = BuildMockSignInManager(um);
        var user = new ApplicationUser { Id = "user-1", MustChangePassword = true };

        um.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
          .ReturnsAsync(user);
        um.Setup(m => m.ChangePasswordAsync(user, "OldPass@1", "NewPass@1"))
          .ReturnsAsync(IdentityResult.Success);
        um.Setup(m => m.UpdateAsync(user))
          .ReturnsAsync(IdentityResult.Success);
        sm.Setup(m => m.SignOutAsync())
          .Returns(Task.CompletedTask);
        sm.Setup(m => m.SignInAsync(user, false, null))
          .Returns(Task.CompletedTask);

        var model = BuildPageModel(um, sm);
        model.Input = new ChangePasswordInputModel
        {
            CurrentPassword = "OldPass@1",
            NewPassword = "NewPass@1",
            ConfirmPassword = "NewPass@1"
        };

        var result = await model.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
    }

    // ── OnPostAsync failure ───────────────────────────────────────────────────

    /// <summary>
    /// When <c>ChangePasswordAsync</c> fails (e.g., wrong current password),
    /// <see cref="ChangePasswordModel.OnPostAsync"/> must return <see cref="PageResult"/>
    /// so the form is re-rendered with the Identity error messages added to
    /// <see cref="ModelStateDictionary"/>.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_ChangeFails_ReturnsPageWithModelErrors()
    {
        var um = BuildMockUserManager();
        var sm = BuildMockSignInManager(um);
        var user = new ApplicationUser { Id = "user-1", MustChangePassword = true };
        var identityError = new IdentityError { Code = "PasswordMismatch", Description = "Incorrect password." };

        um.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
          .ReturnsAsync(user);
        um.Setup(m => m.ChangePasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()))
          .ReturnsAsync(IdentityResult.Failed(identityError));

        var model = BuildPageModel(um, sm);
        model.Input = new ChangePasswordInputModel
        {
            CurrentPassword = "WrongPass",
            NewPassword = "NewPass@1",
            ConfirmPassword = "NewPass@1"
        };

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        um.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }
}

