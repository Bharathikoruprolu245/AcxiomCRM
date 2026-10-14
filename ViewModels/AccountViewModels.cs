using System.ComponentModel.DataAnnotations;
namespace AcxiomCRM.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email or username is required.")] [Display(Name = "Email / Username")]
    public string EmailOrUserName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Password is required.")] [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    [Display(Name = "Remember me")] public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Full name is required."), StringLength(100)] [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Email is required."), EmailAddress(ErrorMessage = "Enter a valid email address."), StringLength(150)]
    public string Email { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")] [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Passwords do not match.")] [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
