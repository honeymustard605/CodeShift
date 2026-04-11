# CodeShift — Mac Development Setup Guide

## The Short Answer

One repo. One VS Code window. Everything lives together in a monorepo. No need for separate workspaces or multiple VS Code instances.

```
codeshift/
├── src/
│   ├── api/                    # .NET 8 backend (single .sln, multiple projects)
│   └── web/                    # React frontend (Vite + TypeScript)
├── infra/                      # Terraform
├── .github/workflows/          # CI/CD
├── test-fixtures/              # Sample legacy solutions for testing
├── docker-compose.yml          # Local dev (API + PostgreSQL + React)
├── Dockerfile.api              # API container
├── .gitignore
└── README.md
```

That's it. One `git clone`, one `code .`, everything in front of you.

---

## Step 0: Install Prerequisites (One Time)

Run these in your terminal:

```bash
# .NET 8 SDK
brew install dotnet@8

# Node.js (for React frontend)
brew install node

# Docker Desktop (for PostgreSQL and containerization)
# Download from https://www.docker.com/products/docker-desktop/

# Terraform (for deployment later)
brew install terraform

# Azure CLI (for deployment later)
brew install azure-cli

# Claude Code
npm install -g @anthropic-ai/claude-code
```

Verify everything:

```bash
dotnet --version      # Should show 8.x.x
node --version        # Should show 20+ or 22+
docker --version      # Should show Docker version
terraform --version   # Should show 1.x
az --version          # Should show azure-cli 2.x
claude --version      # Should show claude-code version
```

---

## Step 1: Create the Monorepo

```bash
mkdir codeshift && cd codeshift
git init
```

---

## Step 2: Scaffold the .NET Backend

The backend is a single .sln with multiple projects. This is standard .NET — not multiple workspaces, just multiple projects inside one solution.

```bash
# Create solution
mkdir -p src/api
cd src/api
dotnet new sln -n CodeShift

# Main API project (the web server)
dotnet new webapi -n CodeShift.Api -o CodeShift.Api --use-minimal-apis
dotnet sln add CodeShift.Api

# Core domain logic (analyzers, services — no web dependencies)
dotnet new classlib -n CodeShift.Core -o CodeShift.Core
dotnet sln add CodeShift.Core

# Data access (EF Core, PostgreSQL)
dotnet new classlib -n CodeShift.Data -o CodeShift.Data
dotnet sln add CodeShift.Data

# Unit tests
dotnet new xunit -n CodeShift.Tests -o CodeShift.Tests
dotnet sln add CodeShift.Tests

# Wire up project references
dotnet add CodeShift.Api reference CodeShift.Core
dotnet add CodeShift.Api reference CodeShift.Data
dotnet add CodeShift.Core reference CodeShift.Data
dotnet add CodeShift.Tests reference CodeShift.Core

cd ../..
```

Your backend structure now looks like:

```
src/api/
├── CodeShift.sln
├── CodeShift.Api/              # Minimal API endpoints, Program.cs, middleware
│   ├── CodeShift.Api.csproj
│   ├── Program.cs
│   └── Endpoints/
├── CodeShift.Core/             # All business logic lives here
│   ├── CodeShift.Core.csproj
│   ├── Analyzers/
│   │   ├── CSharpAnalyzer.cs
│   │   ├── VbNetAnalyzer.cs
│   │   └── Vb6Analyzer.cs
│   ├── Services/
│   │   ├── DependencyGraphBuilder.cs
│   │   ├── HealthScoreCalculator.cs
│   │   ├── RoadmapGenerator.cs
│   │   └── TransformEngine.cs
│   └── Models/
│       ├── AnalysisResult.cs
│       ├── DetectedProject.cs
│       ├── DependencyEdge.cs
│       └── MigrationRoadmap.cs
├── CodeShift.Data/             # EF Core DbContext, migrations, repositories
│   ├── CodeShift.Data.csproj
│   ├── CodeShiftDbContext.cs
│   └── Migrations/
└── CodeShift.Tests/            # Unit tests for analyzers
    ├── CodeShift.Tests.csproj
    └── Analyzers/
        ├── CSharpAnalyzerTests.cs
        ├── VbNetAnalyzerTests.cs
        └── Vb6AnalyzerTests.cs
```

**Why multiple projects instead of one?** Separation of concerns. The Api project handles HTTP. Core has zero web dependencies — it's pure business logic. Data handles persistence. Tests reference Core directly. This is the structure interviewers expect to see.

