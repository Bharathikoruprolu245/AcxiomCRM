using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class FollowUpsController : Controller
{
    private readonly ApplicationDbContext _db; private readonly IAuditService _audit;
    public FollowUpsController(ApplicationDbContext db,IAuditService audit){_db=db;_audit=audit;}
    private IQueryable<FollowUp> Scoped()=>User.IsInRole(Roles.SalesExecutive)?_db.FollowUps.Where(x=>x.AssignedTo==User.Identity!.Name):_db.FollowUps;
    public async Task<IActionResult> Index(string? status,string? assignedTo,DateTime? from,DateTime? to)
    {
        var d=Scoped().Include(x=>x.Customer).Include(x=>x.Lead).AsQueryable();
        if(!string.IsNullOrWhiteSpace(status))d=d.Where(x=>x.Status==status);
        if(!string.IsNullOrWhiteSpace(assignedTo)&&!User.IsInRole(Roles.SalesExecutive))d=d.Where(x=>x.AssignedTo==assignedTo);
        if(from.HasValue)d=d.Where(x=>x.FollowUpDate>=from.Value);
        if(to.HasValue)d=d.Where(x=>x.FollowUpDate<to.Value.Date.AddDays(1));
        ViewBag.Status=status;ViewBag.AssignedTo=assignedTo;ViewBag.From=from?.ToString("yyyy-MM-dd");ViewBag.To=to?.ToString("yyyy-MM-dd");
        return View(await d.OrderBy(x=>x.FollowUpDate).ToListAsync());
    }
    public async Task<IActionResult> Create(){await LoadOptions();return View(new FollowUp{FollowUpDate=DateTime.Today.AddDays(1),AssignedTo=User.Identity!.Name});}
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUp model)
    {
        model.FollowUpId=0;if(User.IsInRole(Roles.SalesExecutive))model.AssignedTo=User.Identity!.Name;
        if(model.FollowUpDate.Date<DateTime.Today)ModelState.AddModelError(nameof(model.FollowUpDate),"Follow-up date cannot be earlier than today.");
        if((model.CustomerId.HasValue?1:0)+(model.LeadId.HasValue?1:0)+(model.OpportunityId.HasValue?1:0)!=1)ModelState.AddModelError("","Choose exactly one related customer, lead, or opportunity.");
        if(model.CustomerId.HasValue&&!await CustomerVisible(model.CustomerId.Value))ModelState.AddModelError(nameof(model.CustomerId),"Customer not found.");
        if(model.LeadId.HasValue&&!await LeadVisible(model.LeadId.Value))ModelState.AddModelError(nameof(model.LeadId),"Lead not found.");
        if(model.OpportunityId.HasValue&&!await OpportunityVisible(model.OpportunityId.Value))ModelState.AddModelError(nameof(model.OpportunityId),"Opportunity not found.");
        if(!ModelState.IsValid){await LoadOptions();return View(model);}
        _db.FollowUps.Add(model);await _db.SaveChangesAsync();await _audit.LogAsync("Create","FollowUp",model.FollowUpId.ToString(),null,model.Subject);
        TempData["Success"]="Follow-up scheduled.";return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Edit(int id){var x=await Scoped().FirstOrDefaultAsync(x=>x.FollowUpId==id);if(x==null)return NotFound();await LoadOptions();return View(x);}
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,FollowUp model)
    {
        var x=await Scoped().FirstOrDefaultAsync(x=>x.FollowUpId==id);if(x==null)return NotFound();
        if(model.Status=="Planned"&&model.FollowUpDate.Date<DateTime.Today)ModelState.AddModelError(nameof(model.FollowUpDate),"Follow-up date cannot be earlier than today.");
        if(!FollowUpStatuses.All.Contains(model.Status))ModelState.AddModelError(nameof(model.Status),"Select a valid status.");
        if((model.CustomerId.HasValue?1:0)+(model.LeadId.HasValue?1:0)+(model.OpportunityId.HasValue?1:0)!=1)ModelState.AddModelError("","Choose exactly one related customer, lead, or opportunity.");
        if(model.CustomerId.HasValue&&!await CustomerVisible(model.CustomerId.Value))ModelState.AddModelError(nameof(model.CustomerId),"Customer not found.");
        if(model.LeadId.HasValue&&!await LeadVisible(model.LeadId.Value))ModelState.AddModelError(nameof(model.LeadId),"Lead not found.");
        if(model.OpportunityId.HasValue&&!await OpportunityVisible(model.OpportunityId.Value))ModelState.AddModelError(nameof(model.OpportunityId),"Opportunity not found.");
        if(!ModelState.IsValid){await LoadOptions();return View(model);}
        var old=$"{x.FollowUpDate:yyyy-MM-dd}; {x.Status}";
        x.CustomerId=model.CustomerId;x.LeadId=model.LeadId;x.OpportunityId=model.OpportunityId;x.FollowUpDate=model.FollowUpDate;x.Subject=model.Subject;x.FollowUpType=model.FollowUpType;x.Remarks=model.Remarks;x.Status=model.Status;
        if(!User.IsInRole(Roles.SalesExecutive))x.AssignedTo=model.AssignedTo;
        await _db.SaveChangesAsync();await _audit.LogAsync("Update","FollowUp",id.ToString(),old,$"{x.FollowUpDate:yyyy-MM-dd}; {x.Status}");
        TempData["Success"]="Follow-up updated.";return RedirectToAction(nameof(Index));
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id){var x=await Scoped().FirstOrDefaultAsync(x=>x.FollowUpId==id);if(x==null)return NotFound();var old=x.Status;x.Status="Completed";await _db.SaveChangesAsync();await _audit.LogAsync("Complete","FollowUp",id.ToString(),old,x.Status);TempData["Success"]="Follow-up completed.";return RedirectToAction(nameof(Index));}
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id){var x=await Scoped().FirstOrDefaultAsync(x=>x.FollowUpId==id);if(x==null)return NotFound();_db.FollowUps.Remove(x);await _db.SaveChangesAsync();await _audit.LogAsync("Delete","FollowUp",id.ToString(),x.Subject,null);TempData["Success"]="Follow-up deleted.";return RedirectToAction(nameof(Index));}
    private async Task<bool> CustomerVisible(int id){var q=_db.Customers.Where(x=>x.CustomerId==id);if(User.IsInRole(Roles.SalesExecutive))q=q.Where(x=>x.AssignedTo==User.Identity!.Name||x.CreatedBy==User.Identity!.Name);return await q.AnyAsync();}
    private async Task<bool> LeadVisible(int id){var q=_db.Leads.Where(x=>x.LeadId==id);if(User.IsInRole(Roles.SalesExecutive))q=q.Where(x=>x.AssignedTo==User.Identity!.Name);return await q.AnyAsync();}
    private async Task<bool> OpportunityVisible(int id){var q=_db.Opportunities.Where(x=>x.OpportunityId==id);if(User.IsInRole(Roles.SalesExecutive))q=q.Where(x=>x.AssignedTo==User.Identity!.Name);return await q.AnyAsync();}
    private async Task LoadOptions()
    {
        var customers=_db.Customers.AsQueryable();var leads=_db.Leads.AsQueryable();var opps=_db.Opportunities.AsQueryable();
        if(User.IsInRole(Roles.SalesExecutive)){var owner=User.Identity!.Name;customers=customers.Where(x=>x.AssignedTo==owner||x.CreatedBy==owner);leads=leads.Where(x=>x.AssignedTo==owner);opps=opps.Where(x=>x.AssignedTo==owner);}
        ViewBag.Customers=await customers.OrderBy(x=>x.CustomerName).ToListAsync();ViewBag.Leads=await leads.OrderBy(x=>x.LeadName).ToListAsync();ViewBag.Opportunities=await opps.OrderBy(x=>x.OpportunityName).ToListAsync();
    }
}
