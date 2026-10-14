using System.ComponentModel.DataAnnotations;
namespace AcxiomCRM.Models;

public static class Phone { public const string Pattern = @"^[6-9]\d{9}$"; public const string Message = "Enter a valid 10-digit Indian mobile number."; }

public static class LeadStatuses
{
    public const string New = "New", Contacted = "Contacted", Qualified = "Qualified",
        Unqualified = "Unqualified", Converted = "Converted", Lost = "Lost";
    public static readonly string[] All = { New, Contacted, Qualified, Unqualified, Converted, Lost };
    // Allowed workflow transitions (spec 4.5: prevent invalid transitions)
    public static readonly Dictionary<string, string[]> Transitions = new()
    {
        [New] = new[] { Contacted, Lost },
        [Contacted] = new[] { Qualified, Unqualified, Lost },
        [Qualified] = new[] { Converted, Lost },
        [Unqualified] = new[] { Contacted, Lost },
        [Converted] = Array.Empty<string>(),
        [Lost] = Array.Empty<string>()
    };
}
public static class OpportunityStages
{
    public const string Qualification = "Qualification", Proposal = "Proposal", Negotiation = "Negotiation", Won = "Won", Lost = "Lost";
    public static readonly string[] All = { Qualification, Proposal, Negotiation, Won, Lost };
}
public static class FollowUpStatuses
{
    public static readonly string[] All = { "Planned", "Completed", "Missed", "Cancelled" };
}
public static class ActivityTypes { public static readonly string[] All = { "Call", "Meeting", "Email", "Task" }; }

public class Customer
{
    public int CustomerId { get; set; }
    [Required, StringLength(20)] public string CustomerCode { get; set; } = string.Empty;
    [Required, StringLength(100)] public string CustomerName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = string.Empty;
    [Required, StringLength(10), RegularExpression(global::AcxiomCRM.Models.Phone.Pattern,
    ErrorMessage = global::AcxiomCRM.Models.Phone.Message)]
    public string Phone { get; set; } = string.Empty;
    [StringLength(100)] public string? CompanyName { get; set; }
    [StringLength(250)] public string? Address { get; set; }
    [StringLength(60)] public string? City { get; set; }
    [StringLength(60)] public string? State { get; set; }
    [Required, StringLength(20)] public string Status { get; set; } = "Active";
    [StringLength(1000)] public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? AssignedTo { get; set; }
}

public class Lead
{
    public int LeadId { get; set; }
    [Required, StringLength(20)] public string LeadCode { get; set; } = string.Empty;
    [Required, StringLength(100)] public string LeadName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = string.Empty;
    [Required, StringLength(10), RegularExpression(global::AcxiomCRM.Models.Phone.Pattern,
    ErrorMessage = global::AcxiomCRM.Models.Phone.Message)]
    public string Phone { get; set; } = string.Empty;
    [StringLength(100)] public string? CompanyName { get; set; }
    [StringLength(50)] public string? Source { get; set; }
    [Required, StringLength(20)] public string Status { get; set; } = LeadStatuses.New;
    [StringLength(20)] public string Priority { get; set; } = "Medium";
    [Range(0, 100000000, ErrorMessage = "Expected value must be between 0 and 100,000,000.")]
    public decimal ExpectedValue { get; set; }
    [StringLength(1000)] public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? AssignedTo { get; set; }
}

public class Opportunity
{
    public int OpportunityId { get; set; }
    [Required, StringLength(150)] public string OpportunityName { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }
    [Range(typeof(decimal), "0.01", "999999999999", ErrorMessage = "Opportunity Amount must be greater than 0.")]
    public decimal Amount { get; set; }
    [Required, StringLength(20)] public string Stage { get; set; } = OpportunityStages.Qualification;
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")] public int Probability { get; set; }
    public DateTime ExpectedCloseDate { get; set; }
    [Required, StringLength(20)] public string Status { get; set; } = "Open"; // Open / Won / Lost
    [StringLength(1000)] public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? AssignedTo { get; set; }
    public decimal WeightedAmount => Amount * Probability / 100m;
}

public class FollowUp
{
    public int FollowUpId { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }
    public int? OpportunityId { get; set; }
    [AcxiomCRM.Validation.NotInPast("Follow-up date cannot be earlier than today.")]
    public DateTime FollowUpDate { get; set; }
    [StringLength(150)] public string? Subject { get; set; }
    [Required, StringLength(30)] public string FollowUpType { get; set; } = "Call";
    [StringLength(1000)] public string? Remarks { get; set; }
    [Required, StringLength(20)] public string Status { get; set; } = "Planned";
    public string? AssignedTo { get; set; }
}

public class Activity
{
    public int ActivityId { get; set; }
    [Required, StringLength(20)] public string ActivityType { get; set; } = "Call";
    [Required, StringLength(150)] public string Subject { get; set; } = string.Empty;
    [StringLength(1000)] public string? Description { get; set; }
    public DateTime ActivityDate { get; set; } = DateTime.Today;
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }
    public string? AssignedTo { get; set; }
    [Required, StringLength(20)] public string Status { get; set; } = "Planned";
}

public class AuditLog
{
    public int AuditLogId { get; set; }
    public string? UserId { get; set; }
    [StringLength(256)] public string? UserName { get; set; }
    [Required, StringLength(50)] public string Action { get; set; } = string.Empty;
    [Required, StringLength(50)] public string EntityName { get; set; } = string.Empty;
    [StringLength(50)] public string? RecordId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    [StringLength(20)] public string Result { get; set; } = "Success";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    [StringLength(60)] public string? IpAddress { get; set; }
}
