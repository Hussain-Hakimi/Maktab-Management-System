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

CREATE TABLE IF NOT EXISTS tbl_AuditLog (
    LogID INTEGER PRIMARY KEY AUTOINCREMENT,
    UserName TEXT NOT NULL,
    Action TEXT NOT NULL,
    Timestamp TEXT NOT NULL DEFAULT (CURRENT_TIMESTAMP)
);
";
}