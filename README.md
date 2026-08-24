# 🇦🇫 Maktab Management System

**Offline Afghan School Management System**

A lightweight, offline-first desktop application designed for schools in Afghanistan, especially environments where internet access is limited or unavailable.

Built with **C# / .NET 8**, **WPF**, **SQLite**, and **QuestPDF**, with a Dari/Persian right-to-left interface.

---

## Table of Contents

- [Project Goal](#-project-goal)
- [Version History](#-version-history)
- [Technology Stack](#️-technology-stack)
- [Project Structure](#-project-structure)
- [Grading and Examination System](#-grading-and-examination-system)
- [Student Report Card / اطلاع‌نامه](#-student-report-card--اطلاعنامه)
- [Classes and Subjects](#-classes-and-subjects)
- [Student Management](#-student-management)
- [Attendance](#-attendance)
- [Library](#-library)
- [Textbooks](#-textbooks)
- [Fees](#-fees)
- [Mark Entry](#-mark-entry)
- [User Accounts & Roles](#-user-accounts--roles)
- [School Settings](#-school-settings)
- [Database](#-database)
- [Database Migrations](#-database-migrations)
- [Backup and Restore](#-backup-and-restore)
- [Logging & Audit](#-logging--audit)
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
- Attendance
- Library books and lending
- Textbooks inventory and distribution
- Student fees and payments
- Examination marks
- Automatic grading
- Student report cards
- User accounts and role-based access
- Audit logging
- School settings
- Database backup and restore

The system is designed to work on a single Windows computer without requiring an internet connection or an online server.

---

## 🚀 Version History

### Version 1.0.1 — Stabilization Release

Core academic MVP: classes, subjects, students, marks, grading, report cards, backup/restore, logging, and tests.

### Version 1.1.0 — School Operations Release

Added attendance, library, textbooks, fees, database migration system.

### Version 1.2.0 — Administration & Security Release

Added:

- User accounts with roles (Admin, Teacher, Librarian, Accountant)
- Login window (PBKDF2 password hashing)
- Role-based sidebar navigation
- Change password
- Audit logging (login, user management, and all major operations)
- General school settings with logo upload
- Promotion settings editor
- Dashboard with summary statistics
- Bulk import (students via CSV)

---
### Version 1.4.0 — Alerts & Dashboard Enhancement Release

Added:
- Promotion History Viewer
- Alerts Center (overdue books, outstanding fees, high absence)
- Bulk Import for Marks & Attendance (CSV)
- Enhanced Dashboard with charts:
  - Grade distribution bar chart
  - Attendance trend line chart (last 7 days)
  - Fee collection progress bar
 
---

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

---

## 📁 Project Structure

Maktab-Management-System/
│
├── src/
│ ├── Maktab.Domain/
│ ├── Maktab.Application/
│ ├── Maktab.Infrastructure/
│ └── Maktab.App.Wpf/
│
├── tests/
│ └── Maktab.Tests/
│
├── .github/
│ └── workflows/
│
└── README.md



---

## 📊 Grading and Examination System

### Marks for Each Subject

| Examination | Maximum |
|---|---|
| Midterm | 40 |
| Final | 60 |
| **Total** | **100** |

### 🎓 Student Group / Grade

| Average | Group | Dari |
|---|---|---|
| 90–100 | A | الف |
| 85–89.99 | B | ب |
| 75–84.99 | C | ج |
| 65–74.99 | D | د |
| Below 65 | F | ه |

### Promotion Rule

- Average ≥ 65, 0 failed subjects, absences ≤ 30 → Promoted
- Average ≥ 65, 1–3 failed subjects → Conditional
- Average < 65 or >3 failed subjects or absences > 30 → Repeat

**These values are configurable via Promotion Settings (Admin).**

---

## 📄 Student Report Card / اطلاع‌نامه

Generates PDF report cards containing:

- Student name, father’s name, class, roll number
- Subject marks (midterm, final, total)
- Overall average and grade
- Promotion status
- Absence days (from attendance)
- Signature areas

---

## 🏫 Classes and Subjects

Manage classes and subjects: add, edit, delete, assign subjects to classes.

## 👨‍🎓 Student Management

Student records: ID, first name, last name, father's name, class, roll number, registration date. Duplicate roll numbers prevented.

## 🗓️ Attendance

Daily attendance entry per class with statuses: Present, Absent, Ill, Permission. Absence days integrated into promotion.

## 📚 Library

Books, issue/return, overdue tracking.

## 📦 Textbooks

Textbook inventory, issue/return to students.

## 💰 Fees

Fee records, payment tracking, outstanding balances.

## 📝 Mark Entry

Enter midterm (0–40) and final (0–60) marks; auto total and pass/fail.

## 👤 User Accounts & Roles

- Roles: Admin, Teacher, Librarian, Accountant
- Login with PBKDF2 password hashing
- Role-based sidebar (only allowed items visible)
- Change own password
- Default admin: `admin / admin123`

## 🏫 School Settings

Admin can set school name, address, phone, academic year, and logo (uploaded image stored locally).

## 💾 Database

SQLite local database. All data stored offline.

### Database Migrations

Uses `PRAGMA user_version`. Baseline + migrations:

- v1: initial schema
- v2: users
- v3: settings

Future migrations added in `DatabaseMigrations.GetMigrations()`.

## 🔄 Backup and Restore

Manual/automatic backup, restore, retention. Recommended to copy backups externally.

## 📝 Logging & Audit

File-based logs for errors; database audit trail for user actions (viewable in Admin > Audit Logs).

## 🇦🇫 Dari / Persian RTL Interface

All labels in Dari/Persian, right-to-left.

## 🧪 Testing

xUnit tests for services, repositories, migrations, and integration (real SQLite).

## ⚙️ Build and Run

### Requirements

- Windows
- .NET 8 SDK
- Visual Studio 2022 or compatible

### Clone

```bash
git clone https://github.com/Hussain-Hakimi/Maktab-Management-System.git
cd Maktab-Management-System

### Restore / Build / Test

dotnet restore
dotnet build
dotnet test


bash
dotnet publish src/Maktab.App.Wpf/Maktab.App.Wpf.csproj -c Release -r win-x64 --self-contained true
Copy the whole publish folder to the target computer.

🔁 Continuous Integration
GitHub Actions builds and tests on Windows runner.

🗺️ Development Roadmap
V1.2 (current) — Users, roles, audit, settings, dashboard, bulk import

V1.3 — Advanced reports, import/export for marks/attendance

V2.0 — Multi-user/network, advanced analytics

🔐 Offline-First Design
No internet required for core functionality.

🎯 Design Principles
Simple, offline, reliable, maintainable, localized, extensible.

📌 Current Status
Version: 1.4.0 — Administration & Security Release

The system includes all V1.2 features and is ready for production use.

👨‍💻 Project
Maktab Management System — built with ❤️ for Afghan schools.

text

---

## ✅ Phase 3 Complete

After applying all files, run:

```bash
dotnet build
dotnet test
All tests should pass. The V1.2 release is now complete.
