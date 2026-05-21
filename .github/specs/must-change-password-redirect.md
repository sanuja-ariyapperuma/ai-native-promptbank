# Spec: MustChangePassword Redirect Filter

## Context

Admin accounts and password-reset users are created with `MustChangePassword = true`. They must be forced to change their password before accessing any other page. The `ApplicationUser.MustChangePassword` property was added in issue #4.

## Acceptance Criteria

- **AC-1:** An authenticated user with `MustChangePassword = true` who requests any page is redirected to `/Account/ChangePassword`.
- **AC-2:** Navigating directly to any page other than `/Account/ChangePassword` or `/Account/Logout` while `MustChangePassword = true` still redirects to the change-password page (no bypass possible via URL manipulation).
- **AC-3:** After a successful password change on `/Account/ChangePassword`, `MustChangePassword` is set to `false` and the user is redirected to the original destination (or `/` if no return URL was captured).
- **AC-4:** Users with `MustChangePassword = false` are completely unaffected — they browse normally.
- **AC-5:** `dotnet build` passes with no errors.

## Implementation Plan

### Filter
- Add `PromptBank/Filters/MustChangePasswordFilter.cs` implementing `IAsyncPageFilter`.
- On every authenticated page request, load the `ApplicationUser` via `UserManager` and check `MustChangePassword`.
- Exempt paths: `/Account/ChangePassword`, `/Account/Logout` (checked via `HttpContext.Request.Path`).
- Static assets never reach Razor Pages filters and need no explicit exemption.
- If redirect is needed, pass the original path as `?returnUrl=…` query parameter.

### New Page
- `Pages/Account/ChangePassword.cshtml` — Bootstrap form with three fields: **Current password**, **New password**, **Confirm new password**.
- `OnGetAsync`: verify user exists and has `MustChangePassword = true`; otherwise redirect to `/`.
- `OnPostAsync`: call `UserManager.ChangePasswordAsync`; on success set `MustChangePassword = false`, call `SignInManager.RefreshSignInAsync`, then redirect to `returnUrl` (local URL validated) or `/`.

### Registration
- Register filter globally in `Program.cs` via `AddRazorPages(options => …)`.
- Register `MustChangePasswordFilter` as a scoped service.

### Tests
- **Unit tests** (`PromptBank.UnitTests/Pages/ChangePasswordModelTests.cs`): page model `OnPostAsync` sets `MustChangePassword = false` on success; returns `Page()` with model errors on failure; `OnGetAsync` redirects away when flag is already `false`.
- **E2E tests** (`PromptBank.Tests/Tests/MustChangePasswordTests.cs`): AC-1 redirect, AC-2 direct URL bypass blocked, AC-3 password change clears flag and navigates normally, AC-4 unaffected users.
