using AcxiomCRM.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize(Roles="Admin")]
public class AuditLogsController : Controller
{
    private readonly ApplicationDbContext _db;
    public AuditLogsController(ApplicationDbContext db){_db=db;}
    public async Task<IActionResult> Index(string? user,string? module,string? action,DateTime? from,DateTime? to,int page=1)
    {
        var q=_db.AuditLogs.AsNoTracking().AsQueryable();
        if(!string.IsNullOrWhiteSpace(user))q=q.Where(x=>(x.UserName??"").Contains(user)|| (x.UserId??"").Contains(user));
        if(!string.IsNullOrWhiteSpace(module))q=q.Where(x=>x.EntityName==module);
        if(!string.IsNullOrWhiteSpace(action))q=q.Where(x=>x.Action==action);
        if(from.HasValue)q=q.Where(x=>x.CreatedDate>=from.Value);
        if(to.HasValue)q=q.Where(x=>x.CreatedDate<to.Value.Date.AddDays(1));
        ViewBag.UserFilter=user;ViewBag.Module=module;ViewBag.Action=action;ViewBag.From=from?.ToString("yyyy-MM-dd");ViewBag.To=to?.ToString("yyyy-MM-dd");ViewBag.Page=page;
        return View(await q.OrderByDescending(x=>x.CreatedDate).Skip((Math.Max(1,page)-1)*50).Take(50).ToListAsync());
    }
}
