using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers.Api;

[ApiController,Route("api/followups"),IgnoreAntiforgeryToken,Authorize]
public class FollowUpsApiController:ControllerBase
{
    private readonly ApplicationDbContext _db;private readonly IAuditService _audit;
    public FollowUpsApiController(ApplicationDbContext db,IAuditService audit){_db=db;_audit=audit;}
    private IQueryable<FollowUp> Scope()=>User.IsInRole(Roles.SalesExecutive)?_db.FollowUps.Where(x=>x.AssignedTo==User.Identity!.Name):_db.FollowUps;
    [HttpGet] public async Task<IActionResult> Get(string? status,DateTime? from,DateTime? to)
    {
        var d=Scope().AsNoTracking();if(!string.IsNullOrWhiteSpace(status))d=d.Where(x=>x.Status==status);if(from.HasValue)d=d.Where(x=>x.FollowUpDate>=from.Value);if(to.HasValue)d=d.Where(x=>x.FollowUpDate<to.Value.Date.AddDays(1));
        return Ok(await d.OrderBy(x=>x.FollowUpDate).Select(x=>new FollowUpDto(x.FollowUpId,x.CustomerId,x.LeadId,x.OpportunityId,x.FollowUpDate,x.Subject,x.FollowUpType,x.Status,x.AssignedTo)).ToListAsync());
    }
    [HttpPost] public async Task<IActionResult> Create(FollowUpInput m)
    {
        if(m.FollowUpDate.Date<DateTime.Today)return BadRequest(new{message="Follow-up date cannot be earlier than today."});
        var targets=(m.CustomerId.HasValue?1:0)+(m.LeadId.HasValue?1:0)+(m.OpportunityId.HasValue?1:0);if(targets!=1)return BadRequest(new{message="Choose exactly one related customer, lead, or opportunity."});
        if(m.CustomerId.HasValue)
        {
            var q=_db.Customers.Where(x=>x.CustomerId==m.CustomerId.Value);
            if(User.IsInRole(Roles.SalesExecutive))q=q.Where(x=>x.AssignedTo==User.Identity!.Name||x.CreatedBy==User.Identity!.Name);
            if(!await q.AnyAsync())return BadRequest(new{message="Customer not found or not accessible."});
        }
        if(m.LeadId.HasValue)
        {
            var q=_db.Leads.Where(x=>x.LeadId==m.LeadId.Value);
            if(User.IsInRole(Roles.SalesExecutive))q=q.Where(x=>x.AssignedTo==User.Identity!.Name);
            if(!await q.AnyAsync())return BadRequest(new{message="Lead not found or not accessible."});
        }
        if(m.OpportunityId.HasValue)
        {
            var q=_db.Opportunities.Where(x=>x.OpportunityId==m.OpportunityId.Value);
            if(User.IsInRole(Roles.SalesExecutive))q=q.Where(x=>x.AssignedTo==User.Identity!.Name);
            if(!await q.AnyAsync())return BadRequest(new{message="Opportunity not found or not accessible."});
        }
        var x=new FollowUp{CustomerId=m.CustomerId,LeadId=m.LeadId,OpportunityId=m.OpportunityId,FollowUpDate=m.FollowUpDate,Subject=m.Subject,FollowUpType=m.FollowUpType,Remarks=m.Remarks,Status="Planned",AssignedTo=User.IsInRole(Roles.SalesExecutive)?User.Identity!.Name:m.AssignedTo};
        _db.FollowUps.Add(x);await _db.SaveChangesAsync();await _audit.LogAsync("Create","FollowUp",x.FollowUpId.ToString(),null,x.Subject);
        return Created($"/api/followups/{x.FollowUpId}",new FollowUpDto(x.FollowUpId,x.CustomerId,x.LeadId,x.OpportunityId,x.FollowUpDate,x.Subject,x.FollowUpType,x.Status,x.AssignedTo));
    }
    public record FollowUpDto(int Id,int? CustomerId,int? LeadId,int? OpportunityId,DateTime Date,string? Subject,string Type,string Status,string? AssignedTo);
    public class FollowUpInput{public int? CustomerId{get;set;}public int? LeadId{get;set;}public int? OpportunityId{get;set;}[Required]public DateTime FollowUpDate{get;set;}[StringLength(150)]public string? Subject{get;set;}[Required,StringLength(30)]public string FollowUpType{get;set;}="Call";[StringLength(1000)]public string? Remarks{get;set;}public string? AssignedTo{get;set;}}
}
