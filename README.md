# Elmansa Education Platform — Backend API

A RESTful API backend built with ASP.NET Core 8.0, powering the **Elmanssa Education Platform** — an educational learning management system with integrated AI tutoring for Saudi Arabian students (Qudrat, Tahseely, and high school curricula). Provides course management, student enrollment with progress tracking, JWT authentication with email confirmation, and a Gemini-powered AI educational assistant with per-user rate limiting and usage quotas.

---

## Overview

This API serves as the backend for the [Elmanssa Education Platform](https://github.com/phlzas/Elmansa-education-platform) frontend. It enables instructors to create and publish courses with lessons, students to enroll and track their learning progress, and all authenticated users to interact with an AI tutor ("Gemmy") that answers curriculum-specific questions in Arabic.

The system is designed for the Saudi education market, with the AI assistant scoped to Qudrat tests, Tahseely tests, and Saudi high school curricula.

---

## ✨ API Features

- **Authentication & User Management** — Registration with email confirmation, JWT login with refresh tokens, password reset flow, two-factor authentication support, user profile management
- **Course Management** — Full CRUD for courses, publish/unpublish workflow, paginated listing with search and category filtering, popular courses endpoint, instructor's own courses view
- **Enrollment & Progress Tracking** — Student enrollment in courses, per-lesson progress tracking (percentage + completion), enrollment completion with grades, course drop functionality
- **AI Educational Assistant** — Gemini-powered chat endpoint scoped to Saudi curriculum, token-based usage tracking per request, cost calculation per interaction
- **Rate Limiting & Quotas** — Daily and monthly token quotas per user, per-minute request throttling, IP-based rate limiting, automatic suspension on threshold breach, quota status visibility
- **Email Services** — SMTP-based email for confirmation links and password reset flows
- **Swagger/OpenAPI** — Full interactive API documentation served at the root URL in development

---

## 🛠 Tech Stack

| Component | Technology | Version |
|---|---|---|
| Framework | ASP.NET Core (Web API) | .NET 8.0 |
| Language | C# | 12 |
| ORM | Entity Framework Core | 8.0.24 |
| Database Provider | SQL Server (LocalDB default) | — |
| Authentication | ASP.NET Core Identity + JWT Bearer | 8.0.24 |
| JWT Library | System.IdentityModel.Tokens.Jwt | 8.16.0 |
| API Documentation | Swashbuckle (Swagger) | 6.6.2 |
| AI Integration | Google Gemini (generativelanguage API) | gemini-2.5-flash-light |
| Container Support | .NET SDK Containers (Windows nanoserver) | — |

---

## 📂 Architecture

The project follows a **layered architecture** with clear separation of concerns:

```
Controllers/          ← API endpoints (HTTP layer, route handling, auth checks)
    ↓
Services/             ← Business logic (interfaces + implementations)
    ↓
Repositories/         ← Data access (Generic Repository + Unit of Work pattern)
    ↓
Data/                 ← EF Core DbContext (AppDbContext : IdentityDbContext)
    ↓
Models/               ← Domain entities (Course, Lesson, Enrollment, etc.)
```

### Key Design Patterns

- **Repository Pattern** — `IGenericRepository<T>` for common CRUD operations, with domain-specific repositories (`ICourseRepository`, `IEnrollmentRepository`, `ILessonRepository`) for tailored queries
- **Unit of Work** — `IUnitOfWork` coordinates transactional commits across multiple repositories
- **Options Pattern** — Configuration classes (`EmailSettings`, `AIOptions`, `RateLimitOptions`) bound from `appsettings.json` via `IConfiguration`
- **DTOs / Request-Response** — Dedicated request/response objects in `RequestResponse/` (`AuthDtos`, `CourseRequestResponse`, `AIRequestDto`) decouple API contracts from domain models

### Project Structure

```
├── Configuration/          # Options classes (IdentityConfiguration, EmailSettings, AIOptions, RateLimitOptions)
├── Controllers/            # AuthController, CoursesController, EnrollmentsController, AIController
├── Data/                   # AppDbContext with EF Core Fluent API configuration
├── Migrations/             # EF Core database migrations
├── Models/                 # Domain entities (ApplicationUser, Course, Lesson, Enrollment, LessonProgress, AIRequestLog, UsageQuota)
├── Properties/             # launchSettings.json
├── Repositories/           # Generic + specific repositories + Unit of Work
├── RequestResponse/        # DTOs for API requests and responses
├── Services/               # Service interfaces
│   └── Implementation/     # Service implementations (CourseService, EnrollmentService, TokenService, EmailService, GeminiAIService, RateLimitService)
├── Program.cs              # Application entry point and DI configuration
├── appsettings.json        # Configuration (connection strings, JWT, email, AI, rate limiting)
└── Elmansa api.csproj      # Project file (.NET 8.0)
```

---

## 🔌 Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server LocalDB (included with Visual Studio) or a SQL Server instance

### Setup

1. **Clone the repository**

   ```bash
   git clone https://github.com/phlzas/Elmansa-api.git
   cd Elmansa-api
   ```

2. **Configure secrets** — Update `appsettings.json` or use User Secrets:

   ```bash
   dotnet user-secrets set "Jwt:Key" "your-production-jwt-key-minimum-32-characters"
   dotnet user-secrets set "EmailSettings:Username" "your-gmail@gmail.com"
   dotnet user-secrets set "EmailSettings:Password" "your-gmail-app-password"
   dotnet user-secrets set "AISettings:ApiKey" "your-google-gemini-api-key"
   ```

   At minimum, you must set `Jwt:Key` (minimum 32 characters) or the app will throw on startup.

3. **Update the connection string** in `appsettings.json` if not using LocalDB:

   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=your-server;Database=ELmansa-Db;Trusted_Connection=True;TrustServerCertificate=True"
   }
   ```

4. **Run database migrations**

   ```bash
   dotnet ef database update
   ```

5. **Run the API**

   ```bash
   dotnet run
   ```

   - HTTP: `http://localhost:5095`
   - HTTPS: `https://localhost:7273`

