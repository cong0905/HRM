# Human Resource Management System (HRM)

A desktop Human Resource Management application built with **C# / .NET 8 WinForms**, **Entity Framework Core**, and **SQL Server** using a layered **N-Tier architecture**.

The project focuses on clean separation of concerns between presentation, business logic, data access, shared DTO/helpers, and domain entities.

## Highlights

- .NET 8 WinForms desktop application
- 5-layer N-Tier architecture
- Entity Framework Core Code-First
- Fluent API configuration
- Repository Pattern and Service Layer
- Dependency Injection
- DTO-based data transfer between layers
- Authentication and role-based authorization
- Password hashing with BCrypt
- Auto-Migration and Auto-Seeding
- Gemini AI integration for HR assistance

## Architecture

```text
HRM.GUI
   ↓
HRM.BLL
   ↓
HRM.DAL
   ↓
SQL Server

HRM.Common → shared DTOs and helpers
HRM.Domain → entities / domain models
```

### Layers

| Project | Responsibility |
| --- | --- |
| `HRM.Domain` | Domain entities mapped to the database |
| `HRM.DAL` | EF Core DbContext, repositories, database access |
| `HRM.BLL` | Business rules and application services |
| `HRM.GUI` | WinForms presentation layer |
| `HRM.Common` | DTOs, shared helpers, BCrypt utilities |

The GUI layer does not access `DbContext` or repositories directly. Business operations go through services in the BLL layer.

## Tech Stack

- **Language:** C#
- **Framework:** .NET 8 / WinForms
- **ORM:** Entity Framework Core
- **Database:** SQL Server
- **Security:** BCrypt.Net-Next
- **Architecture:** N-Tier, Repository Pattern, Service Layer, DTO, Dependency Injection
- **AI:** Google Gemini API

## Project Structure

```text
db/
├── src/
│   ├── HRM.Domain/
│   ├── HRM.DAL/
│   ├── HRM.BLL/
│   ├── HRM.GUI/
│   ├── HRM.Common/
│   └── HRM.sln
├── docs/
├── README.md
└── HuongDanChayDuAn.md
```

## Getting Started

### Requirements

- Visual Studio 2022
- .NET 8 SDK
- SQL Server or SQL Server LocalDB

### Run locally

1. Clone the repository.
2. Open `db/src/HRM.sln` in Visual Studio.
3. Copy:
   `db/src/HRM.GUI/appsettings.json.example`
   to:
   `db/src/HRM.GUI/appsettings.json`
4. Configure the SQL Server connection string.
5. Optionally add a Gemini API key for AI features.
6. Set `HRM.GUI` as the Startup Project.
7. Press **F5**.

The application supports automatic EF Core migration and seed data initialization on startup.

## Configuration Example

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=HRM_System;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY_HERE"
  }
}
```

Never commit real API keys or production credentials.

## Additional Documentation

- [Detailed project documentation](db/README.md)
- [Vietnamese setup guide](db/HuongDanChayDuAn.md)

## Author

**Nguyễn Đức Công**

- GitHub: https://github.com/cong0905
