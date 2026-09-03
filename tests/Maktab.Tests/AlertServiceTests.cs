using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.Tests;

public class AlertServiceTests
{
    private sealed class MockBookService : IBookService
    {
        public List<BookIssueDto> OverdueIssues { get; set; } = [];

        public Task<IReadOnlyList<BookDto>> GetBooksAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BookDto>>(new List<BookDto>());

        public Task<int> AddBookAsync(SaveBookDto book, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateBookAsync(int bookId, SaveBookDto book, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteBookAsync(int bookId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<BookIssueDto>> GetIssuesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BookIssueDto>>(new List<BookIssueDto>());
        public Task<IReadOnlyList<BookIssueDto>> GetOverdueIssuesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BookIssueDto>>(OverdueIssues);
        public Task<int> IssueBookAsync(IssueBookDto issue, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task ReturnBookAsync(ReturnBookDto returnInfo, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class MockFeeService : IFeeService
    {
        public List<FeeDto> Fees { get; set; } = [];

        public Task<IReadOnlyList<FeeDto>> GetFeesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FeeDto>>(Fees);
        public Task<int> AddFeeAsync(SaveFeeDto fee, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteFeeAsync(int feeId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<FeePaymentDto>> GetPaymentsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> RecordPaymentAsync(RecordPaymentDto payment, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class MockAttendanceService : IAttendanceService
    {
        public List<StudentAttendanceDto> DateAttendance { get; set; } = [];
        public List<StudentAttendanceSummaryDto> Summaries { get; set; } = [];

        public Task<IReadOnlyList<StudentAttendanceDto>> GetClassAttendanceForDateAsync(int classId, DateTime date, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StudentAttendanceDto>>(DateAttendance);

        public Task SaveAttendanceBatchAsync(IEnumerable<SaveAttendanceDto> attendance, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> GetStudentAbsenceDaysAsync(int studentId, string academicYear, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<StudentAttendanceSummaryDto?> GetStudentAttendanceSummaryAsync(int studentId, int academicYearId, CancellationToken cancellationToken = default)
        {
            var summary = Summaries.FirstOrDefault(s => s.StudentId == studentId);
            return Task.FromResult(summary);
        }

        public Task<IReadOnlyList<StudentAttendanceSummaryDto>> GetClassAttendanceSummaryAsync(int classId, int academicYearId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StudentAttendanceSummaryDto>>(Summaries);

        public Task<IReadOnlyList<MonthlyAttendanceRowDto>> GetMonthlyAttendanceReportAsync(int classId, int year, int month, int academicYearId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MonthlyAttendanceRowDto>>(new List<MonthlyAttendanceRowDto>());
    }

    private sealed class MockAcademicYearService : IAcademicYearService
    {
        public AcademicYearDto? ActiveYear { get; set; }

        public Task<AcademicYearDto?> GetActiveAcademicYearAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ActiveYear);
        public Task<IReadOnlyList<AcademicYearDto>> GetAllAcademicYearsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> CreateAcademicYearAsync(SaveAcademicYearDto dto, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task SetActiveAcademicYearAsync(int academicYearId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class MockStudentService : IStudentService
    {
        public List<Student> Students { get; set; } = [];

        public Task<IReadOnlyList<Student>> GetAllStudentsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Student>>(Students);
        public Task<IReadOnlyList<Student>> GetStudentsByClassAsync(int classId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Student?> GetStudentByIdAsync(int studentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> RegisterStudentAsync(string firstName, string lastName, string fatherName, int classId, string rollNumber, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateStudentAsync(int studentId, string firstName, string lastName, string fatherName, int classId, string rollNumber, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task RemoveStudentAsync(int studentId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> GetNextRollNumberAsync(int classId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    [Fact]
    public async Task GetAlerts_ReturnsOverdueBookAndFeeAlerts()
    {
        // Arrange
        var bookService = new MockBookService
        {
            OverdueIssues = new List<BookIssueDto>
            {
                new() { IssueId = 1, BookTitle = "Math Book", StudentName = "Ali", RollNumber = "101", DueDate = DateTime.Today.AddDays(-10) }
            }
        };
        var feeService = new MockFeeService
        {
            Fees = new List<FeeDto>
            {
                new() { FeeId = 1, FeeType = "Tuition", StudentName = "Sara", RollNumber = "102", Amount = 1000m, TotalPaid = 200m, AcademicYearId = 1 }
            }
        };
        var attendanceService = new MockAttendanceService();
        var academicYearService = new MockAcademicYearService { ActiveYear = new AcademicYearDto { AcademicYearId = 1, YearName = "۱۴۰۴ - ۱۴۰۵" } };
        var studentService = new MockStudentService { Students = new List<Student>() };

        var alertService = new AlertService(bookService, feeService, attendanceService, academicYearService, studentService);

        // Act
        var alerts = await alertService.GetAlertsAsync();

        // Assert
        Assert.Equal(2, alerts.Count);
        Assert.Contains(alerts, a => a.Type == "OverdueBook");
        Assert.Contains(alerts, a => a.Type == "OutstandingFee");
    }

    [Fact]
    public async Task GetAlerts_WhenNoData_ReturnsEmptyList()
    {
        var bookService = new MockBookService();
        var feeService = new MockFeeService();
        var attendanceService = new MockAttendanceService();
        var academicYearService = new MockAcademicYearService { ActiveYear = null };
        var studentService = new MockStudentService();

        var alertService = new AlertService(bookService, feeService, attendanceService, academicYearService, studentService);
        var alerts = await alertService.GetAlertsAsync();

        Assert.Empty(alerts);
    }
}
