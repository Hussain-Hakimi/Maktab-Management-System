# 🇦🇫 Maktab Management System

**Offline Afghan School Management System**

A lightweight, offline-first desktop application designed for schools in Afghanistan, especially environments where internet access is limited or unavailable.

Built with **C# / .NET 8**, **WPF**, **SQLite**, and **QuestPDF**, with a Dari/Persian right-to-left interface.

---

## Table of Contents

- [Project Goal](#-project-goal)
- [Version 1.0 — Core Academic MVP](#-version-10--core-academic-mvp)
- [Technology Stack](#️-technology-stack)
- [Project Structure](#-project-structure)
- [Grading and Examination System](#-grading-and-examination-system)
- [Student Report Card / اطلاع‌نامه](#-student-report-card--اطلاعنامه)
- [Classes and Subjects](#-classes-and-subjects)
- [Student Management](#-student-management)
- [Mark Entry](#-mark-entry)
- [Database](#-database)
- [Backup and Restore](#-backup-and-restore)
- [Logging](#-logging)
- [Dari / Persian RTL Interface](#-dari--persian-rtl-interface)
- [Testing](#-testing)
- [Build and Run](#️-build-and-run)
- [Publish / Deployment](#-publish--deployment)
- [Continuous Integration](#-continuous-integration)
- [Development Roadmap](#️-development-roadmap)
- [Offline-First Design](#-offline-first-design)
- [Design Principles](#-design-principles)
- [Current Status](#-current-status)

---

## 🎯 Project Goal

The goal of this project is to provide a simple, reliable, and completely offline school-management solution for schools that need to manage:

- Classes and subjects
- Students
- Examination marks
- Automatic grading
- Student report cards
- Database backup and restore

The system is designed to work on a single Windows computer without requiring an internet connection or an online server.

## 🚀 Version 1.0 — Core Academic MVP

Version 1.0 focuses on the core academic management features.

### Included in V1.0

- 🏫 Class management
- 📚 Subject management
- 👨‍🎓 Student registration and management
- 📝 Examination mark entry
- 🧮 Automatic calculation of subject totals
- 🎓 Automatic student grading
- 📄 PDF report card / اطلاع‌نامه generation
- 💾 SQLite database
- 🔄 Database backup and restore
- 📝 Application logging
- 🇦🇫 Dari/Persian RTL interface
- 🧪 Automated unit tests

### Not Included in V1.0

The following features are **planned for later versions** and are not part of the current MVP:

- Attendance management
- Library management
- Textbook management
- Fee management
- User accounts and authentication
- Role-based permissions
- Advanced reports and analytics
- Multi-user/network support

This keeps V1.0 focused on the essential academic workflow.

## 🏗️ Technology Stack

| Component | Technology |
|---|---|
| Programming Language | C# |
| Framework | .NET 8 |
| Desktop UI | WPF |
| Database | SQLite |
| PDF Generation | QuestPDF |
| Architecture | Layered / Clean Architecture style |
| Testing | xUnit |
| Version Control | Git / GitHub |
| CI | GitHub Actions |

The application is intentionally desktop-based rather than web-based because the primary requirement is offline operation on Windows computers.

## 📁 Project Structure

```
Maktab-Management-System/
│
├── src/
│   │
│   ├── Maktab.Domain/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   └── Rules/
│   │
│   ├── Maktab.Application/
│   │   ├── Abstractions/    (interfaces + DTOs)
│   │   └── Services/
│   │
│   ├── Maktab.Infrastructure/
│   │   ├── Logging/
│   │   ├── Persistence/
│   │   └── Reports/
│   │
│   └── Maktab.App.Wpf/
│       ├── Views/
│       ├── MainWindow.xaml
│       └── App.xaml
│
├── tests/
│   └── Maktab.Tests/
│
├── .github/
│   └── workflows/
│
└── README.md
```

The project is separated into Domain, Application, Infrastructure, UI, and Test layers to make the system easier to maintain and extend.

## 📊 Grading and Examination System

The grading system follows the school's specified examination rules.

### Marks for Each Subject

Each subject has a maximum of 100 marks:

| Examination | Maximum |
|---|---|
| Midterm Examination | 40 |
| Final Examination | 60 |
| **Total** | **100** |

The system automatically calculates:

```
Subject Total = Midterm + Final
```

**Important:** A separate percentage is **not** calculated or displayed for each subject.

For example, if a student receives:

```
Midterm = 32
Final   = 51
```

the system records:

```
Total = 83
```

The report card does not display `83%` for that individual subject — the subject total is sufficient.

### 🎓 Student Group / Grade

The student's final group is determined from the **average of the student's subject totals**, not from any single subject.

| Average | Group | Dari |
|---|---|---|
| 90 – 100 | A | الف |
| 85 – 89.99 | B | ب |
| 75 – 84.99 | C | ج |
| 65 – 74.99 | D | د |
| Below 65 | F | ه |

### Promotion Rule

For a student to be promoted to the next grade (for example, from Grade 1 to Grade 2), **all** of the following conditions must be met. Promotion is determined by combining the student's overall average with how many individual subjects they failed.

**1. Overall average**
The student's overall average (average of subject totals) must be **65 or higher**. If the average is below 65, the student must repeat the same grade the following year — regardless of individual subject results.

**2. Per-subject pass mark**
In each subject, the combined total of the two exams (midterm + final) must be **40 or higher** to count as a pass in that subject. A subject total below 40 counts as a failed subject.

**3. Conditional promotion — 1 to 3 failed subjects**
If the student's average is 65 or higher, but the student has a total below 40 in **one, two, or three subjects**, the student **cannot** be promoted outright. The result shown on the report card is **"Conditional" (مشروط)** rather than "Promoted."

**4. Repeat grade — more than 3 failed subjects**
If the student has a total below 40 in **more than three subjects**, the student must **repeat the grade** — this applies even if the student's overall average is 65 or higher.

**5. Attendance Limit**
If a student has a total of **more than 30 days of absence** in an academic year, the student **cannot** pass and must **repeat the grade**, regardless of their average or subject results.

**Summary of possible outcomes:**

| Overall Average | Failed Subjects (Total < 40) | Absences (Days) | Result |
|---|---|---|---|
| ≥ 65 | 0 | ≤ 30 | ✅ **Promoted** |
| ≥ 65 | 1 – 3 | ≤ 30 | 🟡 **Conditional (مشروط)** |
| ≥ 65 | > 3 | Any | 🔴 **Repeat Grade** |
| < 65 | Any | Any | 🔴 **Repeat Grade** |
| Any | Any | > 30 | 🔴 **Repeat Grade** |

```
IF (Failed Subjects > 3):
    Result = REPEAT GRADE                      # regardless of average

ELSE IF (Average < 65):
    Result = REPEAT GRADE

ELSE IF (Failed Subjects is 1, 2, or 3):
    Result = CONDITIONAL (مشروط)                # average passes, but subject failures block promotion

ELSE:  # Average >= 65 AND Failed Subjects = 0
    Result = PROMOTED
```

These rules are implemented in `GradingPolicy.cs` and `PromotionPolicy.cs` and are applied consistently across mark calculation, report cards, tests, and future academic-year promotion functionality.

## 📄 Student Report Card / اطلاع‌نامه

The system generates an offline PDF report card for each student, containing:

- Student name
- Father's name
- Class
- Roll number
- Subject names
- Marks obtained for each subject
- Total marks
- Student's overall average
- Final group / grade
- Promotion status (Promoted / Conditional / Repeat Grade — see [Promotion Rule](#promotion-rule))
- Signature areas

### Subject Marks

For each subject, the report card displays the marks obtained out of the subject total. It does **not** calculate or display a separate percentage per subject.

```
Mathematics       82
Physics           76
Chemistry         91
Dari              88
```

The report card then calculates the student's overall result from these subject totals.

### 🧮 Overall Student Average

The student's overall average is calculated from the subject totals, and that average determines the student's final group.

The final report therefore focuses on:

- Total Marks
- Average
- Final Group
- Promotion Status

— rather than displaying unnecessary percentages for every subject.

## 🏫 Classes and Subjects

The system allows the administrator to manage:

**Classes**
- Add classes
- Edit classes
- Delete classes
- View class information

**Subjects**
- Add subjects
- Edit subjects
- Delete subjects
- Assign subjects to classes

## 👨‍🎓 Student Management

Student records include:

- Student ID
- First name
- Last name
- Father's name
- Class
- Roll number
- Registration date

The system validates student information and prevents duplicate roll numbers within the same class.

## 📝 Mark Entry

The mark-entry system supports:

- Selecting a class
- Selecting a subject
- Viewing students in the selected class
- Entering midterm marks
- Entering final marks
- Automatic total calculation
- Validation of mark ranges
- Saving examination results

**Mark limits:**

```
Midterm: 0–40
Final:   0–60
Total:   0–100
```

Invalid values are rejected.

## 💾 Database

The application uses **SQLite** as its local database, stored locally on the computer. This allows the application to operate without an internet connection or remote database server — architecture intentionally designed for environments where reliable internet access may not be available.

## 🔄 Backup and Restore

The application includes an offline database backup system supporting:

- Manual database backup
- Automatic startup backup
- Backup listing
- Database restoration
- Backup retention
- Old backup cleanup
- Backup error logging

The current backup retention policy keeps recent backups while removing older backups according to the configured retention period.

### Recommended Backup Practice

Users should periodically copy important backups to an external storage device such as:

- USB flash drive
- External hard drive
- Another trusted storage location

A backup stored only on the same computer does not protect against complete hardware failure.

## 📝 Logging

The application maintains local log files for important application events and errors. Logging helps with:

- Troubleshooting
- Diagnosing failures
- Backup monitoring
- Application maintenance

No internet connection is required for logging.

## 🇦🇫 Dari / Persian RTL Interface

The application provides a right-to-left interface designed for Dari/Persian-speaking users, with Dari labels for major school-management functions such as:

- صنف‌ها و مضامین
- شاگردان
- ثبت نمرات
- کارنامه / اطلاع‌نامه
- پشتیبان‌گیری و تنظیمات

## 🧪 Testing

The project includes automated tests covering important application logic and services. Current test coverage includes:

- Student management
- Examination marks
- Report cards
- Backup and restore (including a real backup → modify → restore round-trip)
- Domain rules
- Shamsi academic-year calculation
- SQLite integration tests against a real temporary database (schema constraints, unique roll numbers, mark upserts, FK cascade/restrict)

Tests are intended to prevent regressions as new features are added.

## ⚙️ Build and Run

### Requirements

For development:

- Windows
- .NET 8 SDK
- Visual Studio 2022 or another compatible .NET IDE
- Git

### Clone the Repository

```bash
git clone https://github.com/Hussain-Hakimi/Maktab-Management-System.git
cd Maktab-Management-System
```

### Restore Dependencies

```bash
dotnet restore
```

### Build

```bash
dotnet build
```

### Run Tests

```bash
dotnet test
```

### Run the Application

Open the WPF project at `src/Maktab.App.Wpf/` and run it using Visual Studio or the .NET CLI.

## 📦 Publish / Deployment

To create a self-contained Windows installer folder (no .NET installation required on the school computer):

```bash
dotnet publish src/Maktab.App.Wpf/Maktab.App.Wpf.csproj -c Release -r win-x64 --self-contained true
```

The output is written to `src/Maktab.App.Wpf/bin/Release/net8.0-windows/win-x64/publish/`. Copy that folder to the target Windows computer and run `Maktab.App.Wpf.exe`.

The application stores its database, backups, logs, and generated report cards in the `AppData` folder next to the executable, so keep the whole folder together when copying it.

## 🔁 Continuous Integration

The project uses GitHub Actions (`.github/workflows/dotnet.yml`) to automatically:

- Restore dependencies
- Build the project
- Run automated tests

This helps detect broken builds and failing tests when changes are pushed or submitted through pull requests.

Because the application is a Windows WPF application, the CI workflow uses a **Windows** GitHub-hosted runner (`windows-latest`).

## 🗺️ Development Roadmap

### Version 1.0 — Core Academic MVP
- Classes
- Subjects
- Student management
- Mark entry
- Automatic totals
- Student grading
- PDF report cards
- SQLite database
- Backup and restore
- Logging
- Dari RTL interface
- Unit tests

### Version 1.0.1 — Stabilization

This release focused on making the existing MVP reliable before adding new features.

Completed:
- ✅ New grading rules verified throughout the application (domain, services, PDF, UI)
- ✅ Grading and report-card tests updated
- ✅ GitHub Actions CI added (`windows-latest` runner)
- ✅ Database/integration tests added (real SQLite: CRUD, unique roll numbers, mark upserts, CHECK constraints, FK cascade/restrict)
- ✅ Backup and restore round-trip tests added
- ✅ Foreign-key enforcement fixed on every connection (`ForeignKeys = true` in the connection string)
- ✅ Hardcoded academic year removed — the current Shamsi academic year is now detected automatically (e.g. «۱۴۰۵ - ۱۴۰۶»)
- ✅ Global error handling added (UI, background threads, and unobserved tasks are logged; a friendly Dari message is shown)
- ✅ Publish/deployment instructions documented (see [Publish / Deployment](#-publish--deployment))

### Version 1.1 — School Operations

**Attendance**
- Weekly/Monthly Excel templates for offline attendance tracking.
- Pre-filled templates default to "Present".
- Supported statuses: Present, Absent, Ill, Permission.
- Manual data entry (Admin-driven) or Excel import functionality.
- Automatic absence tallying tied to promotion rules.
- Daily attendance
- Present/absent records
- Absence statistics
- Attendance reports

**Library**
- Book management
- Book issuing
- Book returns
- Due dates
- Overdue records

**Textbooks**
- Textbook inventory
- Student textbook issuing
- Return tracking

**Fees**
- Fee records
- Payment tracking
- Receipts
- Outstanding fees

### Version 1.2 — Administration
- User accounts
- Authentication
- Role-based permissions
- Administrator account
- Teacher accounts
- Librarian account
- Accountant account
- Expanded audit logging
- School settings

### Version 2.0 — Advanced School Management

Possible future features:
- Academic-year management
- Student enrollment history
- Student promotion workflow
- Advanced reports (class performance, subject performance, attendance analytics)
- Excel import/export and report generation
- Advanced dashboard
- Multi-user/network support

These features will only be introduced when they are needed and when the core offline system is stable.

## 🔐 Offline-First Design

The application is designed around an offline-first philosophy. Core functionality should not depend on:

- Internet access
- Cloud services
- Online accounts
- Remote databases
- External APIs

The primary data remains on the local computer — particularly important for schools operating in areas with unreliable or unavailable internet connectivity.

## 🎯 Design Principles

- **Simple** — the application should be easy for school staff to understand and operate.
- **Offline** — core school operations should work without internet access.
- **Reliable** — student and examination data must be protected against accidental loss.
- **Maintainable** — the codebase should remain modular and testable.
- **Localized** — the interface should be appropriate for Dari/Persian-speaking users.
- **Extensible** — the architecture should allow future features without requiring a complete rewrite.

## 📌 Current Status

**Version:** 1.0.1 — Stabilization Release

The core MVP includes:

```
Classes
   ↓
Subjects
   ↓
Students
   ↓
Marks
   ↓
Subject Totals
   ↓
Overall Average
   ↓
A / B / C / D / F
   ↓
Report Card
```

`GradingPolicy.cs`, `PromotionPolicy.cs`, the report-card logic, the UI, and the tests are all in sync with the grading rules described in this document. Version 1.0.1 completed the stabilization roadmap (integration tests, backup/restore tests, FK enforcement, automatic Shamsi academic year, global error handling, publish instructions). The next development priority is the **V1.1 school-operation features** (attendance, library, textbooks, fees).

---

## 👨‍💻 Project

**Maktab Management System** — an offline school-management application designed with the goal of making academic administration simpler and more accessible for Afghan schools.

Built with ❤️ for Afghan schools.
