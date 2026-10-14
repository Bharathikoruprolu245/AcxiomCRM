using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers.Api;

[ApiController,Route("api/opportunities"),IgnoreAntiforgeryToken,Authorize]
public class OpportunitiesApiController:ControllerBase
{
    private readonly ApplicationDbContext _db;private readonly IAuditService _audit;
    public OpportunitiesApiController(ApplicationDbContext db,IAuditService audit){_db=db;_audit=audit;}
    private IQueryable<Opportunity> Scope()=>User.IsInRole(Roles.SalesExecutive)?_db.Opportunities.Where(x=>x.AssignedTo==User.Identity!.Name):_db.Opportunities;
    [HttpGet] public async Task<IActionResult> Get(string? q,string? stage){var d=Scope().Include(x=>x.Customer).AsQueryable();if(!string.IsNullOrWhiteSpace(q))d=d.Where(x=>x.OpportunityName.Contains(q)||(x.Customer!=null&&x.Customer.CustomerName.Contains(q)));if(!string.IsNullOrWhiteSpace(stage))d=d.Where(x=>x.Stage==stage);return Ok(await d.Select(x=>new OpportunityDto(x.OpportunityId,x.OpportunityName,x.CustomerId,x.Customer==null?"":x.Customer.CustomerName,x.Amount,x.Probability,x.Stage,x.Status,x.ExpectedCloseDate,x.Amount*x.Probability/100m,x.AssignedTo)).ToListAsync());}
    [HttpPost] public async Task<IActionResult> Create(OpportunityInput m)
    {
        if(m.Amount<=0)return BadRequest(new{message="Opportunity Amount must be greater than 0."});
        if(m.Probability<0||m.Probability>100)return BadRequest(new{message="Probability must be between 0 and 100."});
        if(m.Status=="Open"&&m.ExpectedCloseDate.Date<DateTime.Today)return BadRequest(new{message="Expected Close Date cannot be in the past."});
        if(!OpportunityStages.All.Contains(m.Stage)||!new[]{"Open","Won","Lost"}.Contains(m.Status))return BadRequest(new{message="Invalid stage or status."});
        var customerQuery=_db.Customers.Where(x=>x.CustomerId==m.CustomerId);
        if(User.IsInRole(Roles.SalesExecutive))customerQuery=customerQuery.Where(x=>x.AssignedTo==User.Identity!.Name||x.CreatedBy==User.Identity!.Name);
        if(!await customerQuery.AnyAsync())return BadRequest(new{message="Customer not found or not accessible."});
        var x=new Opportunity{OpportunityName=m.OpportunityName,CustomerId=m.CustomerId,LeadId=m.LeadId,Amount=m.Amount,Probability=m.Probability,Stage=m.Stage,Status=m.Status,ExpectedCloseDate=m.ExpectedCloseDate,Notes=m.Notes,AssignedTo=User.IsInRole(Roles.SalesExecutive)?User.Identity!.Name:m.AssignedTo,CreatedDate=DateTime.UtcNow};
        _db.Opportunities.Add(x);await _db.SaveChangesAsync();await _audit.LogAsync("Create","Opportunity",x.OpportunityId.ToString(),null,x.OpportunityName);
        return Created($"/api/opportunities/{x.OpportunityId}",new OpportunityDto(x.OpportunityId,x.OpportunityName,x.CustomerId,"",x.Amount,x.Probability,x.Stage,x.Status,x.ExpectedCloseDate,x.WeightedAmount,x.AssignedTo));
    }
    public record OpportunityDto(int Id,string Name,int CustomerId,string CustomerName,decimal Amount,int Probability,string Stage,string Status,DateTime ExpectedCloseDate,decimal WeightedAmount,string? AssignedTo);
    public class OpportunityInput{[Required,StringLength(150)]public string OpportunityName{get;set;}="";[Range(1,int.MaxValue)]public int CustomerId{get;set;}public int? LeadId{get;set;}[Range(typeof(decimal),"0.01","999999999999")]public decimal Amount{get;set;}[Range(0,100)]public int Probability{get;set;}[Required]public string Stage{get;set;}=OpportunityStages.Qualification;[Required]public string Status{get;set;}="Open";public DateTime ExpectedCloseDate{get;set;}[StringLength(1000)]public string? Notes{get;set;}public string? AssignedTo{get;set;}}
}
