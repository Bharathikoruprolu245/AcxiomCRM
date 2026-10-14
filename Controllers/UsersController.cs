using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize(Roles="Admin")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _users;private readonly RoleManager<IdentityRole> _roles;private readonly IAuditService _audit;
    public UsersController(UserManager<ApplicationUser> users,RoleManager<IdentityRole> roles,IAuditService audit){_users=users;_roles=roles;_audit=audit;}
    public async Task<IActionResult> Index(string? q)
    {
        var users=_users.Users.AsNoTracking().AsQueryable();
        if(!string.IsNullOrWhiteSpace(q))users=users.Where(x=>(x.Email??"").Contains(q)||x.FullName.Contains(q));
        var list=await users.OrderBy(x=>x.Email).ToListAsync();
        var rows=new List<UserRow>();
        foreach(var u in list)rows.Add(new UserRow{Id=u.Id,Email=u.Email??"",FullName=u.FullName,IsActive=u.IsActive,LockedOut=await _users.IsLockedOutAsync(u),Role=(await _users.GetRolesAsync(u)).FirstOrDefault()??"None"});
        ViewBag.Query=q;return View(rows);
    }
    public IActionResult Create()=>View(new UserEditModel{IsActive=true});
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserEditModel m)
    {
        if(!Roles.All.Contains(m.Role))ModelState.AddModelError(nameof(m.Role),"Select a valid role.");
        if(string.IsNullOrWhiteSpace(m.Password))ModelState.AddModelError(nameof(m.Password),"A temporary password is required.");
        if(!ModelState.IsValid)return View(m);
        var u=new ApplicationUser{UserName=m.Email,Email=m.Email,FullName=m.FullName,EmailConfirmed=true,IsActive=m.IsActive};
        var result=await _users.CreateAsync(u,m.Password);
        if(!result.Succeeded){foreach(var e in result.Errors)ModelState.AddModelError("",e.Description);return View(m);}
        await _users.AddToRoleAsync(u,m.Role);await _audit.LogAsync("Create","User",u.Id,null,$"{u.Email}; {m.Role}");
        TempData["Success"]="User created.";return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Edit(string id)
    {
        var u=await _users.FindByIdAsync(id);if(u==null)return NotFound();
        return View(new UserEditModel{Id=u.Id,Email=u.Email??"",FullName=u.FullName,IsActive=u.IsActive,Role=(await _users.GetRolesAsync(u)).FirstOrDefault()??Roles.SalesExecutive});
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id,UserEditModel m)
    {
        var u=await _users.FindByIdAsync(id);if(u==null)return NotFound();
        if(!Roles.All.Contains(m.Role))ModelState.AddModelError(nameof(m.Role),"Select a valid role.");
        ModelState.Remove(nameof(m.Password));
        if(!ModelState.IsValid)return View(m);
        var oldRole=(await _users.GetRolesAsync(u)).FirstOrDefault()??"None";var oldActive=u.IsActive;
        u.FullName=m.FullName;u.IsActive=m.IsActive;
        var res=await _users.UpdateAsync(u);if(!res.Succeeded){foreach(var e in res.Errors)ModelState.AddModelError("",e.Description);return View(m);}
        await _users.RemoveFromRolesAsync(u,await _users.GetRolesAsync(u));await _users.AddToRoleAsync(u,m.Role);
        if(!string.IsNullOrWhiteSpace(m.Password)){var token=await _users.GeneratePasswordResetTokenAsync(u);var pr=await _users.ResetPasswordAsync(u,token,m.Password);if(!pr.Succeeded){foreach(var e in pr.Errors)ModelState.AddModelError("",e.Description);return View(m);}}
        await _audit.LogAsync("RoleChange","User",u.Id,$"{oldRole}; active={oldActive}",$"{m.Role}; active={m.IsActive}");
        TempData["Success"]="User updated.";return RedirectToAction(nameof(Index));
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var u=await _users.FindByIdAsync(id);if(u==null)return NotFound();
        if(u.Id==_users.GetUserId(User)){TempData["Error"]="You cannot deactivate your own account.";return RedirectToAction(nameof(Index));}
        u.IsActive=!u.IsActive;await _users.UpdateAsync(u);await _audit.LogAsync(u.IsActive?"Activate":"Deactivate","User",u.Id,null,u.IsActive.ToString());
        TempData["Success"]="User status changed.";return RedirectToAction(nameof(Index));
    }
    public sealed class UserRow{public string Id{get;set;}="";public string Email{get;set;}="";public string FullName{get;set;}="";public bool IsActive{get;set;}public bool LockedOut{get;set;}public string Role{get;set;}="";}
    public sealed class UserEditModel
    {
        public string? Id{get;set;}
        [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(100)] public string FullName{get;set;}="";
        [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.EmailAddress, System.ComponentModel.DataAnnotations.StringLength(150)] public string Email{get;set;}="";
        public bool IsActive{get;set;}=true;
        [System.ComponentModel.DataAnnotations.Required] public string Role{get;set;}=Roles.SalesExecutive;
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)] public string? Password{get;set;}
    }
}