6. **Access Swagger UI** — In Development mode, Swagger is served at the root URL (`/`):

   ```
   https://localhost:7273/
   ```

   The Swagger UI includes JWT Bearer token support — click "Authorize" and paste your JWT token to test authenticated endpoints.

---

## 🔗 Frontend Integration

This API is the backend companion to the **Elmanssa Education Platform** frontend (`phlzas/Elmansa-education-platform`).

- The frontend is expected to run on `http://localhost:3000` (configured via `AppUrl` in `appsettings.json`)
- Email confirmation and password reset links are constructed using this `AppUrl`
- CORS should be configured on the frontend or via a reverse proxy for production deployment

---

## 🗺 API Overview

All endpoints are prefixed with `/api/`. Authentication endpoints are public; course read endpoints are public; course write, enrollment, and AI endpoints require a valid JWT Bearer token.

### Auth (`/api/Auth`)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| POST | `/register` | Register a new user | Public |
| POST | `/login` | Login, returns JWT + refresh token | Public |
| POST | `/confirm-email` | Confirm email with token | Public |
| POST | `/resend-confirmation-email` | Resend email confirmation | Public |
| POST | `/forgot-password` | Initiate password reset | Public |
| POST | `/reset-password` | Reset password with token | Public |
| GET | `/me` | Get current user profile | Required |
| GET | `/manage/info` | Get account management info | Required |
| POST | `/manage/info` | Update account info (phone) | Required |
| POST | `/manage/2fa` | Enable/disable two-factor auth | Required |

### Courses (`/api/Courses`)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| GET | `/` | List all courses (paginated) | Public |
| GET | `/published` | List published courses | Public |
| GET | `/popular` | Get popular courses | Public |
| GET | `/search` | Search courses by term | Public |
| GET | `/category/{category}` | Filter by category | Public |
| GET | `/{id}` | Get course detail with lessons | Public |
| POST | `/` | Create a new course | Required |
| PUT | `/{id}` | Update a course | Required |
| DELETE | `/{id}` | Delete a course | Required |
| POST | `/{id}/publish` | Publish a course | Required |
| GET | `/my-courses` | Get instructor's own courses | Required |

### Enrollments (`/api/Enrollments`)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| GET | `/my-enrollments` | Get current user's enrollments | Required |
| GET | `/course/{courseId}` | Get enrollments for a course | Required |
| GET | `/{enrollmentId}` | Get enrollment details | Required |
| POST | `/enroll` | Enroll in a course | Required |
| POST | `/{enrollmentId}/lesson-progress` | Update lesson progress | Required |
| POST | `/{enrollmentId}/complete` | Complete an enrollment | Required |
| POST | `/{enrollmentId}/drop` | Drop a course | Required |

### AI (`/api/AI`)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| POST | `/ask` | Send prompt to AI assistant | Required |
| GET | `/quota-status` | Get usage quota status | Required |
| GET | `/history` | Get AI request history (paginated) | Required |

---

## Configuration Reference

| Setting | Purpose | Default |
|---------|---------|---------|
| `ConnectionStrings:DefaultConnection` | SQL Server connection | LocalDB, `ELmansa-Db` |
| `Jwt:Key` | JWT signing key (min 32 chars) | *Must be configured* |
| `Jwt:Issuer` | JWT issuer claim | `Elmansa` |
| `Jwt:Audience` | JWT audience claim | `ElmansaUsers` |
| `Jwt:ExpirationMinutes` | Token lifetime in minutes | `60` |
| `AppUrl` | Frontend URL (for email links) | `http://localhost:3000` |
| `EmailSettings` | SMTP configuration for emails | Gmail SMTP (587) |
| `AISettings` | Gemini API configuration | `gemini-2.5-flash-light` |
| `RateLimitSettings` | Token quotas and throttling | 10K daily / 100K monthly tokens |

---

## License

MIT License — see the Swagger metadata for details.

Contact: [hamedrabi3@gmail.com](mailto:hamedrabi3@gmail.com) | [elmanssa.com](https://elmanssa.com)
