using Maktab.Domain.Entities;

namespace Maktab.Application.Abstractions;

public interface IFeeRepository
{
    Task<IReadOnlyList<FeeRecord>> GetFeeRecordsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeeRecord>> GetFeeRecordsByStudentAsync(int studentId, CancellationToken cancellationToken = default);
    Task<FeeRecord?> GetFeeRecordByIdAsync(int feeId, CancellationToken cancellationToken = default);
    Task<int> CreateFeeRecordAsync(FeeRecord fee, CancellationToken cancellationToken = default);
    Task DeleteFeeRecordAsync(int feeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FeePayment>> GetPaymentsByFeeAsync(int feeId, CancellationToken cancellationToken = default);
    Task<int> CreatePaymentAsync(FeePayment payment, CancellationToken cancellationToken = default);
    Task SetReceiptNumberAsync(int paymentId, string receiptNumber, CancellationToken cancellationToken = default);
}
