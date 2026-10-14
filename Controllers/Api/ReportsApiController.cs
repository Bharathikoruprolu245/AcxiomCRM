using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers.Api;

[ApiController,Route("api/reports"),IgnoreAntiforgeryToken,Authorize(Roles="Admin,Manager")]
public class ReportsApiController:ControllerBase
{
    private readonly ApplicationDbContext _db;public ReportsApiController(ApplicationDbContext db){_db=db;}
    [HttpGet("pipeline")]
    public async Task<IActionResult> Pipeline()
    {
        var rows=await _db.Opportunities.Where(x=>x.Status=="Open").GroupBy(x=>new{x.Stage,x.AssignedTo}).Select(g=>new{g.Key.Stage,Owner=g.Key.AssignedTo,Count=g.Count(),Amount=g.Sum(x=>x.Amount),WeightedAmount=g.Sum(x=>x.Amount*x.Probability/100m)}).OrderBy(x=>x.Stage).ToListAsync();
        return Ok(rows);
    }
}
