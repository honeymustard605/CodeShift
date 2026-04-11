# CodeShift

Legacy .NET modernization platform. Analyzes C#, VB.NET, and VB6 codebases to produce dependency graphs, health scores, migration roadmaps, and automated transformation suggestions.

## Quick Start

```bash
# Start PostgreSQL
docker-compose up db -d

# Run the API (from src/api/)
dotnet run --project CodeShift.Api

# Run the frontend (from src/web/)
npm install && npm run dev
```

## Architecture

```
codeshift/
├── src/api/          # .NET 8 minimal API (Api, Core, Data, Tests)
├── src/web/          # React + Vite + TypeScript + Tailwind
├── infra/            # Terraform (Azure)
└── test-fixtures/    # Sample legacy codebases for manual/integration testing
```

### Backend Projects

| Project | Purpose |
|---|---|
| `CodeShift.Api` | Minimal API endpoints, DI wiring, HTTP concerns |
| `CodeShift.Core` | Analyzers, services, domain models — no infrastructure deps |
| `CodeShift.Data` | EF Core DbContext, entities, migrations |
| `CodeShift.Tests` | xUnit tests for analyzers and services |

### Frontend Pages

| Route | Page |
|---|---|
| `/` | Home / upload |
| `/dashboard/:id` | Analysis dashboard |
| `/graph/:id` | Dependency graph |
| `/roadmap/:id` | Migration roadmap |
| `/transform/:id` | Code transformation diff viewer |

## Development

### Prerequisites
- .NET 8 SDK
- Node 20+
- Docker (for PostgreSQL)

### Database Migrations

```bash
cd src/api
dotnet ef migrations add InitialCreate --project CodeShift.Data --startup-project CodeShift.Api
dotnet ef database update --project CodeShift.Data --startup-project CodeShift.Api
```

### Running Tests

```bash
cd src/api
dotnet test
```

## Infrastructure

Terraform configuration targets Azure (App Service + Azure Database for PostgreSQL + Blob Storage + Key Vault). See `infra/` for details.
