# PRD: Admin Panel

**Status:** Ready for implementation  
**Author:** Sanuja Ariyapperuma  
**Date:** 2026-05-12  

---

## 1. Problem Statement

PromptBank has no way to manage users or moderate content. As the user base grows, there is no mechanism to disable problematic accounts, remove low-quality prompts, or create admin-level staff accounts. All of this currently requires direct database access.

---

## 2. Goal

Introduce a secure, role-gated admin panel at `/admin` that gives designated administrators full control over users and content, without exposing any admin capability to regular users.

---

## 3. Non-Goals

- OAuth / social login
- Promoting or demoting users between roles
- Closing public registration
- Email-based flows (password reset links, invite emails)
- Audit logging
- Multi-tenancy

---

## 4. User Roles

Three distinct, immutable roles are introduced. A user cannot move between roles.

| Role | Description |
|---|---|
| `User` | Self-registered. Can create, rate, and pin prompts. |
| `Admin` | Created by Super Admin. Manages regular users and moderates prompts. |
| `SuperAdmin` | Singleton. Manages admin accounts. Highest privilege. |

---

## 5. Seeded Accounts

The following accounts are seeded at startup via `Program.cs`.

| Username | Password | Role |
|---|---|---|
| `admin` | `Admin@1234` | `Admin` |
| `superadmin` | `SuperAdmin@1234` | `SuperAdmin` |

---

## 6. Scope of Work

### 6.1 Model Changes

Add two fields to `ApplicationUser`:

| Field | Type | Purpose |
|---|---|---|
| `IsDisabled` | `bool` | Explicit admin-disable flag, independent of Identity lockout |
| `MustChangePassword` | `bool` | Forces password change on next login |

An EF Core migration is required.

---

### 6.2 Disable / Enable User

**Who can perform:** Admin (on regular users), SuperAdmin (on admins)  
**Who is protected:** Admins cannot disable themselves; SuperAdmin cannot disable themselves

When a user is disabled:
- `IsDisabled = true`
- Login is blocked
- Their prompts are hidden from the public listing

When re-enabled:
- `IsDisabled = false`
- Login is restored
- Their prompts reappear in the public listing

---

### 6.3 Password Reset

**Who can perform:** Admin (on regular users), SuperAdmin (on admins)

1. Admin sets a temporary password for the target account
2. `MustChangePassword = true` is set
3. On the user's next login they are redirected to a mandatory change-password page before accessing any other page

---

### 6.4 Admin Account Creation

**Who can perform:** SuperAdmin only

1. SuperAdmin fills in username, email, and a temporary password
2. Account is created with the `Admin` role
3. `MustChangePassword = true` is set automatically
4. User must change their password on first login

---

### 6.5 Prompt Moderation

**Who can perform:** Admin + SuperAdmin

Admin can view all prompts in the system (regardless of ownership) and delete any of them. Deleted prompts are permanently removed.

---

## 7. Pages

### `/admin` — Dashboard
**Access:** Admin, SuperAdmin  
Displays:
- Total registered users
- Total prompts
- Average rating across all prompts
- 5 most recently registered users

---

### `/admin/users` — Regular User List
**Access:** Admin, SuperAdmin  
Displays a table of all `User`-role accounts with:
- Username, email, registration date, status (active / disabled)
- Actions: Disable / Enable, Reset Password, View Detail

---

### `/admin/users/{id}` — User Detail
**Access:** Admin, SuperAdmin  
Displays:
- User profile (username, email, registration date, status)
- All prompts created by the user (title, rating, created date)
- Admin can delete any of the user's prompts from this page

---

### `/admin/prompts` — Prompt Moderation
**Access:** Admin, SuperAdmin  
Displays a table of all prompts with:
- Title, owner username, rating, created date
- Action: Delete

---

### `/admin/admins` — Admin Account List
**Access:** SuperAdmin only  
Displays a table of all `Admin`-role accounts with:
- Username, email, creation date, status (active / disabled)
- Actions: Disable / Enable, Reset Password
- Button: Create New Admin → opens create form (username, email, temporary password)

---

## 8. Layout and Navigation

- All admin pages use a dedicated `_AdminLayout.cshtml` separate from the main app layout
- The admin layout includes a left sidebar or top nav with links to: Dashboard, Users, Prompts, Admins (SuperAdmin only)
- The main app layout shows no admin-specific navigation to regular users
- An "Admin Panel" link is shown in the main nav only when the logged-in user has the `Admin` or `SuperAdmin` role

---

## 9. Authorization

