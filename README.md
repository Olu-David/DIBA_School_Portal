🏫 Dominion Intelligent Builder Academic School Portal (DIBA Portal)

A robust, enterprise-grade, multi-tier School Management System (SMS) built for Dominion Intelligent Builder Academic School. The platform features a secure ASP.NET Core Web API backend and a modern React & TypeScript frontend, designed to manage the complete academic lifecycle — from admissions and fee processing to continuous assessments and automated result generation.

---

📖 Table of Contents

· Tech Stack
· System Architecture & Database Models
· Administrative Hierarchy & Roles (RBAC)
· Getting Started
· Development Roadmap
· Core API Endpoints Reference
· Security & Architecture Best Practices
· Contributing & Development Workflow
· License

---

🛠️ Tech Stack

Layer Technologies
Backend ASP.NET Core Web API (.NET 9/8), Entity Framework Core, ASP.NET Core Identity, JWT Authentication, PostgreSQL / MS SQL Server
Frontend React, TypeScript, Vite, React Router, Axios, Bootstrap / Tailwind CSS
Infrastructure & Services Redis (Distributed Caching), Paystack / Flutterwave Webhooks (Payment Gateway), QuestPDF / DinkToPdf (Report Card Generation)

---

🏛️ System Architecture & Database Models

The backend utilizes Entity Framework Core structured into clean, modular functional domains:

1. System, Security & Audit

· ApplicationUser — Extends IdentityUser with custom profile metadata (FirstName, LastName, ProfilePictureUrl, IsActive).
· AuditLog — Tracks critical administrative actions, record modifications (old vs. new values), user IDs, IP addresses, and timestamps.
· NotificationLog — Records automated email, SMS, and in-app alerts dispatched to students, parents, and staff.

2. Academic Structure & Setup

· Session — Represents academic years (e.g., 2026/2027) with active state tracking.
· Term — Term subdivisions (First Term, Second Term, Third Term).
· Section — Major academic divisions (Enum: Nursery, Primary, JuniorSecondary, SeniorSecondary).
· ClassLevel — Grade tiers (Primary 1, JSS 1, SS 3).
· ClassRoom (Class Arms) — Specific classroom streams (e.g., JSS 1A, JSS 1B) assigned to specific Form Teachers.

3. Curriculum & Enrollment

· Subject — Master course directory (Mathematics, Basic Science, etc.).
· ClassSubject — Junction table mapping which subjects are taught in specific class levels.
· StudentProfile & TeacherProfile — Extended user metadata and institutional identification numbers.
· Enrollment — Tracks student placement in a specific ClassRoom for a given term and session.

4. Assessment & Result Processing

· AssessmentConfig — Configures grading components and percentage weights per term (e.g., Test 1: 10%, Test 2: 10%, Exam: 80%).
· ScoreEntry — Individual student mark entries per assessment component.
· TermResult — Aggregated performance summary containing total marks, averages, class positions, and teacher remarks.

5. Finance & Fee Management

· FeeCategory — Expense types (Tuition, Uniform, ICT Levy, Laboratory Fee).
· FeeStructure — Financial pricing mapped to specific class levels and terms.
· StudentInvoice — Billed liabilities generated for enrolled students.
· PaymentTransaction — Payment gateway webhook logs, reference hashes, and financial reconciliation channels.

---

🔐 Scalable Administrative Hierarchy & Roles (RBAC)

To reflect the school's precise management structure, the portal uses a granular, tiered Role-Based Access Control matrix configured via ASP.NET Core Identity:

