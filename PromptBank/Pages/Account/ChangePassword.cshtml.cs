using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using PromptBank.Models;

namespace PromptBank.Pages.Account;

/// <summary>
/// Page model for the mandatory change-password page.
/// Shown to any authenticated user whose <see cref="ApplicationUser.MustChangePassword"/>
/// flag is <c>true</c>.  On success the flag is cleared and the user is redirected to their
/// original destination (or the home page when no return URL is available).
/// </summary>
public class ChangePasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    /// <summary>
    /// Initialises a new instance of <see cref="ChangePasswordModel"/>.
    /// </summary>
    /// <param name="userManager">The Identity user manager.</param>
    /// <param name="signInManager">The Identity sign-in manager used to refresh the session cookie after a password change.</param>
    public ChangePasswordModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    /// <summary>Gets or sets the form input bound from the POST body.</summary>
    [BindProperty]
    public ChangePasswordInputModel Input { get; set; } = new();

    /// <summary>Gets or sets the URL the user should be redirected to after a successful change.</summary>
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Displays the change-password form.  Redirects to the home page if the current user
    /// does not have the <see cref="ApplicationUser.MustChangePassword"/> flag set.
    /// </summary>
    /// <param name="returnUrl">Optional return URL captured by the redirect filter.</param>
    /// <returns>The page, or a redirect when the flag is not set.</returns>
    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToPage("/Account/Login");

        if (!user.MustChangePassword)
            return RedirectToPage("/Index");

        ReturnUrl = returnUrl;
        return Page();
    }

    /// <summary>
    /// Handles the change-password form submission.  On success, sets
    /// <see cref="ApplicationUser.MustChangePassword"/> to <c>false</c>, refreshes the
    /// authentication cookie, and redirects to <paramref name="returnUrl"/> or the home page.
    /// </summary>
    /// <param name="returnUrl">Optional return URL to redirect to after a successful change.</param>
    /// <returns>The page with validation errors on failure; a redirect on success.</returns>
    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
            return Page();

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToPage("/Account/Login");

        var result = await _userManager.ChangePasswordAsync(
            user, Input.CurrentPassword, Input.NewPassword);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);

        var freshUser = await _userManager.FindByIdAsync(user.Id);
        if (freshUser is not null)
            await _signInManager.SignInAsync(freshUser, isPersistent: false);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToPage("/Index");
    }
}

/// <summary>
/// Input model for the change-password form.
/// </summary>
public class ChangePasswordInputModel
{
    /// <summary>Gets or sets the user's current (existing) password.</summary>
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>Gets or sets the desired new password.</summary>
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>Gets or sets the confirmation of the new password; must match <see cref="NewPassword"/>.</summary>
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    [Compare(nameof(NewPassword), ErrorMessage = "The new password and confirmation do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
