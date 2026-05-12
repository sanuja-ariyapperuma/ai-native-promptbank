# Spec: Add IsDisabled and MustChangePassword to ApplicationUser

## Context
The admin panel requires two new flags on `ApplicationUser` to support user-disable and forced-password-change flows.

## Acceptance Criteria

- **AC-1:** `ApplicationUser` has `IsDisabled` (bool, default `false`)
- **AC-2:** `ApplicationUser` has `MustChangePassword` (bool, default `false`)
- **AC-3:** EF Core migration generated and applied successfully
- **AC-4:** `dotnet build` passes with no errors

## Out of scope
- UI surfaces for these flags (admin panel, login enforcement) — downstream issues
- Any middleware or login-flow logic that reads these flags
