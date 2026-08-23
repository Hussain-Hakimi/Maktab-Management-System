namespace Maktab.Application.Abstractions;

public interface IAcademicYearService
{
    Task<AcademicYearDto?> GetActiveAcademicYearAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AcademicYearDto>> GetAllAcademicYearsAsync(CancellationToken cancellationToken = default);
    Task<int> CreateAcademicYearAsync(SaveAcademicYearDto dto, CancellationToken cancellationToken = default);
    Task SetActiveAcademicYearAsync(int academicYearId, CancellationToken cancellationToken = default);
}
