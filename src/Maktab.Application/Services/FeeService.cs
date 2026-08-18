using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Application.Services;

public sealed class FeeService(
    IFeeRepository feeRepository,
    IStudentRepository studentRepository) : IFeeService
{
    public async Task<IReadOnlyList<FeeRecordDto>> GetFeeRecordsAsync(CancellationToken cancellationToken = default)
    {
        var fees = await feeRepository.GetFeeRecordsAsync(cancellationToken);
        return await ToDtosAsync(fees, cancellationToken);
    }

    public async Task<IReadOnlyList<FeeRecordDto>> GetStudentFeesAsync(int studentId, CancellationToken cancellationToken = default)
    {
        if (studentId <= 0) throw new ArgumentOutOfRangeException(nameof(studentId));

        var fees = await feeRepository.GetFeeRecordsByStudentAsync(studentId, cancellationToken);
        return await ToDtosAsync(fees, cancellationToken);
    }

    public async Task<IReadOnlyList<FeeRecordDto>> GetOutstandingFeesAsync(CancellationToken cancellationToken = default)
    {
        var all = await GetFeeRecordsAsync(cancellationToken);
        return all.Where(f => !f.IsFullyPaid).ToList();
    }

    public async Task<int> CreateFeeRecordAsync(int studentId, string title, decimal amountDue, DateOnly dueDate, string academicYear, CancellationToken cancellationToken = default)
    {
        if (studentId <= 0) throw new ArgumentOutOfRangeException(nameof(studentId));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Fee title is required.", nameof(title));
        if (amountDue <= 0m) throw new ArgumentOutOfRangeException(nameof(amountDue), "Fee amount must be greater than zero.");

        var student = await studentRepository.GetStudentByIdAsync(studentId, cancellationToken);
        if (student is null) throw new InvalidOperationException("Student not found.");

        var fee = new FeeRecord
        {
            StudentId = studentId,
            Title = title.Trim(),
            AmountDue = amountDue,
            DueDate = dueDate,
            AcademicYear = academicYear?.Trim() ?? string.Empty
        };

        return await feeRepository.CreateFeeRecordAsync(fee, cancellationToken);
    }

    public async Task DeleteFeeRecordAsync(int feeId, CancellationToken cancellationToken = default)
    {
        if (feeId <= 0) throw new ArgumentOutOfRangeException(nameof(feeId));

        var payments = await feeRepository.GetPaymentsByFeeAsync(feeId, cancellationToken);
        if (payments.Count > 0)
        {
            throw new InvalidOperationException("This fee has recorded payments and cannot be deleted (payment history must be preserved).");
        }

        await feeRepository.DeleteFeeRecordAsync(feeId, cancellationToken);
    }

    public async Task<IReadOnlyList<FeePaymentDto>> GetPaymentsAsync(int feeId, CancellationToken cancellationToken = default)
    {
        if (feeId <= 0) throw new ArgumentOutOfRangeException(nameof(feeId));

        var payments = await feeRepository.GetPaymentsByFeeAsync(feeId, cancellationToken);
        return payments.Select(p => new FeePaymentDto
        {
            PaymentId = p.PaymentId,
            FeeId = p.FeeId,
            AmountPaid = p.AmountPaid,
            PaymentDate = p.PaymentDate,
            ReceiptNumber = p.ReceiptNumber
        }).ToList();
    }

    public async Task<FeePaymentDto> RecordPaymentAsync(int feeId, decimal amount, CancellationToken cancellationToken = default)
    {
        if (feeId <= 0) throw new ArgumentOutOfRangeException(nameof(feeId));
        if (amount <= 0m) throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be greater than zero.");

        var fee = await feeRepository.GetFeeRecordByIdAsync(feeId, cancellationToken);
        if (fee is null) throw new InvalidOperationException("Fee record not found.");

        var payments = await feeRepository.GetPaymentsByFeeAsync(feeId, cancellationToken);
        var alreadyPaid = payments.Sum(p => p.AmountPaid);
        var remaining = fee.AmountDue - alreadyPaid;

        if (remaining <= 0m)
        {
            throw new InvalidOperationException("This fee is already fully paid.");
        }

        if (amount > remaining)
        {
            throw new InvalidOperationException($"Payment exceeds the remaining balance. Remaining: {remaining:0.##}");
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var payment = new FeePayment
        {
            FeeId = feeId,
            AmountPaid = amount,
            PaymentDate = today
        };

        // Insert first, then derive a unique, human-readable receipt number from the payment id.
        var paymentId = await feeRepository.CreatePaymentAsync(payment, cancellationToken);
        var receiptNumber = $"RCP-{today:yyyyMMdd}-{paymentId:D5}";
        await feeRepository.SetReceiptNumberAsync(paymentId, receiptNumber, cancellationToken);

        return new FeePaymentDto
        {
            PaymentId = paymentId,
            FeeId = feeId,
            AmountPaid = amount,
            PaymentDate = today,
            ReceiptNumber = receiptNumber
        };
    }

    public async Task<decimal> GetStudentOutstandingBalanceAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var fees = await GetStudentFeesAsync(studentId, cancellationToken);
        return fees.Sum(f => f.Remaining);
    }

    private async Task<IReadOnlyList<FeeRecordDto>> ToDtosAsync(IReadOnlyList<FeeRecord> fees, CancellationToken cancellationToken)
    {
        var students = await studentRepository.GetStudentsAsync(cancellationToken);
        var studentMap = students.ToDictionary(s => s.StudentId);

        var result = new List<FeeRecordDto>();
        foreach (var fee in fees)
        {
            var payments = await feeRepository.GetPaymentsByFeeAsync(fee.FeeId, cancellationToken);
            studentMap.TryGetValue(fee.StudentId, out var student);

            result.Add(new FeeRecordDto
            {
                FeeId = fee.FeeId,
                StudentId = fee.StudentId,
                StudentName = student is null ? $"شاگرد {fee.StudentId}" : $"{student.FirstName} {student.LastName}",
                RollNumber = student?.RollNumber ?? string.Empty,
                Title = fee.Title,
                AmountDue = fee.AmountDue,
                AmountPaid = payments.Sum(p => p.AmountPaid),
                DueDate = fee.DueDate,
                AcademicYear = fee.AcademicYear
            });
        }

        return result;
    }
}
