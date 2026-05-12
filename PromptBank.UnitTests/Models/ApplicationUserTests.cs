using PromptBank.Models;

namespace PromptBank.UnitTests.Models;

/// <summary>
/// Unit tests for the default property values of <see cref="ApplicationUser"/>.
/// </summary>
public class ApplicationUserTests
{
    /// <summary>
    /// Verifies that <see cref="ApplicationUser.IsDisabled"/> defaults to <c>false</c> on a new instance.
    /// </summary>
    [Fact]
    public void IsDisabled_DefaultsToFalse()
    {
        var user = new ApplicationUser();

        Assert.False(user.IsDisabled);
    }

    /// <summary>
    /// Verifies that <see cref="ApplicationUser.MustChangePassword"/> defaults to <c>false</c> on a new instance.
    /// </summary>
    [Fact]
    public void MustChangePassword_DefaultsToFalse()
    {
        var user = new ApplicationUser();

        Assert.False(user.MustChangePassword);
    }

    /// <summary>
    /// Verifies that <see cref="ApplicationUser.IsDisabled"/> can be set to <c>true</c>.
    /// </summary>
    [Fact]
    public void IsDisabled_CanBeSetToTrue()
    {
        var user = new ApplicationUser { IsDisabled = true };

        Assert.True(user.IsDisabled);
    }

    /// <summary>
    /// Verifies that <see cref="ApplicationUser.MustChangePassword"/> can be set to <c>true</c>.
    /// </summary>
    [Fact]
    public void MustChangePassword_CanBeSetToTrue()
    {
        var user = new ApplicationUser { MustChangePassword = true };

        Assert.True(user.MustChangePassword);
    }
}