| Page | User | Admin | SuperAdmin |
|---|---|---|---|
| `/admin` (Dashboard) | — | yes | yes |
| `/admin/users` | — | yes | yes |
| `/admin/users/{id}` | — | yes | yes |
| `/admin/prompts` | — | yes | yes |
| `/admin/admins` | — | — | yes |

Unauthorized access redirects to the main login page.  
Self-disable is blocked server-side with a 403 for all roles.

---

## 10. Prompt Visibility Rule

The public prompt listing (`/`) must exclude prompts owned by users where `IsDisabled = true`. This filter is applied in `PromptService` so it is consistently enforced across all query paths.

---

## 11. MustChangePassword Flow

1. User logs in with a temporary password
2. After successful authentication, middleware checks `MustChangePassword`
3. If `true`, the user is redirected to `/Account/ChangePassword` regardless of where they were navigating
4. After a successful password change, `MustChangePassword = false` and the user proceeds normally

---

## 12. Unit Tests (`PromptBank.UnitTests`)

Unit tests cover all service-layer logic using an in-memory database. No Razor Pages or HTTP involved.

### AdminService (new service)
- Creating an admin account sets `MustChangePassword = true` and assigns `Admin` role
- Disabling a user sets `IsDisabled = true`
- Enabling a user sets `IsDisabled = false`
- Disabling self throws / returns error (admin cannot disable themselves)
- Resetting a user's password sets `MustChangePassword = true`
- SuperAdmin cannot disable themselves

### PromptService (existing, extended)
- `GetAllSortedAsync` excludes prompts owned by disabled users
- `GetAllSortedAsync` still returns prompts from enabled users correctly

### Dashboard stats
- Stats query returns correct total user count, prompt count, and average rating
- Recent registrations returns the correct 5 most recent users

---

## 13. E2E Tests (`PromptBank.Tests` — Playwright)

E2E tests cover full browser flows against a running app instance.

### Authentication guard
- Navigating to `/admin` while unauthenticated redirects to login
- Navigating to `/admin` as a regular `User` returns 403
- Navigating to `/admin/admins` as an `Admin` returns 403

### MustChangePassword flow
- Logging in with `MustChangePassword = true` redirects to change-password page
- Navigating to any other page before changing password redirects back to change-password
- After changing password, user proceeds to the originally requested page

### Dashboard
- `superadmin` logs in and sees correct user count, prompt count on the dashboard

### User management (as `admin`)
- Admin can disable a regular user — disabled user cannot log in afterward
- Admin can re-enable a disabled user — user can log in again
- Disabled user's prompts do not appear on the public listing (`/`)
- Admin can reset a regular user's password — user is forced to change password on next login
- Admin cannot disable themselves (disable button absent or action returns error)

### Prompt moderation (as `admin`)
- Admin can view all prompts on `/admin/prompts`
- Admin can delete a prompt — it no longer appears on the public listing

### User detail (as `admin`)
- Admin can navigate to a user's detail page and see their prompts
- Admin can delete a prompt from the user detail page

### Admin management (as `superadmin`)
- SuperAdmin can create a new admin account — new admin must change password on first login
- SuperAdmin can disable an admin — disabled admin cannot log in
- SuperAdmin can reset an admin's password — admin is forced to change password on next login
- SuperAdmin cannot disable themselves

---

## 15. Acceptance Criteria

- [ ] `Admin` and `SuperAdmin` roles exist and are seeded at startup
- [ ] `admin` and `superadmin` seed accounts are created with correct roles and passwords
- [ ] `IsDisabled` and `MustChangePassword` fields exist on `ApplicationUser` with a migration
- [ ] Disabled users cannot log in
- [ ] Disabled users' prompts do not appear in the public listing
- [ ] Admins cannot disable themselves (server returns 403)
- [ ] SuperAdmin cannot disable themselves (server returns 403)
- [ ] Admin can reset a regular user's password; sets `MustChangePassword = true`
- [ ] SuperAdmin can reset an admin's password; sets `MustChangePassword = true`
- [ ] SuperAdmin can create new Admin accounts with `MustChangePassword = true`
- [ ] Users with `MustChangePassword = true` are redirected to change-password page on login
- [ ] Admin can delete any prompt from the moderation page or user detail page
- [ ] `/admin/admins` is inaccessible to `Admin`-role users (returns 403)
- [ ] Dashboard displays correct counts and recent registrations
- [ ] All admin pages use `_AdminLayout.cshtml`
- [ ] Main app nav shows "Admin Panel" link only to `Admin` and `SuperAdmin` users
- [ ] Public registration at `/Account/Register` remains open and unchanged
