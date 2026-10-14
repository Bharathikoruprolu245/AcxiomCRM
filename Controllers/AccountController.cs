using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
namespace AcxiomCRM.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly IAuditService _audit;
    public AccountController(UserManager<ApplicationUser> u, SignInManager<ApplicationUser> s, IAuditService a)
    { _users = u; _signIn = s; _audit = a; }

    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = await _users.FindByEmailAsync(vm.EmailOrUserName) ?? await _users.FindByNameAsync(vm.EmailOrUserName);
        if (user == null || !user.IsActive)
        {
            await _audit.LogAsync("LoginFailed", "Authentication", null, null, $"Identifier: {vm.EmailOrUserName}", "Failed", null, vm.EmailOrUserName);
            ModelState.AddModelError("", "Invalid login attempt.");   // generic: no user enumeration
            return View(vm);
        }

        var result = await _signIn.PasswordSignInAsync(user, vm.Password, vm.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            await _audit.LogAsync("Login", "Authentication", user.Id, null, null, "Success", user.Id, user.UserName);
            return !string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl)
                ? LocalRedirect(vm.ReturnUrl) : RedirectToAction("Index", "Home");
        }
        if (result.IsLockedOut)
        {
            await _audit.LogAsync("Lockout", "Authentication", user.Id, null, null, "Failed", user.Id, user.UserName);
            ModelState.AddModelError("", "Account locked due to repeated failed attempts. Try again later.");
            return View(vm);
        }
        await _audit.LogAsync("LoginFailed", "Authentication", user.Id, null, null, "Failed", user.Id, user.UserName);
        ModelState.AddModelError("", "Invalid login attempt.");
        return View(vm);
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var user = new ApplicationUser { UserName = vm.Email, Email = vm.Email, FullName = vm.FullName, EmailConfirmed = true };
        var res = await _users.CreateAsync(user, vm.Password);   // Identity hashes + enforces password policy
        if (!res.Succeeded)
        {
            foreach (var e in res.Errors) ModelState.AddModelError("", e.Description);
            return View(vm);
        }
        // Self-registration is always the least-privileged role. Role changes are Admin-only.
        await _users.AddToRoleAsync(user, Roles.SalesExecutive);
        await _audit.LogAsync("Register", "User", user.Id, null, $"Email: {user.Email}; Role: {Roles.SalesExecutive}", "Success", user.Id, user.UserName);
        await _signIn.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _audit.LogAsync("Logout", "Authentication");
        await _signIn.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}
