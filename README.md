# AcxiomCRM

ASP.NET Core MVC (.NET 8), Entity Framework Core, SQL Server LocalDB, ASP.NET Core Identity, Bootstrap 5, jQuery unobtrusive validation, and Chart.js.

## Implemented modules

- Identity login/register/logout, password policy, account lockout, role seeding, audit logging.
- Customer CRUD, search/filter, duplicate email/phone prevention, deactivate, assigned-record scope for SalesExecutive.
- Lead CRUD, search/filter, configured status transitions, qualified-lead conversion to Customer and (when expected value is positive) Opportunity.
- Opportunity CRUD, stage/status handling, amount/probability/close-date validation and weighted pipeline.
- Follow-up scheduling, filters, rescheduling, completion, overdue display and date validation.
- Activity CRUD, filters, type/status validation.
- Role-scoped dashboard with eight KPI cards and three Chart.js charts.
- Admin-only user creation/editing/activation, role assignment and password reset.
- Admin-only audit log viewer with user/module/action/date filters.
- Reports for pipeline, customers, leads/conversions, opportunities and audit activity.
- Secured JSON APIs: `/api/customers`, `/api/customers/{id}`, `/api/leads`, `/api/opportunities`.
- `/AuditLog` is an alias for `/AuditLogs`.

## Run locally (Windows)

1. Install the .NET 8 SDK and ensure `dotnet --list-sdks` lists an 8.0 SDK.
2. Open this folder in VS Code or a terminal.
3. Set Development mode for local HTTP testing:
   ```powershell
   $env:ASPNETCORE_ENVIRONMENT = "Development"
   ```
4. Restore packages and build:
   ```powershell
   dotnet restore
   dotnet build
   ```
5. The initial migration is included. Since the entity phone validation metadata was tightened, optionally create a schema migration for the phone column lengths:
   ```powershell
   dotnet ef migrations add CRMValidationUpdates
   ```
   The app calls `Database.MigrateAsync()` at startup and will apply pending migrations automatically.
6. Run:
   ```powershell
   dotnet run
   ```
   Use the HTTP/HTTPS URL printed in the terminal.

## Demo accounts

- `admin@acxiomcrm.com`
- `manager@acxiomcrm.com`
- `sales@acxiomcrm.com`

The demo seed password is `Acxiom@12345` unless changed in configuration. Change it before deployment. The seed password in `appsettings.json` is for local evaluation only; production secrets should be supplied through environment variables or user-secrets.

## API examples

The API uses the current authenticated Identity cookie. For POST/PUT JSON requests, use an authenticated client; API controllers ignore MVC antiforgery tokens and still enforce authentication, authorization, and DTO validation.

- `GET /api/customers?q=acme`
- `GET /api/customers/1`
- `POST /api/customers`
- `PUT /api/customers/1`
- `DELETE /api/customers/1` (deactivates)
- `GET /api/leads?q=website&status=Qualified`
- `POST /api/leads`
- `GET /api/opportunities?stage=Proposal`
- `POST /api/opportunities`

## Notes

- `SalesExecutive` scope is based on `AssignedTo` matching the signed-in user's email; customer records created by that user are also visible to them.
- `Manager` can access team CRM records and reports but not user/role administration or the audit viewer.
- `Admin` has full CRM and administration access.
- Do not expose the demo password, connection strings, password hashes, or security tokens in a public repository.
- The project still needs a full local build and acceptance test on the target Windows machine. Test the 14-step acceptance scenario in the assignment before submission.
