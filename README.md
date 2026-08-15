# Afghan School Management System

A **100% offline** Windows desktop application for managing student records, marks, attendance, report cards, library resources, and fees in a single-laptop environment — built for rural schools in Afghanistan with no reliable internet or power.

> Status: Final Draft SRS approved for development · Version 1.0 · August 2026

---

## Table of Contents

- [Why This Exists](#why-this-exists)
- [Key Features](#key-features)
- [How It Works](#how-it-works)
- [Grading & Promotion Rules](#grading--promotion-rules)
- [Report Card Contents](#report-card-contents)
- [Database Schema](#database-schema)
- [User Roles](#user-roles)
- [Non-Functional Requirements](#non-functional-requirements)
- [Installation](#installation)
- [Roadmap](#roadmap)
- [Success Criteria](#success-criteria)
- [License](#license)

---

## Why This Exists

Remote, mountainous schools in Afghanistan face a specific set of constraints that most school-management software ignores:

- **No internet, unreliable power** — cloud-based tools and centralized databases are not an option.
- **Manual record keeping** — marks, attendance, and report cards are all handwritten, which is slow and error-prone.
- **Data loss risk** — paper records can be damaged, lost, or degrade over time.
- **No transparency** — pass/fail and promotion decisions are calculated by hand.
- **No attendance system** — there's no reliable way to check if a student with 30+ absences is eligible for promotion.
- **Manual resource tracking** — library books and textbooks are tracked (or not tracked) on paper.

This project is a single-laptop, single-folder desktop application designed specifically around these constraints — no installer, no internet, no server, and resilient to sudden power loss.

## Key Features

- **Fully offline** — every feature works with zero network connectivity.
- **Single-folder install** — copy the folder to the desktop and run; no installer or runtime setup.
- **Power-outage resistant** — SQLite in WAL mode plus auto-save keeps data loss to a maximum of ~5 minutes of work.
- **Automated calculations** — totals, percentages, letter grades, and promotion status are computed automatically, removing manual math errors.
- **One-click PDF report cards** — generates a print-ready report card per student in under 5 seconds.
- **Attendance tracking** — daily present/absent recording with automatic absence totals tied into promotion rules.
- **Library & textbook management** — issue/return tracking with overdue reports.
- **Fee tracking** — record payments and generate paid/unpaid reports.
- **Automated backups** — daily scheduled backups with 7-day retention, plus manual backup and restore.
- **Persian/Dari, right-to-left UI** — designed for teachers who are not technically trained, with large fonts and minimal clicks.

## How It Works

1. **Set up school structure** — define grades (1–12) and the subjects for each grade.
2. **Register students** — name, father's name, grade, roll number, registration date.
3. **Enter marks** — midterm (0–40) and final (0–60) per subject; totals and pass/fail status calculate automatically.
4. **Record attendance** — mark present/absent per student, per day; absences accumulate automatically.
5. **Generate report cards** — one click produces a formatted PDF with marks, summary, and promotion status.
6. **Track resources** — issue/return library books and textbooks, and record fee payments.
7. **Stay backed up** — the system auto-backs up daily and after every transaction, with manual backup/restore available at any time.

## Grading & Promotion Rules

Each subject is scored out of 100, combining a midterm and a final exam:

```
Total Marks = Midterm (0–40) + Final (0–60)
Percentage  = (Total / 100) × 100%
```

| Percentage Range | Letter Grade | Description       |
|-------------------|--------------|--------------------|
| 90–100%           | A            | Excellent          |
| 80–89%             | B            | Good               |
| 70–79%             | C            | Satisfactory       |
| 60–69%             | D            | Acceptable         |
| 50–59%             | E            | Poor (but pass)    |
| < 40%              | F            | **Fail**           |

**Promotion decision:**

```
IF (subjects with Total < 40) ≤ 3   →  PROMOTED ✓
IF (subjects with Total < 40) > 3   →  NOT PROMOTED ✗ (repeat grade)

IF (Total Absence Days) > 30        →  NOT PROMOTED ✗ (regardless of marks)
IF (Total Absence Days) ≤ 30        →  Promotion depends on marks

Final Status = PROMOTED  ⇔  (Failed Subjects ≤ 3) AND (Absence Days ≤ 30)
```

## Report Card Contents

Each generated PDF ("Ittila'a Nama") includes:

- **Header** — school name, academic year (Afghan calendar), issue date
- **Student info** — full name, father's name, roll number, grade
- **Marks table** — subject, midterm, final, total, percentage, letter grade, pass/fail
- **Summary** — subjects passed/failed, attendance totals, final promotion status
- **Failure reason** (if applicable) — e.g. "> 30 days absence" or "> 3 failed subjects"
- **Signature section** — space for teacher and principal signature/seal

Files are saved automatically to `Reports/` using the format:
`{StudentName}_{StudentID}_{YearCode}.pdf` (e.g. `Ahmad_Ali_0001_1402.pdf`)

## Database Schema

| Table | Key Columns | Purpose |
|---|---|---|
| `tbl_Classes` | ClassID, GradeName, NumberOfSubjects | Grade definitions |
| `tbl_Subjects` | SubjectID, SubjectName, ClassID | Subjects per grade |
| `tbl_Students` | StudentID, FirstName, LastName, FatherName, ClassID, RollNumber | Student records |
| `tbl_ExamMarks` | MarkID, StudentID, SubjectID, MidtermScore, FinalScore, TotalScore | Exam results |
| `tbl_Attendance` | AttendanceID, StudentID, Date, IsPresent | Daily attendance |
| `tbl_LibraryBooks` | BookID, BookTitle, Author, Quantity | Library inventory |
| `tbl_LibraryTransactions` | TransactionID, StudentID, BookID, IssueDate, DueDate, ReturnDate | Borrowing records |
| `tbl_TextbookDistribution` | DistributionID, StudentID, BookTitle, Year, IsReturned | Textbook tracking |
| `tbl_Fees` | FeeID, StudentID, Amount, PaidDate, Year | Fee payments |
| `tbl_AuditLog` | LogID, UserName, Action, Timestamp | Change history |

## User Roles

| Role | Description | Access |
|---|---|---|
| **Administrator** | Principal / senior teacher | Full access: grades/subjects, students, backup/restore, logs |
| **Teacher** | Subject teacher | Enter marks & attendance, generate report cards; cannot delete students or edit other teachers' marks |
| **Librarian** *(v1.1)* | Library manager | Manage books, issue/return, overdue reports |
| **Accountant** *(v1.1)* | Fee collector | Record and report on fee payments |

Access control rolls out gradually:
- **v1.0** — no login, full access for all users
- **v1.1** — simple username + 4-digit PIN login
- **v2.0** — full role-based access control

## Non-Functional Requirements

- **Performance** — student list loads in < 2s (100 students), class-wide mark calculation in < 1s, PDF report card generation in < 5s, tuned for low-spec hardware (2–4 GB RAM).
- **Reliability** — SQLite WAL mode; database corruption resistant to sudden power loss; validated required fields; unique roll numbers enforced; audit trail on all critical changes.
- **Usability** — full Persian/Dari, right-to-left UI; B Nazanin/Vazir fonts at 12pt default with zoom; minimal-click design with per-screen help.
- **Portability** — single-folder, portable install; runs from a USB drive; no runtime dependencies; Windows 7+ (32-bit and 64-bit).
- **Security** — optional password protection (min. 4 characters); database and backups writable only by the application.
- **Maintainability** — documented C# codebase with clear naming and clean class/method structure; all errors logged to `Logs/error.log`.

## Installation

1. Copy the application folder to the desktop (or any local location, including a USB drive).
2. Double-click the executable — no installer, runtime, or internet connection required.
3. To uninstall, delete the folder.

## Roadmap

**v1.0 — MVP**
- Grade & subject definition
- Student registration
- Mark entry with auto-calculation
- PDF report card generation
- Auto-backup on power loss

**v1.1 — Extended**
- Daily attendance tracking
- Library & textbook management
- Fee collection tracking

**v2.0 — Advanced**
- User login & role-based access control
- Advanced reporting & analytics
- Multi-user concurrent access
- Excel export

## Success Criteria

- Fully operational with zero internet connectivity
- Single-folder installation on a Windows laptop
- Report card generation in under 5 seconds
- No data loss during power outages
- All calculations automated — no manual math
- Persian/Dari interface with full RTL support
- Usable by non-technical teachers without training
- Automated daily backups with 7-day retention

## License

_Add your chosen license here (e.g. MIT, GPL-3.0)._

---

*Built for schools where connectivity isn't a given — every feature works fully offline, on a single laptop, with no technical support required.*