Role Name Organizational Level & Core Responsibilities
SuperAdmin Platform owner; manages system infrastructure, multi-tenant databases, global licenses, and master configurations.
Principal Chief Executive Academic Officer; holds ultimate institutional oversight over senior sections, signs off final terminal report cards, and approves staff evaluations.
Headmaster / Headmistress Administrative head for primary and nursery sections; oversees junior student discipline, attendance tracking, and primary class allocations.
VicePrincipal Executive assistant to the Principal; manages daily timetables, disciplinary committees, academic scheduling, and teacher oversight.
SchoolAdmin Operations manager; handles general day-to-day configuration, user account creation, password resets, and announcements.
Bursar / Accountant Dedicated financial management; handles fee structures, student invoices, payment gateway reconciliation, and debt tracing.
Registrar Manages admissions, student registration numbers, enrollment cycles, and class arm placements.
Teacher Subject instructors; inputs continuous assessment/exam marks, records subject attendance, and uploads lesson notes.
FormTeacher Upgraded teacher role with clearance to review class-wide performance, track general behavior, and write terminal remarks.
Student End-user portal access for viewing personal timetables, assignments, published result sheets, and fee history.
Parent / Guardian Family portal access to link multiple children across sections, track attendance/results, and pay school fees online.

---

🚀 Getting Started

1. Backend Setup (.NET API)

Navigate to the backend directory:

```bash
cd backend/OnlineVotingApplication
```

Configure your database connection string in appsettings.json:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=DIBA_Portal_Db;Username=postgres;Password=yourpassword"
  }
}
```

Apply database migrations:

```bash
dotnet ef database update
```

Start the API server:

```bash
dotnet run
```

2. Frontend Setup (React & Vite)

Navigate to the frontend directory:

```bash
cd frontend
```

Install dependencies:

```bash
npm install
```

Start the development server:

```bash
npm run dev
```

---

📌 Development Roadmap

☑ ASP.NET Core Identity & JWT Authentication Setup
☑ Admin User Management & Penalty Lockout Module
☑ Audit Logging Middleware Architecture
☑ Academic Term & Session Management Module
☑ Student Enrollment & Class Arms Routing
☑ Continuous Assessment & Grading Engine
☐ Fee Structure & Payment Gateway Integration
☐ React Dashboard, Student, and Parent Portal Views

---

🔌 Core API Endpoints Reference

The backend exposes RESTful endpoints secured via JWT Bearer tokens:

Authentication — /api/auth

Method Endpoint Description
POST /login Authenticates users and returns JWT access/refresh tokens.
POST /register Registers new portal users (restricted by role).

User & Role Administration — /api/admin/users

Method Endpoint Description
GET /all Retrieves paginated system users with stats and role filters.
POST /penalty-lockout Applies a 100-year administrative penalty lockout with audit logging.

Academic Structure — /api/academic

Method Endpoint Description
GET /sessions Lists academic sessions and active terms.
POST /class-rooms Manages class arms and student allocations.

Grading & Results — /api/results

Method Endpoint Description
POST /scores Inputs continuous assessment and exam marks by authorized teachers.
GET /terminal-report/{studentId} Compiles and calculates term results, averages, and positions.

Finance — /api/finance

Method Endpoint Description
POST /invoices Generates student fee liabilities.
POST /webhook/paystack Processes automated payment gateway confirmations and receipts.

---

🛡️ Security & Architecture Best Practices

· Token-Based Security — Stateless JWT authentication paired with secure HTTP-only cookies or encrypted local storage headers.
· Data Isolation — Role-Based Access Control (RBAC) enforced via ASP.NET Core [Authorize(Roles = "...")] attributes on controllers and specific endpoint actions.
· Audit Compliance — Every critical mutation (such as penalty lockouts, grade updates, and fee waivers) is captured automatically via middleware into the AuditLog table.
· Input Validation — Comprehensive model state validation on both the API DTOs and React forms to prevent injection and invalid relational mappings.

---

🤝 Contributing & Development Workflow

1. Create a feature branch for your module:
   ```bash
   git checkout -b feature/session-term-module
   ```
2. Commit your changes with descriptive messages:
   ```bash
   git commit -m "feat: added academic session and active term configuration"
   ```
3. Push to the branch and open a Pull Request for code review against the main repository.

---

📜 License

This project is proprietary software developed exclusively for Dominion Intelligent Builder Academic School. All rights reserved.

---

<p align="center">
  <sub>Built with ❤️ for Dominion Intelligent Builder Academic School</sub>
</p>
