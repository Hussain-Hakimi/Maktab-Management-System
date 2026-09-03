namespace Maktab.Infrastructure.Persistence;

internal sealed record DatabaseMigration(int Version, string Sql);

internal static class DatabaseMigrations
{
    public const string BaselineSql = SchemaSql.Script;

    public static IReadOnlyList<DatabaseMigration> GetMigrations()
    {
        return new List<DatabaseMigration>
        {
            new(2, @"
CREATE TABLE IF NOT EXISTS tbl_Users (
    UserID INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    FullName TEXT NOT NULL,
    Role TEXT NOT NULL CHECK (Role IN ('Admin', 'Teacher', 'Librarian', 'Accountant')),
    IsActive INTEGER NOT NULL CHECK (IsActive IN (0, 1))
);"),
            new(3, @"
CREATE TABLE IF NOT EXISTS tbl_Settings (
    SettingID INTEGER PRIMARY KEY AUTOINCREMENT,
    Key TEXT NOT NULL UNIQUE,
    Value TEXT NOT NULL
);"),
            new(4, @"
CREATE TABLE IF NOT EXISTS tbl_AcademicYears (
    AcademicYearID INTEGER PRIMARY KEY AUTOINCREMENT,
    YearName TEXT NOT NULL UNIQUE,
    StartDate TEXT NOT NULL,
    EndDate TEXT NOT NULL,
    IsActive INTEGER NOT NULL CHECK (IsActive IN (0, 1))
);
ALTER TABLE tbl_ExamMarks ADD COLUMN AcademicYearId INTEGER NOT NULL DEFAULT 0;
ALTER TABLE tbl_Attendance ADD COLUMN AcademicYearId INTEGER NOT NULL DEFAULT 0;
ALTER TABLE tbl_Fees ADD COLUMN AcademicYearId INTEGER NOT NULL DEFAULT 0;
"),
            new(5, @"
CREATE TABLE IF NOT EXISTS tbl_StudentPromotionHistory (
    PromotionID INTEGER PRIMARY KEY AUTOINCREMENT,
    StudentID INTEGER NOT NULL,
    FromClassID INTEGER NOT NULL,
    ToClassID INTEGER,
    AcademicYearID INTEGER NOT NULL,
    Result TEXT NOT NULL,
    PromotionDate TEXT NOT NULL,
    FOREIGN KEY (StudentID) REFERENCES tbl_Students(StudentID) ON DELETE CASCADE,
    FOREIGN KEY (FromClassID) REFERENCES tbl_Classes(ClassID) ON DELETE RESTRICT,
    FOREIGN KEY (ToClassID) REFERENCES tbl_Classes(ClassID) ON DELETE RESTRICT,
    FOREIGN KEY (AcademicYearID) REFERENCES tbl_AcademicYears(AcademicYearID) ON DELETE RESTRICT
);
"),
            new(6, @"
CREATE TABLE IF NOT EXISTS tbl_TeacherSubjects (
    TeacherSubjectID INTEGER PRIMARY KEY AUTOINCREMENT,
    TeacherUserID INTEGER NOT NULL,
    ClassID INTEGER NOT NULL,
    SubjectID INTEGER NOT NULL,
    FOREIGN KEY (TeacherUserID) REFERENCES tbl_Users(UserID) ON DELETE CASCADE,
    FOREIGN KEY (ClassID) REFERENCES tbl_Classes(ClassID) ON DELETE CASCADE,
    FOREIGN KEY (SubjectID) REFERENCES tbl_Subjects(SubjectID) ON DELETE CASCADE,
    UNIQUE (TeacherUserID, ClassID, SubjectID)
);

CREATE TABLE IF NOT EXISTS tbl_ClassGuardians (
    ClassGuardianID INTEGER PRIMARY KEY AUTOINCREMENT,
    TeacherUserID INTEGER NOT NULL,
    ClassID INTEGER NOT NULL,
    FOREIGN KEY (TeacherUserID) REFERENCES tbl_Users(UserID) ON DELETE CASCADE,
    UNIQUE (TeacherUserID, ClassID)
);
"),
            new(7, @"
CREATE TABLE IF NOT EXISTS tbl_Exams (
    ExamID INTEGER PRIMARY KEY AUTOINCREMENT,
    SubjectID INTEGER NOT NULL,
    ClassID INTEGER NOT NULL,
    AcademicYearID INTEGER NOT NULL,
    ExamType TEXT NOT NULL CHECK (ExamType IN ('Midterm', 'Final')),
    ExamDate TEXT NOT NULL,
    CreatedByTeacherUserID INTEGER NOT NULL,
    FOREIGN KEY (SubjectID) REFERENCES tbl_Subjects(SubjectID) ON DELETE CASCADE,
    FOREIGN KEY (ClassID) REFERENCES tbl_Classes(ClassID) ON DELETE CASCADE,
    FOREIGN KEY (AcademicYearID) REFERENCES tbl_AcademicYears(AcademicYearID) ON DELETE CASCADE,
    FOREIGN KEY (CreatedByTeacherUserID) REFERENCES tbl_Users(UserID) ON DELETE CASCADE
);
"),
            new(8, @"
CREATE TABLE IF NOT EXISTS tbl_ClassFinalizations (
    ClassFinalizationID INTEGER PRIMARY KEY AUTOINCREMENT,
    ClassID INTEGER NOT NULL,
    AcademicYearID INTEGER NOT NULL,
    IsFinalized INTEGER NOT NULL CHECK (IsFinalized IN (0, 1)),
    FinalizedByTeacherUserID INTEGER NOT NULL,
    FinalizationDate TEXT NOT NULL,
    FOREIGN KEY (ClassID) REFERENCES tbl_Classes(ClassID) ON DELETE CASCADE,
    FOREIGN KEY (AcademicYearID) REFERENCES tbl_AcademicYears(AcademicYearID) ON DELETE CASCADE,
    FOREIGN KEY (FinalizedByTeacherUserID) REFERENCES tbl_Users(UserID) ON DELETE CASCADE,
    UNIQUE (ClassID, AcademicYearID)
);
"),
            new(9, @"
BEGIN TRANSACTION;

ALTER TABLE tbl_ExamMarks RENAME TO tbl_ExamMarks_Legacy;

CREATE TABLE tbl_ExamMarks (
    MarkID INTEGER PRIMARY KEY AUTOINCREMENT,
    StudentID INTEGER NOT NULL,
    SubjectID INTEGER NOT NULL,
    MidtermScore REAL NOT NULL CHECK (MidtermScore >= 0 AND MidtermScore <= 40),
    FinalScore REAL NOT NULL CHECK (FinalScore >= 0 AND FinalScore <= 60),
    TotalScore REAL NOT NULL CHECK (TotalScore >= 0 AND TotalScore <= 100),
    AcademicYearId INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (StudentID) REFERENCES tbl_Students(StudentID) ON DELETE CASCADE,
    FOREIGN KEY (SubjectID) REFERENCES tbl_Subjects(SubjectID) ON DELETE CASCADE,
    UNIQUE (StudentID, SubjectID, AcademicYearId)
);

INSERT INTO tbl_ExamMarks (MarkID, StudentID, SubjectID, MidtermScore, FinalScore, TotalScore, AcademicYearId)
SELECT MarkID, StudentID, SubjectID, MidtermScore, FinalScore, TotalScore, AcademicYearId
FROM tbl_ExamMarks_Legacy;

DROP TABLE tbl_ExamMarks_Legacy;

CREATE INDEX IF NOT EXISTS idx_exammarks_year ON tbl_ExamMarks(AcademicYearId);

COMMIT;
"),
            new(10, @"
CREATE TABLE IF NOT EXISTS tbl_StudentAcademicEnrollments (
    EnrollmentID INTEGER PRIMARY KEY AUTOINCREMENT,
    StudentID INTEGER NOT NULL,
    AcademicYearID INTEGER NOT NULL,
    ClassID INTEGER NOT NULL,
    RollNumber TEXT NOT NULL,
    EnrollmentDate TEXT NOT NULL,
    Status TEXT NOT NULL DEFAULT 'Active' CHECK (Status IN ('Active', 'Promoted', 'Transferred', 'Withdrawn', 'Completed')),
    FOREIGN KEY (StudentID) REFERENCES tbl_Students(StudentID) ON DELETE CASCADE,
    FOREIGN KEY (AcademicYearID) REFERENCES tbl_AcademicYears(AcademicYearID) ON DELETE RESTRICT,
    FOREIGN KEY (ClassID) REFERENCES tbl_Classes(ClassID) ON DELETE RESTRICT,
    UNIQUE (StudentID, AcademicYearID)
);

CREATE INDEX IF NOT EXISTS idx_student_enrollments_year ON tbl_StudentAcademicEnrollments(AcademicYearID);
CREATE INDEX IF NOT EXISTS idx_student_enrollments_class_year ON tbl_StudentAcademicEnrollments(ClassID, AcademicYearID);
CREATE INDEX IF NOT EXISTS idx_student_enrollments_student ON tbl_StudentAcademicEnrollments(StudentID);
")
        };
    }
}
