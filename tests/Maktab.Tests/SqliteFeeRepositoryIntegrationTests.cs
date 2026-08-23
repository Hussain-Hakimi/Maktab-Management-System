using Microsoft.Data.Sqlite;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Infrastructure.Persistence;

namespace Maktab.Tests;

public class SqliteFeeRepositoryIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly AppFolders _folders;
    private readonly ConnectionStringProvider _connectionStringProvider;
    private readonly SqliteFeeRepository _feeRepository;
    private readonly SqliteStudentRepository _studentRepository;
    private readonly SqliteClassSubjectRepository _classSubjectRepository;

    public SqliteFeeRepositoryIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MaktabFeeTests_" + Guid.NewGuid());
        _folders = new AppFolders(
            Root: _tempDir,
            Data: Path.Combine(_tempDir, "Data"),
            Logs: Path.Combine(_tempDir, "Logs"),
            Backups: Path.Combine(_tempDir, "Backups"),
            Reports: Path.Combine(_tempDir, "Reports"),
            Logos: Path.Combine(_tempDir, "Logos"));

        DirectoryBootstrapper.EnsureFoldersExist(_folders);

        _connectionStringProvider = new ConnectionStringProvider(_folders);
        var initializer = new SqliteDatabaseInitializer(_connectionStringProvider);
        initializer.InitializeAsync().GetAwaiter().GetResult();

        _feeRepository = new SqliteFeeRepository(_connectionStringProvider);
        _studentRepository = new SqliteStudentRepository(_connectionStringProvider);
        _classSubjectRepository = new SqliteClassSubjectRepository(_connectionStringProvider);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public async Task FeeCrud_WorksEndToEnd()
    {
        var classId = await _classSubjectRepository.CreateClassAsync(new SchoolClass { GradeName = "صنف هفتم", NumberOfSubjects = 8 });
        var studentId = await _studentRepository.CreateStudentAsync(new Student
        {
            FirstName = "Ahmad", LastName = "Karimi", FatherName = "Mohammad", ClassId = classId, RollNumber = "101"
        });

        var feeId = await _feeRepository.CreateFeeAsync(new Fee
        {
            StudentId = studentId,
            FeeType = "Tuition",
            Amount = 1000m,
            DueDate = DateTime.Today.AddDays(30),
            CreatedDate = DateTime.Now
        });

        Assert.True(feeId > 0);

        var fees = await _feeRepository.GetFeesAsync();
        Assert.Single(fees);
        Assert.Equal(1000m, fees[0].Amount);
        Assert.Equal(0m, fees[0].TotalPaid);

        await _feeRepository.DeleteFeeAsync(feeId);
        fees = await _feeRepository.GetFeesAsync();
        Assert.Empty(fees);
    }

    [Fact]
    public async Task RecordPayment_UpdatesTotalPaidAndStatus()
    {
        var classId = await _classSubjectRepository.CreateClassAsync(new SchoolClass { GradeName = "صنف هفتم", NumberOfSubjects = 8 });
        var studentId = await _studentRepository.CreateStudentAsync(new Student
        {
            FirstName = "Ahmad", LastName = "Karimi", FatherName = "Mohammad", ClassId = classId, RollNumber = "101"
        });

        var feeId = await _feeRepository.CreateFeeAsync(new Fee
        {
            StudentId = studentId,
            FeeType = "Tuition",
            Amount = 1000m,
            DueDate = DateTime.Today.AddDays(30),
            CreatedDate = DateTime.Now
        });

        await _feeRepository.RecordPaymentAsync(new FeePayment
        {
            FeeId = feeId,
            StudentId = studentId,
            Amount = 600m,
            PaymentDate = DateTime.Today,
            ReceiptNumber = "RCP-001"
        });

        var fees = await _feeRepository.GetFeesAsync();
        Assert.Equal(600m, fees[0].TotalPaid);
        Assert.Equal(400m, fees[0].Outstanding);
        Assert.Equal(Domain.Enums.FeeStatus.Partial, fees[0].Status);

        var totalPaid = await _feeRepository.GetTotalPaidByFeeAsync(feeId);
        Assert.Equal(600m, totalPaid);
    }

    [Fact]
    public async Task DeleteStudent_CascadesFeesAndPayments()
    {
        var classId = await _classSubjectRepository.CreateClassAsync(new SchoolClass { GradeName = "صنف هفتم", NumberOfSubjects = 8 });
        var studentId = await _studentRepository.CreateStudentAsync(new Student
        {
            FirstName = "Ahmad", LastName = "Karimi", FatherName = "Mohammad", ClassId = classId, RollNumber = "101"
        });

        var feeId = await _feeRepository.CreateFeeAsync(new Fee
        {
            StudentId = studentId,
            FeeType = "Tuition",
            Amount = 500m,
            DueDate = DateTime.Today,
            CreatedDate = DateTime.Now
        });

        await _feeRepository.RecordPaymentAsync(new FeePayment
        {
            FeeId = feeId,
            StudentId = studentId,
            Amount = 200m,
            PaymentDate = DateTime.Today,
            ReceiptNumber = "RCP-002"
        });

        await _studentRepository.DeleteStudentAsync(studentId);

        var fees = await _feeRepository.GetFeesAsync();
        var payments = await _feeRepository.GetPaymentsAsync();
        Assert.Empty(fees);
        Assert.Empty(payments);
    }
}
