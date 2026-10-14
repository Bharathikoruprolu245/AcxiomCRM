using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class LeadsController : Controller
{
    private readonly ApplicationDbContext _db; private readonly IAuditService _audit;
    public LeadsController(ApplicationDbContext db, IAuditService audit) { _db=db; _audit=audit; }
    private IQueryable<Lead> Scoped() => User.IsInRole(Roles.SalesExecutive)
        ? _db.Leads.Where(x => x.AssignedTo == User.Identity!.Name) : _db.Leads;

    public async Task<IActionResult> Index(string? q, string? status, string? assignedTo)
    {
        var data=Scoped();
        if(!string.IsNullOrWhiteSpace(q)) data=data.Where(x=>x.LeadName.Contains(q)||(x.CompanyName??"").Contains(q)||x.Email.Contains(q)||x.Phone.Contains(q));
        if(!string.IsNullOrWhiteSpace(status)) data=data.Where(x=>x.Status==status);
        if(!string.IsNullOrWhiteSpace(assignedTo) && !User.IsInRole(Roles.SalesExecutive)) data=data.Where(x=>x.AssignedTo==assignedTo);
        ViewBag.Query=q; ViewBag.Status=status; ViewBag.AssignedTo=assignedTo; ViewBag.Statuses=LeadStatuses.All;
        return View(await data.OrderByDescending(x=>x.CreatedDate).ToListAsync());
    }
    public async Task<IActionResult> Details(int id) { var x=await Scoped().FirstOrDefaultAsync(x=>x.LeadId==id); return x==null?NotFound():View(x); }
    public IActionResult Create() { ViewBag.Statuses=LeadStatuses.All; return View(new Lead { LeadCode="LEAD-"+Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(), AssignedTo=User.Identity!.Name }); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Lead model)
    {
        model.LeadId=0; model.CreatedDate=DateTime.UtcNow; ViewBag.Statuses=LeadStatuses.All;
        if(User.IsInRole(Roles.SalesExecutive)) model.AssignedTo=User.Identity!.Name;
        if(!LeadStatuses.All.Contains(model.Status)) ModelState.AddModelError(nameof(model.Status),"Choose a valid lead status.");
        if(!ModelState.IsValid) return View(model);
        _db.Leads.Add(model);
        try { await _db.SaveChangesAsync(); } catch(DbUpdateException) { ModelState.AddModelError("", "Lead code already exists."); return View(model); }
        await _audit.LogAsync("Create","Lead",model.LeadId.ToString(),null,model.LeadName);
        TempData["Success"]="Lead created."; return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Edit(int id) { var x=await Scoped().FirstOrDefaultAsync(x=>x.LeadId==id); if(x==null)return NotFound(); ViewBag.OriginalStatus=x.Status; ViewBag.Statuses=LeadStatuses.All; return View(x); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Lead model)
    {
        var x=await Scoped().FirstOrDefaultAsync(x=>x.LeadId==id); if(x==null)return NotFound();
        if(!LeadStatuses.All.Contains(model.Status)) ModelState.AddModelError(nameof(model.Status),"Choose a valid lead status.");
        if(model.Status!=x.Status && (!LeadStatuses.Transitions.TryGetValue(x.Status,out var allowed)||!allowed.Contains(model.Status)))
            ModelState.AddModelError(nameof(model.Status),$"Transition from {x.Status} to {model.Status} is not allowed.");
        if(!ModelState.IsValid){ ViewBag.OriginalStatus=x.Status; ViewBag.Statuses=LeadStatuses.All; return View(model); }
        var old=$"{x.Status}; {x.ExpectedValue}; {x.AssignedTo}";
        x.LeadName=model.LeadName; x.Email=model.Email; x.Phone=model.Phone; x.CompanyName=model.CompanyName; x.Source=model.Source;
        x.Status=model.Status; x.Priority=model.Priority; x.ExpectedValue=model.ExpectedValue; x.Notes=model.Notes;
        if(!User.IsInRole(Roles.SalesExecutive)) x.AssignedTo=model.AssignedTo;
        await _db.SaveChangesAsync(); await _audit.LogAsync("Update","Lead",id.ToString(),old,$"{x.Status}; {x.ExpectedValue}; {x.AssignedTo}");
        TempData["Success"]="Lead updated."; return RedirectToAction(nameof(Details),new{id});
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var x=await Scoped().FirstOrDefaultAsync(x=>x.LeadId==id); if(x==null)return NotFound();
        if(await _db.Opportunities.AnyAsync(o=>o.LeadId==id) || await _db.FollowUps.AnyAsync(f=>f.LeadId==id) || await _db.Activities.AnyAsync(a=>a.LeadId==id))
        { TempData["Error"]="This lead has related records. Mark it Lost instead of deleting it."; return RedirectToAction(nameof(Details),new{id}); }
        _db.Leads.Remove(x); await _db.SaveChangesAsync(); await _audit.LogAsync("Delete","Lead",id.ToString(),x.LeadName,null);
        TempData["Success"]="Lead deleted."; return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(int id)
    {
        var lead=await Scoped().FirstOrDefaultAsync(x=>x.LeadId==id);
        if(lead==null)return NotFound();
        if(lead.Status!="Qualified"){ TempData["Error"]="Only qualified leads can be converted."; return RedirectToAction(nameof(Details),new{id}); }
        var existing=await _db.Customers.FirstOrDefaultAsync(c=>c.Email==lead.Email || c.Phone==lead.Phone);
        if(existing==null)
        {
            existing=new Customer { CustomerCode="CUS-"+Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(), CustomerName=lead.LeadName, Email=lead.Email, Phone=lead.Phone, CompanyName=lead.CompanyName, AssignedTo=lead.AssignedTo, CreatedBy=User.Identity!.Name, CreatedDate=DateTime.UtcNow };
            _db.Customers.Add(existing);
            try { await _db.SaveChangesAsync(); } catch(DbUpdateException) { TempData["Error"]="A customer with this email or phone already exists."; return RedirectToAction(nameof(Details),new{id}); }
        }
        lead.Status=LeadStatuses.Converted;
        if(lead.ExpectedValue>0 && !await _db.Opportunities.AnyAsync(o=>o.LeadId==lead.LeadId))
        {
            _db.Opportunities.Add(new Opportunity { OpportunityName=lead.LeadName+" Opportunity", CustomerId=existing.CustomerId, LeadId=lead.LeadId, Amount=lead.ExpectedValue, Stage=OpportunityStages.Qualification, Probability=10, ExpectedCloseDate=DateTime.Today.AddDays(30), Status="Open", AssignedTo=lead.AssignedTo });
        }
        await _db.SaveChangesAsync(); await _audit.LogAsync("Convert","Lead",id.ToString(), "Qualified", "Converted");
        TempData["Success"]="Lead converted to customer and opportunity (when expected value is positive)."; return RedirectToAction(nameof(Details),new{id});
    }
}
