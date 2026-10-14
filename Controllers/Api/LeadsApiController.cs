using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers.Api;

[ApiController,Route("api/leads"),IgnoreAntiforgeryToken,Authorize]
public class LeadsApiController:ControllerBase
{
    private readonly ApplicationDbContext _db;private readonly IAuditService _audit;
    public LeadsApiController(ApplicationDbContext db,IAuditService audit){_db=db;_audit=audit;}
    private IQueryable<Lead> Scope()=>User.IsInRole(Roles.SalesExecutive)?_db.Leads.Where(x=>x.AssignedTo==User.Identity!.Name):_db.Leads;
    [HttpGet] public async Task<IActionResult> Get(string? q,string? status){var d=Scope().AsNoTracking();if(!string.IsNullOrWhiteSpace(q))d=d.Where(x=>x.LeadName.Contains(q)||(x.CompanyName??"").Contains(q));if(!string.IsNullOrWhiteSpace(status))d=d.Where(x=>x.Status==status);return Ok(await d.Select(x=>new LeadDto(x.LeadId,x.LeadCode,x.LeadName,x.Email,x.Phone,x.CompanyName,x.Source,x.Status,x.ExpectedValue,x.AssignedTo)).ToListAsync());}
    [HttpPost] public async Task<IActionResult> Create(LeadInput m)
    {
        if(!LeadStatuses.All.Contains(m.Status))return BadRequest(new{message="Invalid lead status."});
        var x=new Lead{LeadCode="LEAD-"+Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),LeadName=m.LeadName,Email=m.Email,Phone=m.Phone,CompanyName=m.CompanyName,Source=m.Source,Status=m.Status,Priority=m.Priority,ExpectedValue=m.ExpectedValue,Notes=m.Notes,AssignedTo=User.IsInRole(Roles.SalesExecutive)?User.Identity!.Name:m.AssignedTo,CreatedDate=DateTime.UtcNow};
        _db.Leads.Add(x);await _db.SaveChangesAsync();await _audit.LogAsync("Create","Lead",x.LeadId.ToString(),null,x.LeadName);
        return Created($"/api/leads/{x.LeadId}",new LeadDto(x.LeadId,x.LeadCode,x.LeadName,x.Email,x.Phone,x.CompanyName,x.Source,x.Status,x.ExpectedValue,x.AssignedTo));
    }
    public record LeadDto(int Id,string Code,string Name,string Email,string Phone,string? Company,string? Source,string Status,decimal ExpectedValue,string? AssignedTo);
    public class LeadInput{[Required,StringLength(100)]public string LeadName{get;set;}="";[Required,EmailAddress]public string Email{get;set;}="";[Required,RegularExpression(@"^[6-9]\d{9}$")]public string Phone{get;set;}="";public string? CompanyName{get;set;}public string? Source{get;set;}[Required]public string Status{get;set;}=LeadStatuses.New;public string Priority{get;set;}="Medium";[Range(0,100000000)]public decimal ExpectedValue{get;set;}public string? Notes{get;set;}public string? AssignedTo{get;set;}}
}
