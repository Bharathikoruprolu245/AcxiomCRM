using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers.Api;

[ApiController, Route("api/customers"), IgnoreAntiforgeryToken, Authorize]
public class CustomersApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;private readonly IAuditService _audit;
    public CustomersApiController(ApplicationDbContext db,IAuditService audit){_db=db;_audit=audit;}
    private IQueryable<Customer> Scope()=>User.IsInRole(Roles.SalesExecutive)?_db.Customers.Where(x=>x.AssignedTo==User.Identity!.Name||x.CreatedBy==User.Identity!.Name):_db.Customers;
    [HttpGet] public async Task<IActionResult> Get(string? q)
    {
        var d=Scope().AsNoTracking();if(!string.IsNullOrWhiteSpace(q))d=d.Where(x=>x.CustomerName.Contains(q)||x.Email.Contains(q)||x.Phone.Contains(q)||(x.CompanyName??"").Contains(q));
        return Ok(await d.OrderBy(x=>x.CustomerName).Select(x=>new CustomerDto(x.CustomerId,x.CustomerCode,x.CustomerName,x.Email,x.Phone,x.CompanyName,x.Status,x.AssignedTo,x.CreatedDate)).ToListAsync());
    }
    [HttpGet("{id:int}")] public async Task<IActionResult> GetById(int id){var x=await Scope().AsNoTracking().FirstOrDefaultAsync(x=>x.CustomerId==id);return x==null?NotFound():Ok(ToDto(x));}
    [HttpPost,Authorize(Roles="Admin,Manager,SalesExecutive")]
    public async Task<IActionResult> Create(CustomerInput m)
    {
        if(await _db.Customers.AnyAsync(x=>x.Email==m.Email||x.Phone==m.Phone))return Conflict(new{message="A customer with this email or phone already exists."});
        var x=new Customer{CustomerCode="CUS-"+Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),CustomerName=m.CustomerName,Email=m.Email,Phone=m.Phone,CompanyName=m.CompanyName,Address=m.Address,City=m.City,State=m.State,Notes=m.Notes,AssignedTo=User.IsInRole(Roles.SalesExecutive)?User.Identity!.Name:m.AssignedTo,CreatedBy=User.Identity!.Name,CreatedDate=DateTime.UtcNow};
        _db.Customers.Add(x);await _db.SaveChangesAsync();await _audit.LogAsync("Create","Customer",x.CustomerId.ToString(),null,x.CustomerName);
        return CreatedAtAction(nameof(GetById),new{id=x.CustomerId},ToDto(x));
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id,CustomerInput m)
    {
        var x=await Scope().FirstOrDefaultAsync(x=>x.CustomerId==id);if(x==null)return NotFound();
        if(await _db.Customers.AnyAsync(c=>c.CustomerId!=id&&(c.Email==m.Email||c.Phone==m.Phone)))return Conflict(new{message="A customer with this email or phone already exists."});
        var old=x.CustomerName;x.CustomerName=m.CustomerName;x.Email=m.Email;x.Phone=m.Phone;x.CompanyName=m.CompanyName;x.Address=m.Address;x.City=m.City;x.State=m.State;x.Notes=m.Notes;
        if(!User.IsInRole(Roles.SalesExecutive))x.AssignedTo=m.AssignedTo;x.ModifiedDate=DateTime.UtcNow;
        await _db.SaveChangesAsync();await _audit.LogAsync("Update","Customer",id.ToString(),old,x.CustomerName);return Ok(ToDto(x));
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id){var x=await Scope().FirstOrDefaultAsync(x=>x.CustomerId==id);if(x==null)return NotFound();x.Status="Inactive";x.ModifiedDate=DateTime.UtcNow;await _db.SaveChangesAsync();await _audit.LogAsync("Deactivate","Customer",id.ToString(),"Active","Inactive");return NoContent();}
    private static CustomerDto ToDto(Customer x)=>new(x.CustomerId,x.CustomerCode,x.CustomerName,x.Email,x.Phone,x.CompanyName,x.Status,x.AssignedTo,x.CreatedDate);
    public record CustomerDto(int Id,string Code,string Name,string Email,string Phone,string? Company,string Status,string? AssignedTo,DateTime CreatedDate);
    public class CustomerInput
    {
        [Required, StringLength(100)] public string CustomerName{get;set;}="";
        [Required,EmailAddress,StringLength(150)] public string Email{get;set;}="";
        [Required,RegularExpression(@"^[6-9]\d{9}$")] public string Phone{get;set;}="";
        [StringLength(100)] public string? CompanyName{get;set;} [StringLength(250)] public string? Address{get;set;}
        [StringLength(60)] public string? City{get;set;} [StringLength(60)] public string? State{get;set;}
        [StringLength(1000)] public string? Notes{get;set;} public string? AssignedTo{get;set;}
    }
}
