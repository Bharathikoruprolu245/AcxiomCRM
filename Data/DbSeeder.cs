using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity;
namespace AcxiomCRM.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider sp, IConfiguration cfg)
    {
        var roleMgr = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userMgr = sp.GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var r in Roles.All)
            if (!await roleMgr.RoleExistsAsync(r)) await roleMgr.CreateAsync(new IdentityRole(r));

        var pwd = cfg["Seed:DefaultPassword"];
if (string.IsNullOrWhiteSpace(pwd))
{
    throw new InvalidOperationException(
        "Configure Seed:DefaultPassword using .NET User Secrets before running the database seeder.");
}
        await EnsureUser(userMgr, "admin@acxiomcrm.com", "System Admin", Roles.Admin, pwd);
        await EnsureUser(userMgr, "manager@acxiomcrm.com", "Sales Manager", Roles.Manager, pwd);
        await EnsureUser(userMgr, "sales@acxiomcrm.com", "Sales Executive", Roles.SalesExecutive, pwd);
    }

    private static async Task EnsureUser(UserManager<ApplicationUser> m, string email, string name, string role, string pwd)
    {
        if (await m.FindByEmailAsync(email) != null) return;
        var u = new ApplicationUser { UserName = email, Email = email, FullName = name, EmailConfirmed = true, IsActive = true };
        var res = await m.CreateAsync(u, pwd);
        if (res.Succeeded) await m.AddToRoleAsync(u, role);
    }
}
