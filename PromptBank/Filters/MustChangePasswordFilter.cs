using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PromptBank.Models;

namespace PromptBank.Filters;

/// <summary>
/// A global Razor Pages filter that intercepts every authenticated page request and
/// redirects to <c>/Account/ChangePassword</c> when the current user's
/// <see cref="ApplicationUser.MustChangePassword"/> flag is <c>true</c>.
/// </summary>
/// <remarks>
/// Exempt paths — checked against <c>HttpContext.Request.Path</c>:
/// <list type="bullet">
///   <item><c>/Account/ChangePassword</c></item>
///   <item><c>/Account/Logout</c></item>
/// </list>
/// Static assets never reach Razor Pages filters and require no explicit exemption.
/// </remarks>
public sealed class MustChangePasswordFilter : IAsyncPageFilter
{
    private readonly UserManager<ApplicationUser> _userManager;

    /// <summary>
    /// Initialises a new instance of <see cref="MustChangePasswordFilter"/>.
    /// </summary>
    /// <param name="userManager">The Identity user manager used to load the current user.</param>
    public MustChangePasswordFilter(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    /// <summary>
    /// Checks whether the authenticated user must change their password before executing
    /// the page handler.  Redirects to <c>/Account/ChangePassword</c> when required.
    /// </summary>
    /// <param name="context">The executing context for the current page handler.</param>
    /// <param name="next">The delegate to invoke to continue the pipeline.</param>
    /// <returns>A task that completes when the filter has run.</returns>
    public async Task OnPageHandlerExecutionAsync(
        PageHandlerExecutingContext context,
        PageHandlerExecutionDelegate next)
    {
        var httpContext = context.HttpContext;

        if (httpContext.User.Identity?.IsAuthenticated == true && !IsExemptPath(httpContext.Request.Path))
        {
            var user = await _userManager.GetUserAsync(httpContext.User);
            if (user?.MustChangePassword == true)
            {
                var returnUrl = httpContext.Request.Path + httpContext.Request.QueryString;
                context.Result = new RedirectToPageResult(
                    "/Account/ChangePassword",
                    new { returnUrl });
                return;
            }
        }

        await next();
    }

    /// <summary>
    /// No-op — handler selection requires no additional processing by this filter.
    /// </summary>
    /// <param name="context">The handler selection context.</param>
    /// <returns>A completed task.</returns>
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    /// <summary>
    /// Returns <c>true</c> for paths that must not trigger the must-change-password redirect.
    /// </summary>
    /// <param name="path">The current request path.</param>
    /// <returns><c>true</c> if the path is exempt; otherwise <c>false</c>.</returns>
    private static bool IsExemptPath(PathString path) =>
        path.StartsWithSegments("/Account/ChangePassword", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/Account/Logout", StringComparison.OrdinalIgnoreCase);
}
