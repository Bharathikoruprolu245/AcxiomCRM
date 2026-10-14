using AcxiomCRM.Data;
using AcxiomCRM.Models;
namespace AcxiomCRM.Services;

public interface IAuditService
{
    Task LogAsync(string action, string entity, string? recordId = null, string? oldValue = null,
                  string? newValue = null, string result = "Success", string? userId = null, string? userName = null);
}

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;
    public AuditService(ApplicationDbContext db, IHttpContextAccessor http) { _db = db; _http = http; }

    public async Task LogAsync(string action, string entity, string? recordId = null, string? oldValue = null,
        string? newValue = null, string result = "Success", string? userId = null, string? userName = null)
    {
        var ctx = _http.HttpContext;
        // NEVER pass passwords/hashes/tokens in old/new values.
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId ?? ctx?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            UserName = userName ?? ctx?.User.Identity?.Name,
            Action = action, EntityName = entity, RecordId = recordId,
            OldValue = oldValue, NewValue = newValue, Result = result,
            IpAddress = ctx?.Connection.RemoteIpAddress?.ToString()
        });
        await _db.SaveChangesAsync();
    }
}
