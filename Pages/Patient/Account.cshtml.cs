using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MMGC.Data;

namespace MMGC.Pages.Patient;

[Authorize(Roles = "Patient")]
public class AccountModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AccountModel> _logger;

    public AccountModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AccountModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    public string? Email { get; set; }
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public ChangePasswordInputModel ChangePasswordInput { get; set; } = new();

    public IActionResult OnGet()
    {
        Email = User.Identity?.Name;
        SuccessMessage = TempData["SuccessMessage"] as string;
        ErrorMessage = TempData["ErrorMessage"] as string;
        return Page();
    }

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        Email = User.Identity?.Name;
        if (string.IsNullOrEmpty(ChangePasswordInput.CurrentPassword) || string.IsNullOrEmpty(ChangePasswordInput.NewPassword))
        {
            ErrorMessage = "Current password and new password are required.";
            return Page();
        }
        if (ChangePasswordInput.NewPassword.Length < 6)
        {
            ErrorMessage = "New password must be at least 6 characters.";
            return Page();
        }
        if (ChangePasswordInput.NewPassword != ChangePasswordInput.ConfirmPassword)
        {
            ErrorMessage = "New password and confirmation do not match.";
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            ErrorMessage = "User not found.";
            return Page();
        }

        var result = await _userManager.ChangePasswordAsync(user, ChangePasswordInput.CurrentPassword, ChangePasswordInput.NewPassword);
        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("Patient changed password.");
            SuccessMessage = "Your password has been changed successfully.";
            ChangePasswordInput = new ChangePasswordInputModel();
            return Page();
        }

        ErrorMessage = result.Errors.FirstOrDefault()?.Description ?? "Failed to change password.";
        return Page();
    }

    public class ChangePasswordInputModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword))]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
