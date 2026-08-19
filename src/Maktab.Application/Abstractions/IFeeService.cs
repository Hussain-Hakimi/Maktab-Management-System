namespace Maktab.Application.Abstractions;

public interface IFeeService
{
    Task<IReadOnlyList<FeeDto>> GetFeesAsync(CancellationToken cancellationToken = default);
    Task<int> AddFeeAsync(SaveFeeDto fee, CancellationToken cancellationToken = default);
    Task DeleteFeeAsync(int feeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FeePaymentDto>> GetPaymentsAsync(CancellationToken cancellationToken = default);
    Task<int> RecordPaymentAsync(RecordPaymentDto payment, CancellationToken cancellationToken = default);
}
