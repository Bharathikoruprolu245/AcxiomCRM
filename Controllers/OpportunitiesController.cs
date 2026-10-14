using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class OpportunitiesController : Controller
{
    private readonly ApplicationDbContext _db; private readonly IAuditService _audit;
    public OpportunitiesController(ApplicationDbContext db, IAuditService audit){_db=db;_audit=audit;}
    private IQueryable<Opportunity> Scoped()=>User.IsInRole(Roles.SalesExecutive)?_db.Opportunities.Where(x=>x.AssignedTo==User.Identity!.Name):_db.Opportunities;
    public async Task<IActionResult> Index(string? q,string? stage,string? status)
    {
        var d=Scoped().Include(x=>x.Customer).AsQueryable();
        if(!string.IsNullOrWhiteSpace(q)) d=d.Where(x=>x.OpportunityName.Contains(q)||(x.Customer!=null&&x.Customer.CustomerName.Contains(q)));
        if(!string.IsNullOrWhiteSpace(stage))d=d.Where(x=>x.Stage==stage);
        if(!string.IsNullOrWhiteSpace(status))d=d.Where(x=>x.Status==status);
        ViewBag.Query=q;ViewBag.Stage=stage;ViewBag.Status=status;ViewBag.Stages=OpportunityStages.All;
        return View(await d.OrderBy(x=>x.ExpectedCloseDate).ToListAsync());
    }
    public async Task<IActionResult> Details(int id){var x=await Scoped().Include(x=>x.Customer).Include(x=>x.Lead).FirstOrDefaultAsync(x=>x.OpportunityId==id);return x==null?NotFound():View(x);}
    public async Task<IActionResult> Create()
    {
        await LoadCustomers(); return View(new Opportunity{AssignedTo=User.Identity!.Name,ExpectedCloseDate=DateTime.Today.AddDays(30)});
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Opportunity model)
    {
        model.OpportunityId=0;model.CreatedDate=DateTime.UtcNow;
        if(User.IsInRole(Roles.SalesExecutive))model.AssignedTo=User.Identity!.Name;
        ValidateBusiness(model);
        if(!await CustomerIsVisible(model.CustomerId))ModelState.AddModelError(nameof(model.CustomerId),"Select a customer you are authorized to access.");
        if(!ModelState.IsValid){await LoadCustomers();return View(model);}
        _db.Opportunities.Add(model);
        await _db.SaveChangesAsync();await _audit.LogAsync("Create","Opportunity",model.OpportunityId.ToString(),null,model.OpportunityName);
        TempData["Success"]="Opportunity created.";return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Edit(int id){var x=await Scoped().FirstOrDefaultAsync(x=>x.OpportunityId==id);if(x==null)return NotFound();await LoadCustomers();return View(x);}
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,Opportunity model)
    {
        var x=await Scoped().FirstOrDefaultAsync(x=>x.OpportunityId==id);if(x==null)return NotFound();
        ValidateBusiness(model);
        if(!await CustomerIsVisible(model.CustomerId))ModelState.AddModelError(nameof(model.CustomerId),"Select a customer you are authorized to access.");
        if(!ModelState.IsValid){await LoadCustomers();return View(model);}
        var old=$"{x.Amount}; {x.Stage}; {x.Status}; {x.Probability}";
        x.OpportunityName=model.OpportunityName;x.CustomerId=model.CustomerId;x.LeadId=model.LeadId;x.Amount=model.Amount;x.Stage=model.Stage;x.Probability=model.Probability;x.ExpectedCloseDate=model.ExpectedCloseDate;x.Status=model.Status;x.Notes=model.Notes;
        if(!User.IsInRole(Roles.SalesExecutive))x.AssignedTo=model.AssignedTo;
        await _db.SaveChangesAsync();await _audit.LogAsync("Update","Opportunity",id.ToString(),old,$"{x.Amount}; {x.Stage}; {x.Status}; {x.Probability}");
        TempData["Success"]="Opportunity updated.";return RedirectToAction(nameof(Details),new{id});
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id){var x=await Scoped().FirstOrDefaultAsync(x=>x.OpportunityId==id);if(x==null)return NotFound();_db.Opportunities.Remove(x);try{await _db.SaveChangesAsync();}catch(DbUpdateException){TempData["Error"]="This opportunity has related records and cannot be deleted.";return RedirectToAction(nameof(Details),new{id});}await _audit.LogAsync("Delete","Opportunity",id.ToString(),x.OpportunityName,null);TempData["Success"]="Opportunity deleted.";return RedirectToAction(nameof(Index));}
    private void ValidateBusiness(Opportunity m)
    {
        if(m.Stage=="Won")m.Status="Won";else if(m.Stage=="Lost")m.Status="Lost";
        if(m.Amount<=0)ModelState.AddModelError(nameof(m.Amount),"Opportunity Amount must be greater than 0.");
        if(m.Probability<0||m.Probability>100)ModelState.AddModelError(nameof(m.Probability),"Probability must be between 0 and 100.");
        if(m.Status=="Open"&&m.ExpectedCloseDate.Date<DateTime.Today)ModelState.AddModelError(nameof(m.ExpectedCloseDate),"Expected Close Date cannot be in the past.");
        if(!OpportunityStages.All.Contains(m.Stage))ModelState.AddModelError(nameof(m.Stage),"Select a valid stage.");
        if(!new[]{"Open","Won","Lost"}.Contains(m.Status))ModelState.AddModelError(nameof(m.Status),"Select a valid status.");
    }
    private async Task<bool> CustomerIsVisible(int id)
    {
        var q=_db.Customers.Where(c=>c.CustomerId==id);
        if(User.IsInRole(Roles.SalesExecutive))q=q.Where(c=>c.AssignedTo==User.Identity!.Name||c.CreatedBy==User.Identity!.Name);
        return await q.AnyAsync();
    }
    private async Task LoadCustomers()
    {
        var q=_db.Customers.Where(c=>c.Status=="Active").AsQueryable();
        if(User.IsInRole(Roles.SalesExecutive))q=q.Where(c=>c.AssignedTo==User.Identity!.Name||c.CreatedBy==User.Identity!.Name);
        ViewBag.Customers=await q.OrderBy(c=>c.CustomerName).ToListAsync();ViewBag.Stages=OpportunityStages.All;
    }
}
