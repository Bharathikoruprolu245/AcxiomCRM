# AcxiomCRM

**AcxiomCRM** is a Customer Relationship Management (CRM) application built with ASP.NET Core MVC and .NET 8. It provides functionality for managing customers, leads, sales opportunities, follow-ups, activities, reports, and user access through role-based authorization.

## Technology Stack

- **Backend:** ASP.NET Core MVC (.NET 8), C#
- **ORM:** Entity Framework Core
- **Database:** SQL Server LocalDB
- **Authentication and Authorization:** ASP.NET Core Identity
- **Frontend:** HTML, CSS, Bootstrap 5, jQuery
- **Client-Side Validation:** jQuery Unobtrusive Validation
- **Data Visualization:** Chart.js
- **API:** REST-style JSON endpoints

## Features and Implemented Modules

### 1. Authentication and Security
- User registration, login, and logout.
- Password policy and account lockout.
- Role initialization and role-based access control.
- Audit logging for application activities.

### 2. Customer Management
- Create, view, update, and manage customer records.
- Search and filter customers.
- Prevent duplicate email addresses and phone numbers.
- Deactivate customer records.
- Restrict Sales Executive access according to record ownership and assignment rules.

### 3. Lead Management
- Create, view, update, search, and filter leads.
- Manage lead statuses and configured status transitions.
- Convert qualified leads into customer records.
- Create an associated opportunity during conversion when the expected value is positive.

### 4. Opportunity Management
- Create and manage sales opportunities.
- Track opportunity stages and statuses.
- Validate amounts, probabilities, and close dates.
- Calculate weighted pipeline values.

### 5. Follow-Up Management
- Schedule and manage follow-ups.
- Filter, reschedule, and complete follow-ups.
- Display overdue follow-ups.
- Apply date validation rules.

### 6. Activity Management
- Create, update, and manage CRM activities.
- Filter activities and validate activity types and statuses.

### 7. Dashboard and Analytics
- Display eight key performance indicator (KPI) cards.
- Visualize CRM metrics using three Chart.js charts.
- Apply role-based data scoping to dashboard information.

### 8. User and Role Administration
- Create and edit users.
- Activate or deactivate user accounts.
- Assign roles and reset passwords.
- Restrict administrative operations to authorized users.

### 9. Audit Logs and Reports
- View audit logs with filters for user, module, action, and date.
- Generate reports covering the sales pipeline, customers, leads and conversions, opportunities, and audit activity.

### 10. JSON API Endpoints
The application provides authenticated JSON endpoints for customer, lead, and opportunity operations.

- Customer API: `/api/customers`
- Lead API: `/api/leads`
- Opportunity API: `/api/opportunities`

The application also supports `/AuditLog` as an alias for `/AuditLogs`.

## Role-Based Access Control

The application defines three primary roles:

- **Sales Executive:** Access to assigned CRM records based on the signed-in user's email address. Customer records created by that user are also visible to them.
- **Manager:** Access to team CRM records and reports, without access to user and role administration or the audit log viewer.
- **Admin:** Full CRM and administrative access, including user management, role assignment, and audit log access.

## Prerequisites

Before running the application, ensure that you have:

- Windows with SQL Server LocalDB installed.
- The .NET 8 SDK.
- Visual Studio Code or another compatible code editor.
- A terminal such as PowerShell.

Verify your .NET SDK installation:

```powershell
dotnet --list-sdks
```

Confirm that an SDK version beginning with `8.0` is listed.

## Getting Started

### 1. Clone the repository

```powershell
git clone https://github.com/Bharathikoruprolu245/AcxiomCRM.git
cd AcxiomCRM
```

### 2. Configure the development environment

Set the ASP.NET Core environment to Development in PowerShell:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
```

Configure the database connection string and any required seed credentials using local development configuration or .NET User Secrets. Do not commit passwords, tokens, or other secrets to the repository.

If the application requires a seed password, configure it locally using the configuration key expected by the application:

```powershell
dotnet user-secrets init
dotnet user-secrets set "Seed:DefaultPassword" "YourStrongLocalPassword"
```

Replace `YourStrongLocalPassword` with a strong password of your choice. Keep it private.

### 3. Restore dependencies

```powershell
dotnet restore
```

### 4. Build the application

```powershell
dotnet build
```

Resolve any build errors before proceeding.

### 5. Run the application

```powershell
dotnet run
```

Open the local URL printed in the terminal.

The application is configured to apply pending Entity Framework Core migrations at startup. Ensure that the database connection is valid and that the required migrations are available.

If the database schema needs to be updated, review the existing migrations before creating a new migration. For example:

```powershell
dotnet ef migrations add CRMValidationUpdates
```

Only run this command when a new migration is actually required.

## API Usage Examples

The following examples illustrate the available endpoint patterns:

| Method | Endpoint | Purpose |
|---|---|---|
| GET | `/api/customers?q=acme` | Search customers |
| GET | `/api/customers/1` | Retrieve a customer |
| POST | `/api/customers` | Create a customer |
| PUT | `/api/customers/1` | Update a customer |
| DELETE | `/api/customers/1` | Deactivate a customer |
| GET | `/api/leads?q=website&status=Qualified` | Search and filter leads |
| POST | `/api/leads` | Create a lead |
| GET | `/api/opportunities?stage=Proposal` | Filter opportunities |
| POST | `/api/opportunities` | Create an opportunity |

API requests must satisfy the application's authentication, authorization, and DTO validation requirements. For POST and PUT requests, provide a valid JSON body matching the relevant DTO.

The API uses the authenticated Identity cookie as described in the application configuration. Use an authenticated client when accessing protected endpoints.

## Security Considerations

- Never commit credentials, password hashes, security tokens, or private configuration values.
- Use .NET User Secrets for local development and an appropriate secret-management solution for production.
- Use secure authentication and authorization settings in deployed environments.
- Review configuration and access permissions before deployment.

## Testing and Validation

Before submission or deployment, verify the application in the target Windows environment.

Recommended validation includes:

- Authentication and account lockout.
- Role-based authorization and record scoping.
- Customer, lead, opportunity, follow-up, and activity workflows.
- Input validation and status transitions.
- Dashboard metrics, reports, and charts.
- API responses, authorization, and error handling.
- Database migrations and application startup.
- The complete 14-step acceptance scenario specified in the assignment.

## Project Status

The repository contains the application source code and project configuration. Build, runtime, and acceptance-test results should be confirmed in the target environment before the application is considered ready for submission.