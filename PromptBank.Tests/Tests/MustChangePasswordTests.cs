using PromptBank.Tests.Infrastructure;

namespace PromptBank.Tests.Tests;

/// <summary>
/// E2E tests for the MustChangePassword redirect filter (issue #6).
/// Verifies that users with <c>MustChangePassword = true</c> are intercepted on every page
/// request and directed to <c>/Account/ChangePassword</c>, and that after a successful
/// password change they can browse normally.
/// </summary>
[Collection("Playwright")]
public sealed class MustChangePasswordTests : E2ETestBase
{
    private const string ForcedUsername = "forced-user";
    private const string ForcedPassword = "Forced@1234";
    private const string NewPassword    = "NewForced@99";

    // Scoped to the ChangePassword form button to avoid matching the navbar Logout button,
    // which is also a <button type="submit"> when the user is authenticated.
    private const string SetNewPasswordButton = "button:has-text(\"Set New Password\")";

    /// <summary>
    /// Initialises the test class with the shared Playwright browser fixture.
    /// </summary>
    /// <param name="playwright">The Playwright fixture provided by the xUnit collection.</param>
    public MustChangePasswordTests(PlaywrightFixture playwright) : base(playwright) { }

    // ── AC-1 ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// AC-1: After login, a user with <c>MustChangePassword = true</c> must be redirected
    /// to <c>/Account/ChangePassword</c> instead of landing on the home page.
    /// </summary>
    [Fact]
    public async Task AfterLogin_UserWithMustChangePassword_RedirectsToChangePasswordPage()
    {
        await CreateMustChangePasswordUserAsync(ForcedUsername, ForcedPassword);

        await LoginAsyncRaw(ForcedUsername, ForcedPassword);

        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Account/ChangePassword"));
    }

    // ── AC-2 ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// AC-2: Even when a user with <c>MustChangePassword = true</c> navigates directly
    /// to another page by URL, the filter still intercepts and redirects to
    /// <c>/Account/ChangePassword</c>.
    /// </summary>
    [Fact]
    public async Task DirectNavigation_UserWithMustChangePassword_StillRedirectsToChangePage()
    {
        await CreateMustChangePasswordUserAsync(ForcedUsername, ForcedPassword);

        // Log in (will be redirected to ChangePassword; ignore that for now)
        await LoginAsyncRaw(ForcedUsername, ForcedPassword);

        // Attempt to navigate directly to the home page
        await Page.GotoAsync($"{BaseUrl}/");

        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Account/ChangePassword"));
    }

    // ── AC-2 (Prompts/Create) ─────────────────────────────────────────────────

    /// <summary>
    /// AC-2: Navigating directly to <c>/Prompts/Create</c> while <c>MustChangePassword = true</c>
    /// must also redirect to <c>/Account/ChangePassword</c>, not the create form.
    /// </summary>
    [Fact]
    public async Task DirectNavToCreate_UserWithMustChangePassword_RedirectsToChangePage()
    {
        await CreateMustChangePasswordUserAsync(ForcedUsername, ForcedPassword);
        await LoginAsyncRaw(ForcedUsername, ForcedPassword);

        await Page.GotoAsync($"{BaseUrl}/Prompts/Create");

        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Account/ChangePassword"));
    }

    // ── AC-3 ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// AC-3: After a successful password change on <c>/Account/ChangePassword</c>,
    /// the user must be able to navigate normally (e.g. to the home page) without
    /// being redirected back to the change-password form.
    /// </summary>
    [Fact]
    public async Task AfterSuccessfulPasswordChange_UserCanBrowseNormally()
    {
        await CreateMustChangePasswordUserAsync(ForcedUsername, ForcedPassword);
        await LoginAsyncRaw(ForcedUsername, ForcedPassword);

        // Should now be on the change-password page
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Account/ChangePassword"));

        // Fill in and submit the change-password form
        await Page.FillAsync("#Input_CurrentPassword", ForcedPassword);
        await Page.FillAsync("#Input_NewPassword",     NewPassword);
        await Page.FillAsync("#Input_ConfirmPassword", NewPassword);
        await Page.ClickAsync(SetNewPasswordButton);

        // After success the user should land on the home page
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/$"));
    }

    /// <summary>
    /// AC-3: After changing the password, navigating to other pages must not trigger
    /// another redirect to <c>/Account/ChangePassword</c> — the flag has been cleared.
    /// </summary>
    [Fact]
    public async Task AfterPasswordChange_FlagCleared_SubsequentNavWorks()
    {
        await CreateMustChangePasswordUserAsync(ForcedUsername, ForcedPassword);
        await LoginAsyncRaw(ForcedUsername, ForcedPassword);

        // Change the password
        await Page.FillAsync("#Input_CurrentPassword", ForcedPassword);
        await Page.FillAsync("#Input_NewPassword",     NewPassword);
        await Page.FillAsync("#Input_ConfirmPassword", NewPassword);
        await Page.ClickAsync(SetNewPasswordButton);
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/$"));

        // Navigate away and back — must not hit the change-password redirect again
        await Page.GotoAsync($"{BaseUrl}/Prompts/Create");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Prompts/Create"));
        await Expect(Page).Not.ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Account/ChangePassword"));
    }

    // ── AC-4 ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// AC-4: A user with <c>MustChangePassword = false</c> (alice) must not be redirected
    /// to <c>/Account/ChangePassword</c> — the filter must leave them unaffected.
    /// </summary>
    [Fact]
    public async Task NormalUser_MustChangePasswordFalse_NotRedirected()
    {
        await LoginAsync("alice", "Alice@1234");

        // alice should land on the home page, not the change-password page
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/$"));
        await Expect(Page).Not.ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Account/ChangePassword"));

        // Verify alice can also navigate to other pages without being intercepted
        await Page.GotoAsync($"{BaseUrl}/Prompts/Create");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Prompts/Create"));
    }
}