---

## Step 3: Add NuGet Packages

```bash
cd src/api

# API project
dotnet add CodeShift.Api package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add CodeShift.Api package Swashbuckle.AspNetCore

# Core project
dotnet add CodeShift.Core package Anthropic.SDK    # Claude API client
# Or use raw HttpClient — check latest Anthropic .NET SDK availability

# Data project
dotnet add CodeShift.Data package Microsoft.EntityFrameworkCore
dotnet add CodeShift.Data package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add CodeShift.Data package Microsoft.EntityFrameworkCore.Design

# Test project
dotnet add CodeShift.Tests package Moq
dotnet add CodeShift.Tests package FluentAssertions

cd ../..
```

---

## Step 4: Scaffold the React Frontend

```bash
cd src
npm create vite@latest web -- --template react-ts
cd web
npm install
npm install -D tailwindcss @tailwindcss/vite

# Key dependencies
npm install react-force-graph-2d    # Dependency graph visualization
npm install recharts                # Charts for dashboard
npm install react-router-dom        # Routing
npm install axios                   # API calls
npm install react-dropzone          # File upload drag-and-drop
npm install @radix-ui/react-dialog @radix-ui/react-tabs  # UI components (or use shadcn)

cd ../..
```

Your frontend structure:

```
src/web/
├── package.json
├── vite.config.ts
├── tsconfig.json
├── index.html
├── src/
│   ├── main.tsx
│   ├── App.tsx
│   ├── api/
│   │   └── client.ts            # Axios instance, API calls
│   ├── components/
│   │   ├── FileUpload.tsx        # Drag-and-drop ZIP upload
│   │   ├── HealthScore.tsx       # Big score badge
│   │   ├── DependencyGraph.tsx   # Force-directed graph
│   │   ├── TechBreakdown.tsx     # Technology cards
│   │   ├── RiskFlags.tsx         # Risk table
│   │   ├── RoadmapTimeline.tsx   # Phased migration plan
│   │   └── DiffViewer.tsx        # Side-by-side code diff
│   ├── pages/
│   │   ├── HomePage.tsx          # Upload + project list
│   │   ├── DashboardPage.tsx     # Analysis results
│   │   ├── GraphPage.tsx         # Full-screen dependency graph
│   │   ├── RoadmapPage.tsx       # AI-generated migration plan
│   │   └── TransformPage.tsx     # Code transform review
│   └── types/
│       └── index.ts              # TypeScript interfaces matching API models
└── public/
```

---

## Step 5: Docker Compose for Local Development

This is how you run everything locally with a single command.

Create `docker-compose.yml` in the repo root:

```yaml
services:
  db:
    image: postgres:16
    environment:
      POSTGRES_DB: codeshift
      POSTGRES_USER: codeshift
      POSTGRES_PASSWORD: localdev
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data

volumes:
  pgdata:
```

For local dev, you run PostgreSQL in Docker but run the API and React app directly on your Mac (faster hot-reload, easier debugging):

```bash
# Terminal 1: Start PostgreSQL
docker-compose up db

# Terminal 2: Run .NET API
cd src/api/CodeShift.Api
dotnet run

# Terminal 3: Run React dev server
cd src/web
npm run dev
```

The API runs on http://localhost:5000 (or whatever port .NET assigns).
The React app runs on http://localhost:5173 (Vite default).

Add a proxy to your Vite config so the React app can call the API without CORS issues:

```typescript
// src/web/vite.config.ts
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5079',  // Your .NET API port
        changeOrigin: true,
      }
    }
  }
})
```

---

## Step 6: Test Fixtures (Sample Legacy Projects)

Create sample legacy solutions that your analyzers will process during development and testing:

```bash
mkdir -p test-fixtures

mkdir -p test-fixtures/webforms-sample
mkdir -p test-fixtures/wcf-sample
mkdir -p test-fixtures/vb6-sample
mkdir -p test-fixtures/mixed-solution
```

You will manually create small, representative legacy project files in these directories. They do not need to compile — your analyzer reads the file contents, it does not build them. For example:

**test-fixtures/webforms-sample/WebFormsApp.csproj** — a .NET Framework 4.8 .csproj with System.Web references
**test-fixtures/webforms-sample/Default.aspx.cs** — a code-behind file with Page_Load
**test-fixtures/vb6-sample/Project1.vbp** — a VB6 project file with COM references
**test-fixtures/vb6-sample/frmMain.frm** — a VB6 form with controls and event handlers

