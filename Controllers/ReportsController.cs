using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize(Roles="Admin,Manager")]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _db;public ReportsController(ApplicationDbContext db){_db=db;}
    public async Task<IActionResult> Index(string? report,string? status,DateTime? from,DateTime? to)
    {
        ViewBag.Report=report??"pipeline";ViewBag.Status=status;ViewBag.From=from?.ToString("yyyy-MM-dd");ViewBag.To=to?.ToString("yyyy-MM-dd");
        var start=from??DateTime.Today.AddMonths(-12);var end=to?.Date.AddDays(1)??DateTime.Today.AddDays(1);
        ViewBag.CustomerCount=await _db.Customers.CountAsync(x=>x.CreatedDate>=start&&x.CreatedDate<end);
        ViewBag.LeadCount=await _db.Leads.CountAsync(x=>x.CreatedDate>=start&&x.CreatedDate<end);
        ViewBag.ConvertedCount=await _db.Leads.CountAsync(x=>x.Status=="Converted"&&x.CreatedDate>=start&&x.CreatedDate<end);
        ViewBag.FollowUps=await _db.FollowUps.CountAsync(x=>x.FollowUpDate>=start&&x.FollowUpDate<end);
        ViewBag.Overdue=await _db.FollowUps.CountAsync(x=>x.Status=="Planned"&&x.FollowUpDate<DateTime.Today);
        var pipeline=await _db.Opportunities.Where(x=>x.Status=="Open").GroupBy(x=>new{x.Stage,x.AssignedTo}).Select(g=>new PipelineRow{Stage=g.Key.Stage,Owner=g.Key.AssignedTo??"",Count=g.Count(),Amount=g.Sum(x=>x.Amount),Weighted=g.Sum(x=>x.Amount*x.Probability/100m)}).OrderBy(x=>x.Stage).ToListAsync();
        ViewBag.Pipeline=pipeline;
        ViewBag.Customers=await _db.Customers.Where(x=>x.CreatedDate>=start&&x.CreatedDate<end&&(status==null||x.Status==status)).OrderByDescending(x=>x.CreatedDate).Take(500).ToListAsync();
        ViewBag.Leads=await _db.Leads.Where(x=>x.CreatedDate>=start&&x.CreatedDate<end&&(status==null||x.Status==status)).OrderByDescending(x=>x.CreatedDate).Take(500).ToListAsync();
        ViewBag.Opportunities=await _db.Opportunities.Include(x=>x.Customer).Where(x=>x.CreatedDate>=start&&x.CreatedDate<end&&(status==null||x.Status==status)).OrderByDescending(x=>x.CreatedDate).Take(500).ToListAsync();
        ViewBag.Audit=await _db.AuditLogs.Where(x=>x.CreatedDate>=start&&x.CreatedDate<end).OrderByDescending(x=>x.CreatedDate).Take(100).ToListAsync();
        ViewBag.FollowUpRows=await _db.FollowUps.Include(x=>x.Customer).Include(x=>x.Lead).Where(x=>x.FollowUpDate>=start&&x.FollowUpDate<end&&(status==null||x.Status==status)).OrderBy(x=>x.FollowUpDate).Take(500).ToListAsync();
        ViewBag.UserActivity=await _db.AuditLogs.Where(x=>x.CreatedDate>=start&&x.CreatedDate<end).GroupBy(x=>x.UserName??"Unknown").Select(g=>new UserActivityRow{UserName=g.Key,ActionCount=g.Count(),LastActivity=g.Max(x=>x.CreatedDate)}).OrderByDescending(x=>x.ActionCount).ToListAsync();
        return View();
    }
    public sealed class PipelineRow{public string Stage{get;set;}="";public string Owner{get;set;}="";public int Count{get;set;}public decimal Amount{get;set;}public decimal Weighted{get;set;}}
    public sealed class UserActivityRow{public string UserName{get;set;}="";public int ActionCount{get;set;}public DateTime LastActivity{get;set;}}
}
