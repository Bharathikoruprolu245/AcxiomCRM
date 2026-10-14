using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class ActivitiesController : Controller
{
    private readonly ApplicationDbContext _db;private readonly IAuditService _audit;
    public ActivitiesController(ApplicationDbContext db,IAuditService audit){_db=db;_audit=audit;}
    private IQueryable<Activity> Scoped()=>User.IsInRole(Roles.SalesExecutive)?_db.Activities.Where(x=>x.AssignedTo==User.Identity!.Name):_db.Activities;
    public async Task<IActionResult> Index(string? type,string? status,DateTime? date)
    {
        var d=Scoped().Include(x=>x.Customer).Include(x=>x.Lead).AsQueryable();
        if(!string.IsNullOrWhiteSpace(type))d=d.Where(x=>x.ActivityType==type);
        if(!string.IsNullOrWhiteSpace(status))d=d.Where(x=>x.Status==status);
        if(date.HasValue)d=d.Where(x=>x.ActivityDate.Date==date.Value.Date);
        ViewBag.Type=type;ViewBag.Status=status;ViewBag.Date=date?.ToString("yyyy-MM-dd");
        return View(await d.OrderByDescending(x=>x.ActivityDate).ToListAsync());
    }
    public async Task<IActionResult> Create(){await LoadOptions();return View(new Activity{ActivityDate=DateTime.Today,AssignedTo=User.Identity!.Name});}
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Activity model)
    {
        model.ActivityId=0;if(User.IsInRole(Roles.SalesExecutive))model.AssignedTo=User.Identity!.Name;
        if(!ActivityTypes.All.Contains(model.ActivityType))ModelState.AddModelError(nameof(model.ActivityType),"Select a valid activity type.");
        if(model.CustomerId==null&&model.LeadId==null)ModelState.AddModelError("","Link the activity to a customer or lead.");
        if(model.CustomerId.HasValue&&!await CustomerVisible(model.CustomerId.Value))ModelState.AddModelError(nameof(model.CustomerId),"Customer not found.");
        if(model.LeadId.HasValue&&!await LeadVisible(model.LeadId.Value))ModelState.AddModelError(nameof(model.LeadId),"Lead not found.");
        if(!new[]{"Planned","Completed","Cancelled"}.Contains(model.Status))ModelState.AddModelError(nameof(model.Status),"Select a valid status.");
        if(!ModelState.IsValid){await LoadOptions();return View(model);}
        _db.Activities.Add(model);await _db.SaveChangesAsync();await _audit.LogAsync("Create","Activity",model.ActivityId.ToString(),null,model.Subject);
        TempData["Success"]="Activity created.";return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Edit(int id){var x=await Scoped().FirstOrDefaultAsync(x=>x.ActivityId==id);if(x==null)return NotFound();await LoadOptions();return View(x);}
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,Activity model)
    {
        var x=await Scoped().FirstOrDefaultAsync(x=>x.ActivityId==id);if(x==null)return NotFound();
        if(!ActivityTypes.All.Contains(model.ActivityType))ModelState.AddModelError(nameof(model.ActivityType),"Select a valid activity type.");
        if(!new[]{"Planned","Completed","Cancelled"}.Contains(model.Status))ModelState.AddModelError(nameof(model.Status),"Select a valid status.");
        if(model.CustomerId==null&&model.LeadId==null)ModelState.AddModelError("","Link the activity to a customer or lead.");
        if(model.CustomerId.HasValue&&!await CustomerVisible(model.CustomerId.Value))ModelState.AddModelError(nameof(model.CustomerId),"Customer not found.");
        if(model.LeadId.HasValue&&!await LeadVisible(model.LeadId.Value))ModelState.AddModelError(nameof(model.LeadId),"Lead not found.");
        if(!ModelState.IsValid){await LoadOptions();return View(model);}
        var old=$"{x.ActivityType};{x.Status}";x.ActivityType=model.ActivityType;x.Subject=model.Subject;x.Description=model.Description;x.ActivityDate=model.ActivityDate;x.CustomerId=model.CustomerId;x.LeadId=model.LeadId;x.Status=model.Status;
        if(!User.IsInRole(Roles.SalesExecutive))x.AssignedTo=model.AssignedTo;
        await _db.SaveChangesAsync();await _audit.LogAsync("Update","Activity",id.ToString(),old,$"{x.ActivityType};{x.Status}");
        TempData["Success"]="Activity updated.";return RedirectToAction(nameof(Index));
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id){var x=await Scoped().FirstOrDefaultAsync(x=>x.ActivityId==id);if(x==null)return NotFound();_db.Activities.Remove(x);await _db.SaveChangesAsync();await _audit.LogAsync("Delete","Activity",id.ToString(),x.Subject,null);TempData["Success"]="Activity deleted.";return RedirectToAction(nameof(Index));}
    private async Task<bool> CustomerVisible(int id){var q=_db.Customers.Where(x=>x.CustomerId==id);if(User.IsInRole(Roles.SalesExecutive))q=q.Where(x=>x.AssignedTo==User.Identity!.Name||x.CreatedBy==User.Identity!.Name);return await q.AnyAsync();}
    private async Task<bool> LeadVisible(int id){var q=_db.Leads.Where(x=>x.LeadId==id);if(User.IsInRole(Roles.SalesExecutive))q=q.Where(x=>x.AssignedTo==User.Identity!.Name);return await q.AnyAsync();}
    private async Task LoadOptions()
    {
        var customers=_db.Customers.AsQueryable();var leads=_db.Leads.AsQueryable();
        if(User.IsInRole(Roles.SalesExecutive)){var owner=User.Identity!.Name;customers=customers.Where(x=>x.AssignedTo==owner||x.CreatedBy==owner);leads=leads.Where(x=>x.AssignedTo==owner);}
        ViewBag.Customers=await customers.OrderBy(x=>x.CustomerName).ToListAsync();ViewBag.Leads=await leads.OrderBy(x=>x.LeadName).ToListAsync();
    }
}