---

## Step 7: .gitignore

```bash
cat > .gitignore << 'EOF'
# .NET
**/bin/
**/obj/
*.user
*.suo

# Node
src/web/node_modules/
src/web/dist/

# IDE
.vs/
.vscode/settings.json
.idea/

# Environment
*.env
appsettings.Development.json

# Terraform
infra/.terraform/
infra/*.tfstate
infra/*.tfstate.backup
infra/*.tfvars

# OS
.DS_Store
Thumbs.db

# Uploads (temp files from analysis)
uploads/
temp/
EOF
```

---

## Step 8: VS Code Setup

### Recommended Extensions

- **C# Dev Kit** (Microsoft) — .NET development, IntelliSense, debugging
- **ESLint** — TypeScript/React linting
- **Tailwind CSS IntelliSense** — autocomplete for Tailwind classes
- **Prettier** — code formatting
- **HashiCorp Terraform** — Terraform syntax highlighting
- **Docker** — Docker file support
- **GitLens** — Git history visualization

### VS Code Workspace Settings

Create `.vscode/launch.json` for debugging:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Launch API",
      "type": "coreclr",
      "request": "launch",
      "program": "${workspaceFolder}/src/api/CodeShift.Api/bin/Debug/net8.0/CodeShift.Api.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/api/CodeShift.Api",
      "stopAtEntry": false,
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ConnectionStrings__Default": "Host=localhost;Database=codeshift;Username=codeshift;Password=localdev"
      }
    }
  ]
}
```

Create `.vscode/tasks.json` for build tasks:

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "build-api",
      "command": "dotnet",
      "args": ["build", "src/api/CodeShift.sln"],
      "type": "process",
      "problemMatcher": "$msCompile"
    },
    {
      "label": "test-api",
      "command": "dotnet",
      "args": ["test", "src/api/CodeShift.sln"],
      "type": "process",
      "problemMatcher": "$msCompile"
    },
    {
      "label": "dev-web",
      "command": "npm",
      "args": ["run", "dev"],
      "type": "shell",
      "options": { "cwd": "${workspaceFolder}/src/web" },
      "isBackground": true
    }
  ]
}
```

---

## Using Claude Code

Claude Code works great in a monorepo. You run it from the repo root and it can see everything:

```bash
cd codeshift
claude
```

Claude Code will have context of the entire repo structure. Some tips:

**Give it context about what you're working on:**
- "I'm working on the CSharpAnalyzer in src/api/CodeShift.Core/Analyzers/"
- "Help me build the DependencyGraph component in src/web/src/components/"
- "Write unit tests for the VB6 parser against the test fixture in test-fixtures/vb6-sample/"

**Use it for the boring parts:**
- "Generate the EF Core entity classes matching this data model" (paste the data model from the build plan)
- "Create the API endpoints for project CRUD"
- "Set up the Vite proxy config and Axios client"

**Use it for the hard parts:**
- "Help me parse a VB6 .vbp file — here's a sample" (paste a .vbp file)
- "Build the dependency graph topological sort algorithm"
- "Write the Claude API prompt construction for the roadmap generator"

**Let Claude Code run the tests:**
- "Run dotnet test and fix any failures"
- "Run npm run build and fix any TypeScript errors"

---

## Daily Development Workflow

```
1. Open terminal, cd to codeshift/
2. docker-compose up db              (start PostgreSQL)
3. Open VS Code: code .
4. Open integrated terminal, split into two panes:
   - Left pane:  cd src/api/CodeShift.Api && dotnet watch run
   - Right pane: cd src/web && npm run dev
5. Open another terminal tab for Claude Code: claude
6. Browser: http://localhost:5173 (React app)
7. Browser: http://localhost:5079/swagger (API docs, if Swagger enabled)
8. Build features, test, commit.
```

The `dotnet watch run` command gives you hot-reload on the API — save a .cs file and the API restarts automatically. Vite gives you instant hot-reload on the React side.

---

## Full Repository Structure (Final)

