using Microsoft.Data.Sqlite;
using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;
using Maktab.Domain.Enums;

namespace Maktab.Infrastructure.Persistence;

public sealed class SqliteFeeRepository(IConnectionStringProvider connectionStringProvider) : IFeeRepository
{
    public async Task<IReadOnlyList<FeeDto>> GetFeesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT f.FeeID, f.StudentID, s.FirstName || ' ' || s.LastName AS StudentName, s.RollNumber,
       f.FeeType, f.Amount, f.DueDate, f.AcademicYearId,
       COALESCE(SUM(p.Amount), 0) AS TotalPaid
FROM tbl_Fees f
JOIN tbl_Students s ON f.StudentID = s.StudentID
LEFT JOIN tbl_FeePayments p ON f.FeeID = p.FeeID
GROUP BY f.FeeID, f.StudentID, s.FirstName, s.LastName, s.RollNumber, f.FeeType, f.Amount, f.DueDate, f.AcademicYearId
ORDER BY f.DueDate;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var result = new List<FeeDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var amount = Convert.ToDecimal(reader.GetDouble(5));
            var totalPaid = Convert.ToDecimal(reader.GetDouble(8));
            var status = totalPaid <= 0m ? FeeStatus.Unpaid :
                         totalPaid >= amount ? FeeStatus.Paid : FeeStatus.Partial;

            result.Add(new FeeDto
            {
                FeeId = reader.GetInt32(0),
                StudentId = reader.GetInt32(1),
                StudentName = reader.GetString(2),
                RollNumber = reader.GetString(3),
                FeeType = reader.GetString(4),
                Amount = amount,
                DueDate = DateTime.Parse(reader.GetString(6)),
                AcademicYearId = reader.GetInt32(7),
                TotalPaid = totalPaid,
                Status = status
            });
        }

        return result;
    }

    public async Task<Fee?> GetFeeByIdAsync(int feeId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT FeeID, StudentID, FeeType, Amount, DueDate, CreatedDate, AcademicYearId
FROM tbl_Fees
WHERE FeeID = $feeId;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$feeId", feeId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new Fee
            {
                FeeId = reader.GetInt32(0),
                StudentId = reader.GetInt32(1),
                FeeType = reader.GetString(2),
                Amount = Convert.ToDecimal(reader.GetDouble(3)),
                DueDate = DateTime.Parse(reader.GetString(4)),
                CreatedDate = DateTime.Parse(reader.GetString(5)),
                AcademicYearId = reader.GetInt32(6)
            };
        }

        return null;
    }

    public async Task<int> CreateFeeAsync(Fee fee, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO tbl_Fees (StudentID, FeeType, Amount, DueDate, CreatedDate, AcademicYearId)
VALUES ($studentId, $feeType, $amount, $dueDate, $createdDate, $academicYearId);
SELECT last_insert_rowid();";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$studentId", fee.StudentId);
            command.Parameters.AddWithValue("$feeType", fee.FeeType);
            command.Parameters.AddWithValue("$amount", (double)fee.Amount);
            command.Parameters.AddWithValue("$dueDate", fee.DueDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("$createdDate", fee.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("$academicYearId", fee.AcademicYearId);

            var id = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            await transaction.CommitAsync(cancellationToken);
            return id;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteFeeAsync(int feeId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM tbl_Fees WHERE FeeID = $feeId;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$feeId", feeId);

            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0) throw new InvalidOperationException("Fee not found.");

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<FeePaymentDto>> GetPaymentsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT p.PaymentID, p.FeeID, p.StudentID, s.FirstName || ' ' || s.LastName AS StudentName, f.FeeType,
       p.Amount, p.PaymentDate, p.ReceiptNumber
FROM tbl_FeePayments p
JOIN tbl_Fees f ON p.FeeID = f.FeeID
JOIN tbl_Students s ON p.StudentID = s.StudentID
ORDER BY p.PaymentDate DESC;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var result = new List<FeePaymentDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new FeePaymentDto
            {
                PaymentId = reader.GetInt32(0),
                FeeId = reader.GetInt32(1),
                StudentId = reader.GetInt32(2),
                StudentName = reader.GetString(3),
                FeeType = reader.GetString(4),
                Amount = Convert.ToDecimal(reader.GetDouble(5)),
                PaymentDate = DateTime.Parse(reader.GetString(6)),
                ReceiptNumber = reader.GetString(7)
            });
        }

        return result;
    }

    public async Task<decimal> GetTotalPaidByFeeAsync(int feeId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COALESCE(SUM(Amount), 0) FROM tbl_FeePayments WHERE FeeID = $feeId;";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$feeId", feeId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToDecimal(result);
    }

    public async Task<int> RecordPaymentAsync(FeePayment payment, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO tbl_FeePayments (FeeID, StudentID, Amount, PaymentDate, ReceiptNumber)
VALUES ($feeId, $studentId, $amount, $paymentDate, $receiptNumber);
SELECT last_insert_rowid();";

        await using var connection = new SqliteConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$feeId", payment.FeeId);
            command.Parameters.AddWithValue("$studentId", payment.StudentId);
            command.Parameters.AddWithValue("$amount", (double)payment.Amount);
            command.Parameters.AddWithValue("$paymentDate", payment.PaymentDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("$receiptNumber", payment.ReceiptNumber);

            var id = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            await transaction.CommitAsync(cancellationToken);
            return id;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
