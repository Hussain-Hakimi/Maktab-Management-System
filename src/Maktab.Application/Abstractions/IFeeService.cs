namespace Maktab.Application.Abstractions;

public interface IFeeService
{
    Task<IReadOnlyList<FeeRecordDto>> GetAllFeesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeeRecordDto>> GetFeesByStudentAsync(int studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeeRecordDto>> GetOutstandingFeesAsync(int? classId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentFeeSummaryDto>> GetStudentFeeSummariesAsync(int? classId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeePaymentDto>> GetPaymentsByFeeAsync(int feeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeePaymentDto>> GetPaymentsByStudentAsync(int studentId, CancellationToken cancellationToken = default);

    Task<int> CreateFeeRecordAsync(int studentId, string title, decimal amountDue, DateOnly? dueDate, string? academicYear, CancellationToken cancellationToken = default);
    Task<FeePaymentDto> RecordPaymentAsync(int feeId, decimal amount, CancellationToken cancellationToken = default);
    Task RemoveFeeRecordAsync(int feeId, CancellationToken cancellationToken = default);
}
