# 🇦🇫 Maktab Management System

> **Offline-First School Management for Afghanistan**
>
> A lightweight, reliable desktop application for managing schools in environments with limited or no internet access.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![Platform](https://img.shields.io/badge/Platform-Windows-0078D4?logo=windows)](https://www.microsoft.com/windows)
[![Language](https://img.shields.io/badge/Language-C%23-239120?logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp)
[![Status](https://img.shields.io/badge/Status-Production%20Ready-28a745)](https://github.com/Hussain-Hakimi/Maktab-Management-System)

---

## ✨ Overview

**Maktab** (Dari: *School*) is a complete school management solution designed specifically for Afghan educational institutions. Built for reliability and offline operation, it handles everything from student records to grades, attendance, library management, and financial tracking—all without requiring an internet connection.

### Why Maktab?

- ✅ **Completely Offline** — Works anywhere, no cloud dependency
- ✅ **Built for Afghan Schools** — Dari/Persian RTL interface, Afghan grading system
- ✅ **All-in-One** — No separate tools needed
- ✅ **Fast & Lightweight** — Runs smoothly on modest hardware
- ✅ **Secure** — Database encryption, audit logging, role-based access
- ✅ **Data-Safe** — Automatic backups, USB export, restore tools
- ✅ **Production Ready** — v1.5 stable release, actively maintained

---

## 🚀 Quick Start

### Prerequisites

- Windows 10 or later
- .NET 8 Runtime (or SDK for development)
- 100 MB free disk space

### Installation

1. **Download** the latest release from [GitHub Releases](https://github.com/Hussain-Hakimi/Maktab-Management-System/releases)
2. **Extract** the publish folder to your desired location
3. **Run** `Maktab.App.Wpf.exe`
4. **Login** with default credentials (set up on first run)

### For Development

```bash
# Clone the repository
git clone https://github.com/Hussain-Hakimi/Maktab-Management-System.git
cd Maktab-Management-System

# Install dependencies and build
dotnet restore
dotnet build

# Run tests
dotnet test

# Start the application
dotnet run --project src/Maktab.App.Wpf/Maktab.App.Wpf.csproj
```

---

## 📋 Features

### 📚 Core Academic Management

- **Classes & Subjects** — Organize by class, assign subjects
- **Student Records** — Complete profiles with enrollment tracking
- **Mark Entry** — Midterm (0–40) + Final (0–60), auto-calculated totals
- **Automatic Grading** — A–F grades with Dari translation
- **Report Cards** — Multiple templates (Simple, Standard, Detailed) in PDF
- **Academic Years** — Create, manage, and promote to new years
- **Student Promotion** — Configurable rules based on GPA, failures, and attendance

### 👥 Student & Staff Management

- **Attendance Tracking** — Daily entry with statuses (Present, Absent, Ill, Permission)
- **User Accounts** — Role-based access (Admin, Teacher, Librarian, Accountant)
- **Secure Authentication** — PBKDF2 password hashing
- **Audit Logging** — Complete audit trail of user actions
- **School Settings** — Name, address, phone, logo, academic year

### 📚 Library Management

- **Book Inventory** — Track all library holdings
- **Issue/Return System** — Manage borrowing with due dates
- **Overdue Alerts** — Automatic tracking of late returns

### 📦 Textbook Distribution

- **Inventory Management** — Track textbook stock
- **Student Distribution** — Issue books to students
- **Return Tracking** — Manage returns and accountability

### 💰 Financial Management

- **Fee Management** — Record and track student fees
- **Payment Tracking** — Outstanding balances and payment history
- **Fee Alerts** — Outstanding fee notifications

### 📊 Reports & Data Export

- **Grade Distribution Reports** — Analyze academic performance
- **Attendance Reports** — Track attendance trends
- **Excel Export** — Export students, marks, attendance, fees
- **Excel/CSV Import** — Bulk import for students, marks, and attendance
- **Customizable Reports** — Filter by class, subject, date range

### 🔔 Smart Alerts

- **Overdue Books** — Library overdue notifications
- **Outstanding Fees** — Payment reminders
- **Attendance Alerts** — High absence rate notifications
- **Alerts Dashboard** — Centralized alerts center

### 💾 Data Management

- **Backup & Restore** — Manual and automatic backups
- **USB Export** — One-click copy backup to USB drive
- **Backup Reminders** — Notifications if last backup > 7 days
- **Database Migrations** — Safe schema updates
- **WAL Mode** — Write-ahead logging for data integrity

---

## 🎓 Academic System Details

### Grading Scale

| Average | Grade | Dari | Status |
|---------|-------|------|--------|
| 90–100 | A | الف | Excellent |
| 85–89.99 | B | ب | Very Good |
| 75–84.99 | C | ج | Good |
| 65–74.99 | D | د | Acceptable |
| <65 | F | ه | Fail |

### Promotion Rules

Students are promoted if they meet **all** criteria:

✅ Average score ≥ 65  
✅ Failed no more than 3 subjects  
✅ Absences ≤ 30 days  

**Status:**
- 🟢 **Promoted** — All criteria met
- 🟡 **Conditional** — Average ≥ 65 but 1–3 failures
- 🔴 **Repeat** — Any criterion failed

*All thresholds are configurable by admins.*

### Report Card

Student report cards include:
- Student and father's name
- Roll number and class
- Subject marks (midterm + final)
- Overall average and grade
- Promotion status
- Absence days
- Signature spaces for teachers and principal

Available templates: **Simple** (minimal), **Standard** (full details), **Detailed** (with statistics).

---

## 🏗️ Architecture

```
Maktab-Management-System/
│
├── src/
│   ├── Maktab.Domain/              # Core business logic & entities
│   ├── Maktab.Application/         # Services & use cases
│   ├── Maktab.Infrastructure/      # Database, migrations, logging
│   └── Maktab.App.Wpf/             # WPF UI & application layer
│
├── tests/
│   └── Maktab.Tests/               # Unit & integration tests
│
├── .github/
│   └── workflows/                  # CI/CD pipelines
│
└── README.md
```

**Architecture Style:** Layered / Clean Architecture with dependency injection, repository pattern, and service-oriented design.

---

## 🛠️ Technology Stack

| Component | Technology | Purpose |
|-----------|-----------|---------|
| **Language** | C# 12 | Type-safe, modern .NET development |
| **Framework** | .NET 8 | High-performance runtime |
| **UI Framework** | WPF | Native Windows desktop interface |
| **Database** | SQLite | Lightweight, embedded, zero-config |
| **PDF Generation** | QuestPDF | Professional report generation |
| **Excel** | ClosedXML | Read/write .xlsx files |
| **Charts** | LiveCharts2 + SkiaSharp | Dashboard visualizations |
| **Testing** | xUnit | Modern unit testing framework |
| **Version Control** | Git / GitHub | Source management & collaboration |
| **CI/CD** | GitHub Actions | Automated build & test |

---

## 🌐 Localization

### Dari/Persian (فارسی/دری)

- **Full RTL Support** — Right-to-left interface for Dari text
- **Native Labels** — All UI elements in Dari
- **Grade Names** — A–F grades with Dari translations
- **Regional Calendar** — Afghan academic year conventions
- **Date Formats** — Localized date handling

*English labels and Dari translations are included throughout the system.*

---

## 🔐 Security & Reliability

### Authentication & Authorization

- PBKDF2 password hashing (NIST-compliant)
- Role-based access control (RBAC)
- Session management
- Password change functionality

### Data Protection

- SQLite database with WAL mode
- Automatic startup backups
- Backup encryption & integrity checks
- USB backup with offline storage
- Complete audit logs of all changes

### Integrity

- Database migrations with version control
- Transaction support for critical operations
- Data validation at application and database levels
- Foreign key constraints

---

## 📊 Version History

| Version | Release | Focus |
|---------|---------|-------|
| **1.5.0** | Current | Data safety, USB backup, grade views, Excel import, backup reminders |
| **1.4.0** | ✓ | Alerts center, bulk CSV import, enhanced dashboard |
| **1.3.0** | ✓ | Academic years, promotion, advanced reports, Excel export |
| **1.2.0** | ✓ | User accounts, roles, audit logging, bulk import |
| **1.1.0** | ✓ | Attendance, library, textbooks, fees |
| **1.0.1** | ✓ | Core MVP — classes, students, marks, grading, reports |

---
### Version 1.6.0 — Role Assignment & Simplified Navigation

Added:
- Top main tabs with side sub-menus (simplified UI)
- TeacherSubject and ClassGuardian assignment (Admin)
- Database migration v6
- Teacher assignment service/repository
- UI for assigning teachers to subjects and designating class guardians

---
 ### Version 1.7.0 — Teacher Exam Workflow & Restricted Marks

Added:
- Exam entity and table (Midterm, Final)
- Exam creation restricted to assigned teachers
- Teacher sees only their assigned classes/subjects in mark entry
- Class guardians can view all subjects in their class but edit only their own
- “My Subjects” view for teachers
- Database migration v7

  ---

### Version 1.8.0 — Guardian Report Card & Finalization

Added:
- Guardian Class View (view all subjects, edit only own)
- Report card generation restricted to guardians and admins
- Class finalization workflow (finalize/unfinalize results)
- Finalization prevents further mark edits
- Database migration v8
- --
### Version 1.9.0 — Improvements & Correctness

Added:
- Login window with first‑run auto‑login (empty credentials → admin)
- Removed attendance trend and grade distribution panels from Dashboard
- Student roll number auto‑generation (next available integer)
- Textbooks subject dropdown from database
- Report card types: Annual and Midterm (with different layouts)
- Configurable official headers (government title, provincial education, district education)
- School logo upload and display in PDF report cards

- --

## 🗺️ Roadmap

### V1.6 (Planning)
- Enhanced fee management (installments, discounts, receipts)
- Attendance periods & tracking
- Admin password reset functionality
- Advanced PDF report templates

### V2.0 (Future)
- Multi-user network support (optional)
- Advanced analytics & dashboards
- Integration APIs
- Mobile companion app

---

## 🧪 Testing

The project includes comprehensive unit and integration tests:

```bash
dotnet test
```

Tests cover:
- Services and business logic
- Repository patterns
- Database migrations
- Excel/CSV import/export
- Report generation
- User authentication

---

## 📦 Build & Publish

### Development Build

```bash
dotnet build
```

### Release Build

```bash
dotnet publish src/Maktab.App.Wpf/Maktab.App.Wpf.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true
```

**Output:** Self-contained executable in the `publish` folder. Copy to any Windows 10+ machine—no .NET installation required.

---

## 🔁 Continuous Integration

Every commit triggers:
- ✅ Code build on Windows runner
- ✅ Unit test suite
- ✅ Integration tests
- ✅ Release artifact generation

Status: [![CI/CD](https://github.com/Hussain-Hakimi/Maktab-Management-System/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Hussain-Hakimi/Maktab-Management-System/actions)

---

## 🤝 Contributing

We welcome contributions! Here's how to get started:

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Commit** your changes (`git commit -m 'Add amazing feature'`)
4. **Push** to the branch (`git push origin feature/amazing-feature`)
5. **Open** a Pull Request

### Development Guidelines

- Follow C# coding conventions
- Write tests for new features
- Update documentation
- Test on Windows 10+
- Ensure CI passes

### Reporting Issues

Found a bug? Please [open an issue](https://github.com/Hussain-Hakimi/Maktab-Management-System/issues) with:
- Clear description
- Steps to reproduce
- Expected vs. actual behavior
- Screenshots if applicable

---

## 📖 Documentation

- **[System Requirements Specification (SRS)](docs/SRS.md)** — Detailed feature specifications
- **[Grading Rules Reference](docs/GRADING.md)** — Complete grading system details
- **[Database Schema](docs/DATABASE.md)** — Entity relationships and migrations
- **[Development Guide](docs/DEVELOPMENT.md)** — Architecture & code organization
- **[Deployment Guide](docs/DEPLOYMENT.md)** — Installation & configuration

---

## 🆘 Getting Help

- **Documentation** — Check the [docs](docs/) folder
- **Issues & Discussions** — [GitHub Discussions](https://github.com/Hussain-Hakimi/Maktab-Management-System/discussions)
- **Report a Bug** — [GitHub Issues](https://github.com/Hussain-Hakimi/Maktab-Management-System/issues)

---

## 📄 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

**You're free to:**
- ✅ Use commercially
- ✅ Modify & distribute
- ✅ Use privately
- ⚠️ Include license & copyright notice

---

## 🙏 Acknowledgments

Built with dedication for Afghan schools and educational institutions. Special thanks to:

- Contributors and testers
- Afghan educators who provided feedback
- Open-source community

---

## 📞 Contact & Links

| Platform | Link |
|----------|------|
| **GitHub** | [Hussain-Hakimi/Maktab-Management-System](https://github.com/Hussain-Hakimi/Maktab-Management-System) |
| **Developer** | [Hussain Hakimi](https://github.com/Hussain-Hakimi) |
| **Issues** | [Report here](https://github.com/Hussain-Hakimi/Maktab-Management-System/issues) |

---

## ⭐ Show Your Support

If Maktab helps your school, please:
- ⭐ **Star** this repository
- 🔄 **Share** with others
- 🐛 **Report issues** to help us improve
- 💡 **Suggest features** you need

---

<div align="center">

**Made with ❤️ for Afghan Schools**

*Bringing reliable technology to education*

![Version](https://img.shields.io/badge/Version-1.5.0-blue)
![Status](https://img.shields.io/badge/Status-Active-green)
![Last Updated](https://img.shields.io/badge/Updated-2026-orange)

</div>
