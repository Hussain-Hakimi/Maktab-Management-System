using Maktab.Application.Abstractions;
using Maktab.Domain.Entities;

namespace Maktab.Application.Services;

public sealed class AcademicYearService(IAcademicYearRepository repository) : IAcademicYearService
{
    public async Task<AcademicYearDto?> GetActiveAcademicYearAsync(CancellationToken cancellationToken = default)
    {
        var year = await repository.GetActiveAsync(cancellationToken);
        return year is null ? null : MapToDto(year);
    }

    public async Task<IReadOnlyList<AcademicYearDto>> GetAllAcademicYearsAsync(CancellationToken cancellationToken = default)
    {
        var years = await repository.GetAllAsync(cancellationToken);
        return years.Select(MapToDto).ToList();
    }

    public async Task<int> CreateAcademicYearAsync(SaveAcademicYearDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.YearName))
            throw new ArgumentException("Year name is required.", nameof(dto.YearName));
        if (dto.StartDate >= dto.EndDate)
            throw new ArgumentException("Start date must be before end date.");

        var entity = new AcademicYear
        {
            YearName = dto.YearName.Trim(),
            StartDate = dto.StartDate.Date,
            EndDate = dto.EndDate.Date,
            IsActive = false
        };

        return await repository.CreateAsync(entity, cancellationToken);
    }

    public async Task SetActiveAcademicYearAsync(int academicYearId, CancellationToken cancellationToken = default)
    {
        if (academicYearId <= 0)
            throw new ArgumentOutOfRangeException(nameof(academicYearId));

        await repository.SetActiveAsync(academicYearId, cancellationToken);
    }

    private static AcademicYearDto MapToDto(AcademicYear year) => new()
    {
        AcademicYearId = year.AcademicYearId,
        YearName = year.YearName,
        StartDate = year.StartDate,
        EndDate = year.EndDate,
        IsActive = year.IsActive
    };
}
