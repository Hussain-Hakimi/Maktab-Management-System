using Maktab.Domain.Entities;

namespace Maktab.Application.Abstractions;

public interface IFeeRepository
{
    Task<IReadOnlyList<FeeRecord>> GetFeeRecordsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeeRecord>> GetFeeRecordsByStudentAsync(int studentId, CancellationToken cancellationToken = default);
    Task<FeeRecord?> GetFeeRecordByIdAsync(int feeId, CancellationToken cancellationToken = default);
    Task<int> CreateFeeRecordAsync(FeeRecord feeRecord, CancellationToken cancellationToken = default);
    Task DeleteFeeRecordAsync(int feeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FeePayment>> GetPaymentsByFeeAsync(int feeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeePayment>> GetPaymentsByStudentAsync(int studentId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalPaidForFeeAsync(int feeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts the payment and assigns its final receipt number
    /// (RCP-yyyyMMdd-NNNNN, based on the new payment id) in one transaction.
    /// Returns the new payment id.
    /// </summary>
    Task<int> CreatePaymentAsync(FeePayment payment, CancellationToken cancellationToken = default);

    Task<FeePayment?> GetPaymentByIdAsync(int paymentId, CancellationToken cancellationToken = default);
}
