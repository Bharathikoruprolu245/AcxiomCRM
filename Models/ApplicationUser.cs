using Microsoft.AspNetCore.Identity;
namespace AcxiomCRM.Models;

// Extends IdentityUser: Identity manages hashing, lockout, roles, cookies (spec 17.4).
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string SalesExecutive = "SalesExecutive";
    public static readonly string[] All = { Admin, Manager, SalesExecutive };
}