```
codeshift/
├── src/
│   ├── api/
│   │   ├── CodeShift.sln
│   │   ├── CodeShift.Api/
│   │   │   ├── CodeShift.Api.csproj
│   │   │   ├── Program.cs
│   │   │   ├── Endpoints/
│   │   │   │   ├── ProjectEndpoints.cs
│   │   │   │   ├── AnalysisEndpoints.cs
│   │   │   │   ├── RoadmapEndpoints.cs
│   │   │   │   └── TransformEndpoints.cs
│   │   │   └── appsettings.json
│   │   ├── CodeShift.Core/
│   │   │   ├── CodeShift.Core.csproj
│   │   │   ├── Analyzers/
│   │   │   │   ├── ICodebaseAnalyzer.cs
│   │   │   │   ├── CSharpAnalyzer.cs
│   │   │   │   ├── VbNetAnalyzer.cs
│   │   │   │   ├── Vb6Analyzer.cs
│   │   │   │   └── AnalyzerRouter.cs       # Detects language, dispatches to correct analyzer
│   │   │   ├── Services/
│   │   │   │   ├── DependencyGraphBuilder.cs
│   │   │   │   ├── HealthScoreCalculator.cs
│   │   │   │   ├── RoadmapGenerator.cs
│   │   │   │   └── TransformEngine.cs
│   │   │   └── Models/
│   │   │       ├── AnalysisResult.cs
│   │   │       ├── DetectedProject.cs
│   │   │       ├── DependencyEdge.cs
│   │   │       ├── RiskFlag.cs
│   │   │       ├── MigrationRoadmap.cs
│   │   │       └── TransformResult.cs
│   │   ├── CodeShift.Data/
│   │   │   ├── CodeShift.Data.csproj
│   │   │   ├── CodeShiftDbContext.cs
│   │   │   ├── Entities/                    # EF Core entities (DB-facing)
│   │   │   └── Migrations/
│   │   └── CodeShift.Tests/
│   │       ├── CodeShift.Tests.csproj
│   │       └── Analyzers/
│   │           ├── CSharpAnalyzerTests.cs
│   │           ├── VbNetAnalyzerTests.cs
│   │           └── Vb6AnalyzerTests.cs
│   └── web/
│       ├── package.json
│       ├── vite.config.ts
│       ├── tailwind.config.ts
│       ├── tsconfig.json
│       ├── index.html
│       └── src/
│           ├── main.tsx
│           ├── App.tsx
│           ├── api/
│           │   └── client.ts
│           ├── components/
│           │   ├── FileUpload.tsx
│           │   ├── HealthScore.tsx
│           │   ├── DependencyGraph.tsx
│           │   ├── LanguageBreakdown.tsx
│           │   ├── TechBreakdown.tsx
│           │   ├── RiskFlags.tsx
│           │   ├── RoadmapTimeline.tsx
│           │   └── DiffViewer.tsx
│           ├── pages/
│           │   ├── HomePage.tsx
│           │   ├── DashboardPage.tsx
│           │   ├── GraphPage.tsx
│           │   ├── RoadmapPage.tsx
│           │   └── TransformPage.tsx
│           └── types/
│               └── index.ts
├── infra/
│   ├── main.tf
│   ├── variables.tf
│   ├── outputs.tf
│   ├── app-service.tf
│   ├── database.tf
│   ├── storage.tf
│   ├── keyvault.tf
│   └── backend.tf
├── test-fixtures/
│   ├── webforms-sample/
│   ├── wcf-sample/
│   ├── vb6-sample/
│   ├── vbnet-sample/
│   └── mixed-solution/
├── .github/
│   └── workflows/
│       └── deploy.yml
├── .vscode/
│   ├── launch.json
│   └── tasks.json
├── docker-compose.yml
├── Dockerfile.api
├── .gitignore
└── README.md
```

---

## Quick Reference Commands

```bash
# Start everything for local dev
docker-compose up db                                    # PostgreSQL
cd src/api/CodeShift.Api && dotnet watch run             # API with hot reload
cd src/web && npm run dev                                # React with hot reload

# Build
dotnet build src/api/CodeShift.sln                       # Build .NET solution
cd src/web && npm run build                              # Build React for production

# Test
dotnet test src/api/CodeShift.sln                        # Run all .NET tests
cd src/web && npm run test                               # Run React tests (if configured)

# Database migrations
cd src/api/CodeShift.Api
dotnet ef migrations add InitialCreate --project ../CodeShift.Data
dotnet ef database update --project ../CodeShift.Data

# Docker build (for deployment)
docker build -f Dockerfile.api -t codeshift-api .

# Claude Code
cd codeshift && claude                                   # Start Claude Code at repo root

# Terraform (when ready to deploy)
cd infra && terraform init && terraform plan
```
