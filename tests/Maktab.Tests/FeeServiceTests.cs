using Maktab.Application.Abstractions;
using Maktab.Application.Services;
using Maktab.Domain.Entities;

namespace Maktab.Tests;

public class FeeServiceTests
{
    private sealed class InMemoryFeeRepository : IFeeRepository
    {
        private readonly List<Fee> _fees = [];
        private readonly List<FeePayment> _payments = [];
        private int _nextFeeId = 1;
        private int _nextPaymentId = 1;

        public Task<IReadOnlyList<FeeDto>> GetFeesAsync(CancellationToken cancellationToken = default)
        {
            var result = _fees.Select(f =>
            {
                var totalPaid = _payments.Where(p => p.FeeId == f.FeeId).Sum(p => p.Amount);
                var status = totalPaid <= 0m ? Domain.Enums.FeeStatus.Unpaid :
                             totalPaid >= f.Amount ? Domain.Enums.FeeStatus.Paid : Domain.Enums.FeeStatus.Partial;
                return new FeeDto
                {
                    FeeId = f.FeeId,
                    StudentId = f.StudentId,
                    FeeType = f.FeeType,
                    Amount = f.Amount,
                    DueDate = f.DueDate,
                    TotalPaid = totalPaid,
                    Status = status
                };
            }).ToList();
            return Task.FromResult<IReadOnlyList<FeeDto>>(result);
        }

        public Task<Fee?> GetFeeByIdAsync(int feeId, CancellationToken cancellationToken = default)
            => Task.FromResult(_fees.FirstOrDefault(f => f.FeeId == feeId));

        public Task<int> CreateFeeAsync(Fee fee, CancellationToken cancellationToken = default)
        {
            fee.FeeId = _nextFeeId++;
            _fees.Add(fee);
            return Task.FromResult(fee.FeeId);
        }

        public Task DeleteFeeAsync(int feeId, CancellationToken cancellationToken = default)
        {
            _fees.RemoveAll(f => f.FeeId == feeId);
            _payments.RemoveAll(p => p.FeeId == feeId);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<FeePaymentDto>> GetPaymentsAsync(CancellationToken cancellationToken = default)
        {
            var result = _payments.Select(p => new FeePaymentDto
            {
                PaymentId = p.PaymentId,
                FeeId = p.FeeId,
                StudentId = p.StudentId,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                ReceiptNumber = p.ReceiptNumber
            }).ToList();
            return Task.FromResult<IReadOnlyList<FeePaymentDto>>(result);
        }

        public Task<decimal> GetTotalPaidByFeeAsync(int feeId, CancellationToken cancellationToken = default)
            => Task.FromResult(_payments.Where(p => p.FeeId == feeId).Sum(p => p.Amount));

        public Task<int> RecordPaymentAsync(FeePayment payment, CancellationToken cancellationToken = default)
        {
            payment.PaymentId = _nextPaymentId++;
            _payments.Add(payment);
            return Task.FromResult(payment.PaymentId);
        }
    }

    [Fact]
    public async Task AddFee_WithValidData_ReturnsId()
    {
        var repo = new InMemoryFeeRepository();
        var service = new FeeService(repo);

        var id = await service.AddFeeAsync(new SaveFeeDto(1, "Tuition", 1000m, DateTime.Today.AddDays(30), 1));

        Assert.True(id > 0);
        var fees = await service.GetFeesAsync();
        Assert.Single(fees);
        Assert.Equal(1000m, fees[0].Amount);
        Assert.Equal(Domain.Enums.FeeStatus.Unpaid, fees[0].Status);
    }

    [Fact]
    public async Task RecordPayment_UpdatesOutstandingAndStatus()
    {
        var repo = new InMemoryFeeRepository();
        var service = new FeeService(repo);
        var feeId = await service.AddFeeAsync(new SaveFeeDto(1, "Tuition", 1000m, DateTime.Today.AddDays(30), 1));

        await service.RecordPaymentAsync(new RecordPaymentDto(feeId, 400m, DateTime.Today));

        var fees = await service.GetFeesAsync();
        Assert.Equal(400m, fees[0].TotalPaid);
        Assert.Equal(600m, fees[0].Outstanding);
        Assert.Equal(Domain.Enums.FeeStatus.Partial, fees[0].Status);
    }

    [Fact]
    public async Task RecordPayment_WhenOverpay_ThrowsInvalidOperationException()
    {
        var repo = new InMemoryFeeRepository();
        var service = new FeeService(repo);
        var feeId = await service.AddFeeAsync(new SaveFeeDto(1, "Tuition", 1000m, DateTime.Today.AddDays(30), 1));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await service.RecordPaymentAsync(new RecordPaymentDto(feeId, 1100m, DateTime.Today));
        });
    }

    [Fact]
    public async Task RecordPayment_WhenExactAmount_MarksAsPaid()
    {
        var repo = new InMemoryFeeRepository();
        var service = new FeeService(repo);
        var feeId = await service.AddFeeAsync(new SaveFeeDto(1, "Tuition", 1000m, DateTime.Today.AddDays(30), 1));

        await service.RecordPaymentAsync(new RecordPaymentDto(feeId, 1000m, DateTime.Today));

        var fees = await service.GetFeesAsync();
        Assert.Equal(1000m, fees[0].TotalPaid);
        Assert.Equal(0m, fees[0].Outstanding);
        Assert.Equal(Domain.Enums.FeeStatus.Paid, fees[0].Status);
    }

    [Fact]
    public async Task DeleteFee_RemovesFeeAndPayments()
    {
        var repo = new InMemoryFeeRepository();
        var service = new FeeService(repo);
        var feeId = await service.AddFeeAsync(new SaveFeeDto(1, "Tuition", 1000m, DateTime.Today.AddDays(30), 1));
        await service.RecordPaymentAsync(new RecordPaymentDto(feeId, 500m, DateTime.Today));

        await service.DeleteFeeAsync(feeId);

        var fees = await service.GetFeesAsync();
        var payments = await service.GetPaymentsAsync();
        Assert.Empty(fees);
        Assert.Empty(payments);
    }
}
