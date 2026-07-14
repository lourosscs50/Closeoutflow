# Closeoutflow

Closeoutflow is a modular .NET workflow platform for tracking jobs, collecting closeout proof, and validating project completion through controlled domain transitions.

**Project domain:** closeoutflow.org

## Why Closeoutflow Exists

Field-service, construction, maintenance, and operations teams often complete physical work before the administrative closeout is fully verified. That gap can create delayed billing, missing proof, unclear job status, and weak audit trails.

Closeoutflow models the workflow explicitly:

1. A job is created.
2. The job is started.
3. The job is marked pending closeout.
4. A closeout record is submitted with proof.
5. The closeout is validated.
6. The job is closed only when the closeout succeeds.

Invalid operations fail safely without leaving jobs or closeout records in a partially modified state.

## Current Capabilities

The v1 backend supports:

* Creating jobs with operator-provided titles
* Listing jobs
* Reading jobs by ID
* Starting jobs through controlled lifecycle transitions
* Marking jobs as pending closeout
* Completing closeouts with one or more proof items
* Listing all closeout records
* Reading closeout records by ID
* Listing closeouts associated with a specific job
* SQLite persistence through Entity Framework Core
* Explicit API request and response contracts
* Health and database-readiness endpoints
* Swagger documentation in development
* Domain, handler, persistence, API contract, workflow, and architecture tests
* GitHub Actions validation on pushes and pull requests

## Architecture

Closeoutflow uses a modular architecture with strict dependency boundaries.

```text
Closeoutflow.Api
├── Closeoutflow.Modules.Jobs
├── Closeoutflow.Modules.Closeouts
└── Closeoutflow.Shared

Closeoutflow.Modules.Closeouts
├── Closeoutflow.Modules.Jobs
└── Closeoutflow.Shared

Closeoutflow.Modules.Jobs
└── Closeoutflow.Shared
```

### Solution Layout

```text
src/
  Closeoutflow.Api/
  Closeoutflow.Modules.Jobs/
  Closeoutflow.Modules.Closeouts/
  Closeoutflow.Shared/

tests/
  Closeoutflow.Api.Tests/
  Closeoutflow.Modules.Jobs.Tests/
  Closeoutflow.Modules.Closeouts.Tests/
  Closeoutflow.Architecture.Tests/
```

### Architectural Rules

* `Closeoutflow.Shared` cannot reference feature modules or the API.
* `Closeoutflow.Modules.Jobs` cannot reference the API or Closeouts module.
* `Closeoutflow.Modules.Closeouts` cannot reference the API.
* `Closeoutflow.Api` is the composition and infrastructure layer.
* Domain rules remain inside the appropriate module.
* Persistence implementations satisfy interfaces owned by the domain modules.

These boundaries are protected by automated architecture tests.

## Technology

* .NET 10
* ASP.NET Core Minimal APIs
* Entity Framework Core
* SQLite
* Swagger/OpenAPI
* xUnit
* GitHub Actions

## Job Lifecycle

```text
New
  ↓
InProgress
  ↓
PendingCloseout
  ↓
Closed
```

A job cannot skip required states. Failed lifecycle operations do not mutate valid existing state or overwrite lifecycle timestamps.

## Proof Types

Closeout proof currently supports:

| Value | Type      |
| ----: | --------- |
|   `0` | Note      |
|   `1` | Photo     |
|   `2` | Signature |

The API currently accepts numeric enum values in closeout requests.

## API Endpoints

### Health

| Method | Endpoint        | Purpose                           |
| ------ | --------------- | --------------------------------- |
| `GET`  | `/health`       | Confirm the API is running        |
| `GET`  | `/health/ready` | Confirm the database is reachable |

### Jobs

| Method | Endpoint                           | Purpose                     |
| ------ | ---------------------------------- | --------------------------- |
| `POST` | `/jobs`                            | Create a job                |
| `GET`  | `/jobs`                            | List jobs                   |
| `GET`  | `/jobs/{id}`                       | Read a job                  |
| `POST` | `/jobs/{id}/start`                 | Start a job                 |
| `POST` | `/jobs/{id}/mark-pending-closeout` | Mark a job pending closeout |
| `POST` | `/jobs/{id}/closeout`              | Complete the job closeout   |

### Closeouts

| Method | Endpoint               | Purpose                    |
| ------ | ---------------------- | -------------------------- |
| `GET`  | `/closeouts`           | List closeout records      |
| `GET`  | `/closeouts/{id}`      | Read a closeout record     |
| `GET`  | `/jobs/{id}/closeouts` | List closeouts for one job |

## Example Workflow

Create a job:

```json
{
  "title": "Replace loading dock safety light"
}
```

Start the job:

```text
POST /jobs/{jobId}/start
```

Mark it pending closeout:

```text
POST /jobs/{jobId}/mark-pending-closeout
```

Complete the closeout:

```json
{
  "summary": "Safety light replaced and tested.",
  "proofItems": [
    {
      "type": 1,
      "value": "photo://loading-dock-light"
    }
  ]
}
```

## Getting Started

### Requirements

* .NET 10 SDK
* Git

### Restore and Build

```bash
dotnet restore
dotnet build --no-restore
```

### Run the API

```bash
dotnet run --project src/Closeoutflow.Api
```

The API uses SQLite and creates a local `closeoutflow.db` database when required.

Swagger UI is available in the Development environment at:

```text
/swagger
```

## Testing

Run the complete validation sequence:

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build --verbosity normal
```

The test suite covers:

* Job lifecycle rules and invariants
* Closeout and proof validation
* Failed-state mutation protection
* Application-handler write behavior
* In-memory repository behavior
* EF Core persistence and domain mapping
* API request and response contracts
* End-to-end closeout workflows
* SQLite test database isolation
* Module dependency boundaries
* Health and readiness behavior

The current release includes more than 100 passing automated tests.

## Continuous Integration

GitHub Actions runs the following validation on every push and pull request:

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build --verbosity normal
```

CI uses the .NET 10 SDK.

## Security and Dependency Validation

The release process includes a transitive NuGet vulnerability scan:

```bash
dotnet list package --vulnerable --include-transitive
```

The current dependency set resolves without known vulnerable packages from the configured NuGet sources.

## Project Status

Closeoutflow has reached its initial backend v1 release milestone.

Potential future work includes:

* Authentication and tenant boundaries
* Operator-facing user interface
* File and object-storage integration for proof uploads
* Database migrations and deployment automation
* Audit history and operational observability
* Additional workflow types and approval stages

## Repository

GitHub: `lourosscs50/Closeoutflow`
