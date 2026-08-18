namespace Maktab.Application.Abstractions;

public interface IFeeService
{
    Task<IReadOnlyList<FeeRecordDto>> GetFeeRecordsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeeRecordDto>> GetStudentFeesAsync(int studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeeRecordDto>> GetOutstandingFeesAsync(CancellationToken cancellationToken = default);
    Task<int> CreateFeeRecordAsync(int studentId, string title, decimal amountDue, DateOnly dueDate, string academicYear, CancellationToken cancellationToken = default);
    Task DeleteFeeRecordAsync(int feeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FeePaymentDto>> GetPaymentsAsync(int feeId, CancellationToken cancellationToken = default);
    Task<FeePaymentDto> RecordPaymentAsync(int feeId, decimal amount, CancellationToken cancellationToken = default);
    Task<decimal> GetStudentOutstandingBalanceAsync(int studentId, CancellationToken cancellationToken = default);
}
