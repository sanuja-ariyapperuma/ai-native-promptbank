# Admin Panel — Design Specification

## Overview

The admin panel is a distinct, role-gated section of PromptBank accessible at the `/admin` URL prefix. It is separated from the main application by both URL structure and layout. Regular users have no access to it.

---

## Roles

The application has three distinct, non-interchangeable roles. Users cannot be promoted or demoted between roles.

| Role | How created | Scope |
|---|---|---|
| `User` | Self-registered via `/Account/Register` | Browse, create, rate, pin prompts |
| `Admin` | Created by Super Admin via admin panel | Manage regular users, moderate prompts |
| `SuperAdmin` | Seeded at startup (singleton) | Manage admin accounts |

---

## Seeded Accounts

Seeded in `Program.cs` at startup alongside the existing alice/bob/carol users.

| Username | Password | Role |
|---|---|---|
| `admin` | `Admin@1234` | `Admin` |
| `superadmin` | `SuperAdmin@1234` | `SuperAdmin` |

---

## Model Changes

Two new fields added to `ApplicationUser`:

```csharp
public bool IsDisabled { get; set; }          // explicit admin-disable flag
public bool MustChangePassword { get; set; }  // forces password change on next login
```

`IsDisabled` is used instead of relying solely on Identity's `LockoutEnd` to distinguish admin-disabled accounts from automatic lockouts caused by failed login attempts.

---

## Disable Behaviour

When an admin disables a user:

1. `IsDisabled = true` is set on the account
2. Login is blocked
3. The user's prompts are hidden from the public listing

### Self-disable protection

- Admins cannot disable themselves
- Super Admin cannot disable themselves
- Enforced server-side (returns 403)

---

## Password Reset

When an admin or Super Admin resets a user's password:

1. Admin sets a temporary password
2. `MustChangePassword = true` is set on the account
3. On next login, the user is redirected to a mandatory change-password page before accessing anything else

This behaviour is consistent with new admin account creation.

---

## Admin Account Creation

Super Admin can create new Admin accounts from the admin panel:

- Fields: username, email, temporary password
- `MustChangePassword = true` is set automatically
- The new account is assigned the `Admin` role
- Regular user registration (`/Account/Register`) remains unaffected and open

---

## URL Structure and Layout

- All admin pages live under `Pages/Admin/`
- URL prefix: `/admin`
- Uses a dedicated `_AdminLayout.cshtml` separate from the main app layout
- The admin layout has its own navigation tailored to admin tasks

---

## Features and Access

### Dashboard — `/admin`
**Access:** Admin + SuperAdmin

Displays a site health snapshot:
- Total registered users
- Total prompts
- Average rating across all prompts
- Newest user registrations

---

### User List — `/admin/users`
**Access:** Admin + SuperAdmin

Lists all regular users (`User` role) with:
- Username, email, registration date
- Disabled/active status
- Actions: disable/enable, reset password, view detail

---

### User Detail — `/admin/users/{id}`
**Access:** Admin + SuperAdmin

Shows a user's profile and all prompts they have created.

---

### Prompt Moderation — `/admin/prompts`
**Access:** Admin + SuperAdmin

Lists all prompts in the system regardless of ownership. Admin can delete any prompt.

---

### Admin List — `/admin/admins`
**Access:** SuperAdmin only

Lists all Admin-role accounts with:
- Username, email, creation date
- Disabled/active status
- Actions: disable/enable, reset password, create new admin

Super Admin cannot disable themselves from this page.

---

## Authorization Summary

| Page | Admin | SuperAdmin |
|---|---|---|
| Dashboard | yes | yes |
| User list + detail | yes | yes |
| Prompt moderation | yes | yes |
| Admin list + management | no | yes |

---

## Registration

Public self-registration at `/Account/Register` remains open. Anyone can create a regular `User` account. Admins do not need to create regular user accounts.
