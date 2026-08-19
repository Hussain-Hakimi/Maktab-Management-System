using Maktab.Domain.Entities;

namespace Maktab.Application.Abstractions;

public interface IFeeRepository
{
    Task<IReadOnlyList<FeeDto>> GetFeesAsync(CancellationToken cancellationToken = default);
    Task<Fee?> GetFeeByIdAsync(int feeId, CancellationToken cancellationToken = default);
    Task<int> CreateFeeAsync(Fee fee, CancellationToken cancellationToken = default);
    Task DeleteFeeAsync(int feeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FeePaymentDto>> GetPaymentsAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetTotalPaidByFeeAsync(int feeId, CancellationToken cancellationToken = default);
    Task<int> RecordPaymentAsync(FeePayment payment, CancellationToken cancellationToken = default);
}
