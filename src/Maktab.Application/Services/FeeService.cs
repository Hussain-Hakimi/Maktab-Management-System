using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Application.Services;

public sealed class FeeService(IFeeRepository repository) : IFeeService
{
    public Task<IReadOnlyList<FeeDto>> GetFeesAsync(CancellationToken cancellationToken = default)
        => repository.GetFeesAsync(cancellationToken);

    public async Task<int> AddFeeAsync(SaveFeeDto fee, CancellationToken cancellationToken = default)
    {
        if (fee.StudentId <= 0) throw new ArgumentOutOfRangeException(nameof(fee.StudentId));
        if (string.IsNullOrWhiteSpace(fee.FeeType)) throw new ArgumentException("Fee type is required.");
        if (fee.Amount <= 0m) throw new ArgumentOutOfRangeException(nameof(fee.Amount), "Amount must be greater than zero.");
        if (fee.DueDate == default) throw new ArgumentException("Due date is required.");
        if (fee.AcademicYearId <= 0) throw new ArgumentOutOfRangeException(nameof(fee.AcademicYearId));

        var entity = new Fee
        {
            StudentId = fee.StudentId,
            FeeType = fee.FeeType.Trim(),
            Amount = fee.Amount,
            DueDate = fee.DueDate.Date,
            CreatedDate = DateTime.Now,
            AcademicYearId = fee.AcademicYearId
        };

        return await repository.CreateFeeAsync(entity, cancellationToken);
    }

    public async Task DeleteFeeAsync(int feeId, CancellationToken cancellationToken = default)
    {
        if (feeId <= 0) throw new ArgumentOutOfRangeException(nameof(feeId));

        var fee = await repository.GetFeeByIdAsync(feeId, cancellationToken);
        if (fee is null) throw new InvalidOperationException("Fee not found.");

        await repository.DeleteFeeAsync(feeId, cancellationToken);
    }

    public Task<IReadOnlyList<FeePaymentDto>> GetPaymentsAsync(CancellationToken cancellationToken = default)
        => repository.GetPaymentsAsync(cancellationToken);

    public async Task<int> RecordPaymentAsync(RecordPaymentDto payment, CancellationToken cancellationToken = default)
    {
        if (payment.FeeId <= 0) throw new ArgumentOutOfRangeException(nameof(payment.FeeId));
        if (payment.Amount <= 0m) throw new ArgumentOutOfRangeException(nameof(payment.Amount), "Payment amount must be greater than zero.");
        if (payment.PaymentDate == default) throw new ArgumentException("Payment date is required.");

        var fee = await repository.GetFeeByIdAsync(payment.FeeId, cancellationToken);
        if (fee is null) throw new InvalidOperationException("Fee not found.");

        var totalPaid = await repository.GetTotalPaidByFeeAsync(payment.FeeId, cancellationToken);
        var outstanding = fee.Amount - totalPaid;

        if (payment.Amount > outstanding)
            throw new InvalidOperationException($"Payment amount exceeds outstanding balance ({outstanding}).");

        var entity = new FeePayment
        {
            FeeId = payment.FeeId,
            StudentId = fee.StudentId,
            Amount = payment.Amount,
            PaymentDate = payment.PaymentDate.Date,
            ReceiptNumber = GenerateReceiptNumber(payment.FeeId, payment.PaymentDate)
        };

        return await repository.RecordPaymentAsync(entity, cancellationToken);
    }

    private static string GenerateReceiptNumber(int feeId, DateTime date)
    {
        return $"RCP-{date:yyyyMMddHHmmss}-{feeId}";
    }
}
