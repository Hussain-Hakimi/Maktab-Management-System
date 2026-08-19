namespace Maktab.Infrastructure.Persistence;

internal static class SchemaSql
{
    public const string Script = @"
CREATE TABLE IF NOT EXISTS tbl_Classes (
    ClassID INTEGER PRIMARY KEY AUTOINCREMENT,
    GradeName TEXT NOT NULL,
    NumberOfSubjects INTEGER NOT NULL CHECK (NumberOfSubjects >= 0)
);

CREATE TABLE IF NOT EXISTS tbl_Subjects (
    SubjectID INTEGER PRIMARY KEY AUTOINCREMENT,
    SubjectName TEXT NOT NULL,
    ClassID INTEGER NOT NULL,
    FOREIGN KEY (ClassID) REFERENCES tbl_Classes(ClassID) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS tbl_Students (
    StudentID INTEGER PRIMARY KEY AUTOINCREMENT,
    FirstName TEXT NOT NULL,
    LastName TEXT NOT NULL,
    FatherName TEXT NOT NULL,
    ClassID INTEGER NOT NULL,
    RollNumber TEXT NOT NULL,
    RegistrationDate TEXT NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    FOREIGN KEY (ClassID) REFERENCES tbl_Classes(ClassID) ON DELETE RESTRICT,
    UNIQUE (ClassID, RollNumber)
);

CREATE TABLE IF NOT EXISTS tbl_ExamMarks (
    MarkID INTEGER PRIMARY KEY AUTOINCREMENT,
    StudentID INTEGER NOT NULL,
    SubjectID INTEGER NOT NULL,
    MidtermScore REAL NOT NULL CHECK (MidtermScore >= 0 AND MidtermScore <= 40),
    FinalScore REAL NOT NULL CHECK (FinalScore >= 0 AND FinalScore <= 60),
    TotalScore REAL NOT NULL CHECK (TotalScore >= 0 AND TotalScore <= 100),
    FOREIGN KEY (StudentID) REFERENCES tbl_Students(StudentID) ON DELETE CASCADE,
    FOREIGN KEY (SubjectID) REFERENCES tbl_Subjects(SubjectID) ON DELETE CASCADE,
    UNIQUE (StudentID, SubjectID)
);

CREATE TABLE IF NOT EXISTS tbl_Attendance (
    AttendanceID INTEGER PRIMARY KEY AUTOINCREMENT,
    StudentID INTEGER NOT NULL,
    AttendanceDate TEXT NOT NULL,
    Status TEXT NOT NULL CHECK (Status IN ('Present', 'Absent', 'Ill', 'Permission')),
    FOREIGN KEY (StudentID) REFERENCES tbl_Students(StudentID) ON DELETE CASCADE,
    UNIQUE (StudentID, AttendanceDate)
);

CREATE INDEX IF NOT EXISTS idx_attendance_date ON tbl_Attendance(AttendanceDate);

CREATE TABLE IF NOT EXISTS tbl_Books (
    BookID INTEGER PRIMARY KEY AUTOINCREMENT,
    Title TEXT NOT NULL,
    Author TEXT NOT NULL,
    ISBN TEXT,
    Category TEXT,
    TotalCopies INTEGER NOT NULL CHECK (TotalCopies >= 0),
    AvailableCopies INTEGER NOT NULL CHECK (AvailableCopies >= 0 AND AvailableCopies <= TotalCopies)
);

CREATE TABLE IF NOT EXISTS tbl_BookIssues (
    IssueID INTEGER PRIMARY KEY AUTOINCREMENT,
    BookID INTEGER NOT NULL,
    StudentID INTEGER NOT NULL,
    IssueDate TEXT NOT NULL,
    DueDate TEXT NOT NULL,
    ReturnDate TEXT,
    Status TEXT NOT NULL CHECK (Status IN ('Issued', 'Returned')),
    FOREIGN KEY (BookID) REFERENCES tbl_Books(BookID) ON DELETE RESTRICT,
    FOREIGN KEY (StudentID) REFERENCES tbl_Students(StudentID) ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS idx_bookissues_status ON tbl_BookIssues(Status);
CREATE INDEX IF NOT EXISTS idx_bookissues_due ON tbl_BookIssues(DueDate);

CREATE TABLE IF NOT EXISTS tbl_Textbooks (
    TextbookID INTEGER PRIMARY KEY AUTOINCREMENT,
    Title TEXT NOT NULL,
    Subject TEXT,
    ClassID INTEGER,
    TotalCopies INTEGER NOT NULL CHECK (TotalCopies >= 0),
    AvailableCopies INTEGER NOT NULL CHECK (AvailableCopies >= 0 AND AvailableCopies <= TotalCopies),
    FOREIGN KEY (ClassID) REFERENCES tbl_Classes(ClassID) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS tbl_TextbookIssues (
    IssueID INTEGER PRIMARY KEY AUTOINCREMENT,
    TextbookID INTEGER NOT NULL,
    StudentID INTEGER NOT NULL,
    IssueDate TEXT NOT NULL,
    ReturnDate TEXT,
    Status TEXT NOT NULL CHECK (Status IN ('Issued', 'Returned')),
    FOREIGN KEY (TextbookID) REFERENCES tbl_Textbooks(TextbookID) ON DELETE RESTRICT,
    FOREIGN KEY (StudentID) REFERENCES tbl_Students(StudentID) ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS idx_textbookissues_status ON tbl_TextbookIssues(Status);

CREATE TABLE IF NOT EXISTS tbl_Fees (
    FeeID INTEGER PRIMARY KEY AUTOINCREMENT,
    StudentID INTEGER NOT NULL,
    FeeType TEXT NOT NULL,
    Amount REAL NOT NULL CHECK (Amount > 0),
    DueDate TEXT NOT NULL,
    CreatedDate TEXT NOT NULL,
    FOREIGN KEY (StudentID) REFERENCES tbl_Students(StudentID) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS tbl_FeePayments (
    PaymentID INTEGER PRIMARY KEY AUTOINCREMENT,
    FeeID INTEGER NOT NULL,
    StudentID INTEGER NOT NULL,
    Amount REAL NOT NULL CHECK (Amount > 0),
    PaymentDate TEXT NOT NULL,
    ReceiptNumber TEXT NOT NULL,
    FOREIGN KEY (FeeID) REFERENCES tbl_Fees(FeeID) ON DELETE CASCADE,
    FOREIGN KEY (StudentID) REFERENCES tbl_Students(StudentID) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_feepayments_fee ON tbl_FeePayments(FeeID);
CREATE INDEX IF NOT EXISTS idx_feepayments_student ON tbl_FeePayments(StudentID);

CREATE TABLE IF NOT EXISTS tbl_AuditLog (
    LogID INTEGER PRIMARY KEY AUTOINCREMENT,
    UserName TEXT NOT NULL,
    Action TEXT NOT NULL,
    Timestamp TEXT NOT NULL DEFAULT (CURRENT_TIMESTAMP)
);
";
}
