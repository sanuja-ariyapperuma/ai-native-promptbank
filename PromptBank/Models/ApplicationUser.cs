using Microsoft.AspNetCore.Identity;

namespace PromptBank.Models;

/// <summary>
/// Application user that extends ASP.NET Core Identity's <see cref="IdentityUser"/>.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Gets or sets a value indicating whether this account has been disabled by an administrator.
    /// Disabled accounts cannot log in and their prompts are hidden from public listings.
    /// </summary>
    public bool IsDisabled { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the user must change their password on next login.
    /// </summary>
    public bool MustChangePassword { get; set; } = false;
}
