using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class CustomersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    public CustomersController(ApplicationDbContext db, IAuditService audit) { _db = db; _audit = audit; }

    private IQueryable<Customer> Scoped()
    {
        var q = _db.Customers.AsQueryable();
        if (User.IsInRole(Roles.SalesExecutive)) q = q.Where(x => x.AssignedTo == User.Identity!.Name || x.CreatedBy == User.Identity!.Name);
        return q;
    }

    public async Task<IActionResult> Index(string? q, string? status, int page = 1)
    {
        var data = Scoped();
        if (!string.IsNullOrWhiteSpace(q))
            data = data.Where(x => x.CustomerName.Contains(q) || x.Email.Contains(q) || x.Phone.Contains(q) || (x.CompanyName ?? "").Contains(q));
        if (!string.IsNullOrWhiteSpace(status)) data = data.Where(x => x.Status == status);
        ViewBag.Query = q; ViewBag.Status = status; ViewBag.Page = page;
        return View(await data.OrderByDescending(x => x.CreatedDate).Skip((Math.Max(page,1)-1)*20).Take(20).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var item = await Scoped().FirstOrDefaultAsync(x => x.CustomerId == id);
        return item == null ? NotFound() : View(item);
    }

    [Authorize(Roles = "Admin,Manager,SalesExecutive")]
    public IActionResult Create() => View(new Customer { CustomerCode = "CUS-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(), AssignedTo = User.Identity!.Name });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer model)
    {
        model.CustomerId = 0;
        if (User.IsInRole(Roles.SalesExecutive)) model.AssignedTo = User.Identity!.Name;
        model.CreatedBy = User.Identity!.Name; model.CreatedDate = DateTime.UtcNow;
        await ValidateDuplicates(model, null);
        if (!ModelState.IsValid) return View(model);
        _db.Customers.Add(model);
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException) { ModelState.AddModelError("", "A customer with this email, phone, or code already exists."); return View(model); }
        await _audit.LogAsync("Create", "Customer", model.CustomerId.ToString(), null, model.CustomerName);
        TempData["Success"] = "Customer created successfully."; return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await Scoped().FirstOrDefaultAsync(x => x.CustomerId == id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Customer model)
    {
        var item = await Scoped().FirstOrDefaultAsync(x => x.CustomerId == id);
        if (item == null) return NotFound();
        await ValidateDuplicates(model, id);
        if (!ModelState.IsValid) return View(model);
        var old = $"{item.CustomerName}; {item.Email}; {item.Phone}; {item.Status}";
        item.CustomerName=model.CustomerName; item.Email=model.Email; item.Phone=model.Phone; item.CompanyName=model.CompanyName;
        item.Address=model.Address; item.City=model.City; item.State=model.State; item.Status=model.Status; item.Notes=model.Notes;
        if (!User.IsInRole(Roles.SalesExecutive)) item.AssignedTo=model.AssignedTo;
        item.ModifiedDate=DateTime.UtcNow;
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException) { ModelState.AddModelError("", "A customer with this email or phone already exists."); return View(item); }
        await _audit.LogAsync("Update", "Customer", id.ToString(), old, $"{item.CustomerName}; {item.Email}; {item.Phone}; {item.Status}");
        TempData["Success"]="Customer updated."; return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await Scoped().FirstOrDefaultAsync(x => x.CustomerId == id);
        if (item == null) return NotFound();
        item.Status = "Inactive"; item.ModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Deactivate", "Customer", id.ToString(), "Active", "Inactive");
        TempData["Success"]="Customer deactivated."; return RedirectToAction(nameof(Index));
    }

    private async Task ValidateDuplicates(Customer m, int? id)
    {
        var q = _db.Customers.Where(x => !id.HasValue || x.CustomerId != id.Value);
        if (!string.IsNullOrWhiteSpace(m.Email) && await q.AnyAsync(x => x.Email.ToLower() == m.Email.ToLower())) ModelState.AddModelError(nameof(m.Email), "This email is already used by another customer.");
        if (!string.IsNullOrWhiteSpace(m.Phone) && await q.AnyAsync(x => x.Phone == m.Phone)) ModelState.AddModelError(nameof(m.Phone), "This phone number is already used by another customer.");
        if (!string.IsNullOrWhiteSpace(m.CustomerCode) && await q.AnyAsync(x => x.CustomerCode == m.CustomerCode)) ModelState.AddModelError(nameof(m.CustomerCode), "Customer code already exists.");
    }
}
