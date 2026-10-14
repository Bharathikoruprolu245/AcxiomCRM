using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    public HomeController(ApplicationDbContext db){_db=db;}
    public async Task<IActionResult> Index()
    {
        var customers=_db.Customers.AsQueryable();var leads=_db.Leads.AsQueryable();var opps=_db.Opportunities.AsQueryable();var followups=_db.FollowUps.AsQueryable();
        if(User.IsInRole(Roles.SalesExecutive))
        {
            var owner=User.Identity!.Name;
            customers=customers.Where(x=>x.AssignedTo==owner||x.CreatedBy==owner);
            leads=leads.Where(x=>x.AssignedTo==owner);opps=opps.Where(x=>x.AssignedTo==owner);followups=followups.Where(x=>x.AssignedTo==owner);
        }
        ViewBag.TotalCustomers=await customers.CountAsync();
        ViewBag.TotalLeads=await leads.CountAsync();
        ViewBag.OpenLeads=await leads.CountAsync(x=>x.Status!="Converted"&&x.Status!="Lost"&&x.Status!="Unqualified");
        ViewBag.TotalOpportunities=await opps.CountAsync();
        ViewBag.OpenOpportunities=await opps.CountAsync(x=>x.Status=="Open");
        ViewBag.WonOpportunities=await opps.CountAsync(x=>x.Status=="Won");
        ViewBag.LostOpportunities=await opps.CountAsync(x=>x.Status=="Lost");
        ViewBag.Pipeline=await opps.Where(x=>x.Status=="Open").Select(x=>(decimal?)x.Amount).SumAsync()??0m;
        ViewBag.PendingFollowUps=await followups.CountAsync(x=>x.Status=="Planned");
        var leadRows=await leads.GroupBy(x=>x.Status).Select(g=>new{Label=g.Key,Count=g.Count()}).ToListAsync();
        ViewBag.LeadLabels=leadRows.Select(x=>x.Label).ToArray();ViewBag.LeadValues=leadRows.Select(x=>x.Count).ToArray();
        var stageRows=await opps.GroupBy(x=>x.Stage).Select(g=>new{Label=g.Key,Count=g.Count()}).ToListAsync();
        ViewBag.StageLabels=OpportunityStages.All;ViewBag.StageValues=OpportunityStages.All.Select(s=>stageRows.FirstOrDefault(x=>x.Label==s)?.Count??0).ToArray();
        var months=Enumerable.Range(0,6).Select(i=>new DateTime(DateTime.Today.Year,DateTime.Today.Month,1).AddMonths(i-5)).ToArray();
        var startDate = months[0];
var endDate = months[months.Length - 1].AddMonths(1);

var won = await opps
    .Where(x => x.Status == "Won"
        && x.ExpectedCloseDate >= startDate
        && x.ExpectedCloseDate < endDate)
    .ToListAsync();
        ViewBag.MonthLabels=months.Select(x=>x.ToString("MMM yyyy")).ToArray();
        ViewBag.MonthValues=months.Select(m=>won.Where(x=>x.ExpectedCloseDate.Year==m.Year&&x.ExpectedCloseDate.Month==m.Month).Sum(x=>x.Amount)).ToArray();
        ViewBag.Upcoming=await followups.Where(x=>x.Status=="Planned").OrderBy(x=>x.FollowUpDate).Take(5).ToListAsync();
        return View();
    }
    [AllowAnonymous] public IActionResult Error()=>View();
}
