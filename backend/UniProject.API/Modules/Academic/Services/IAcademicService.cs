using UniProject.Share.Application.Pagination;
using UniProject.Share.Domain;

namespace UniProject.API.Modules.Academic
{
    public interface IFacultyService
    {
        Task<PagedResult<FacultyDto>> GetFacultiesAsync(PaginationParams @params, CancellationToken cancellationToken = default);
        Task<FacultyDto> GetFacultyByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<FacultyDto> CreateFacultyAsync(CreateFacultyRequest request, CancellationToken cancellationToken = default);
        Task<FacultyDto> UpdateFacultyAsync(int id, UpdateFacultyRequest request, CancellationToken cancellationToken = default);
        Task<Result> DeleteFacultyAsync(int id, CancellationToken cancellationToken = default);
    }

    public interface IDepartmentService
    {
        Task<PagedResult<DepartmentDto>> GetDepartmentsAsync(PaginationParams @params, int? facultyId = null, CancellationToken cancellationToken = default);
        Task<DepartmentDto> GetDepartmentByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default);
        Task<DepartmentDto> UpdateDepartmentAsync(int id, UpdateDepartmentRequest request, CancellationToken cancellationToken = default);
        Task<Result> DeleteDepartmentAsync(int id, CancellationToken cancellationToken = default);
    }

    public interface IMajorService
    {
        Task<PagedResult<MajorDto>> GetMajorsAsync(PaginationParams @params, int? departmentId = null, CancellationToken cancellationToken = default);
        Task<MajorDto> GetMajorByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<MajorDto> CreateMajorAsync(CreateMajorRequest request, CancellationToken cancellationToken = default);
        Task<MajorDto> UpdateMajorAsync(int id, UpdateMajorRequest request, CancellationToken cancellationToken = default);
    }

    public interface IClassService
    {
        Task<PagedResult<ClassDto>> GetClassesAsync(PaginationParams @params, int? majorId = null, CancellationToken cancellationToken = default);
        Task<ClassDto> GetClassByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<ClassDto> CreateClassAsync(CreateClassRequest request, CancellationToken cancellationToken = default);
        Task<ClassDto> UpdateClassAsync(int id, UpdateClassRequest request, CancellationToken cancellationToken = default);
    }

    public interface IAcademicYearService
    {
        Task<IReadOnlyList<AcademicYearDto>> GetAcademicYearsAsync(CancellationToken cancellationToken = default);
        Task<AcademicYearDto> CreateAcademicYearAsync(CreateAcademicYearRequest request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SemesterDto>> GetSemestersAsync(int? academicYearId = null, CancellationToken cancellationToken = default);
        Task<SemesterDto> CreateSemesterAsync(CreateSemesterRequest request, CancellationToken cancellationToken = default);
    }

    public interface ILecturerService
    {
        Task<IReadOnlyList<DegreeDto>> GetDegreesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AcademicRankDto>> GetAcademicRanksAsync(CancellationToken cancellationToken = default);
        Task<PagedResult<LecturerDto>> GetLecturersAsync(PaginationParams @params, int? departmentId = null, CancellationToken cancellationToken = default);
        Task<LecturerDto> GetLecturerByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<LecturerDto> GetLecturerByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);
        Task<LecturerDto> CreateLecturerAsync(CreateLecturerRequest request, CancellationToken cancellationToken = default);
        Task<LecturerDto> UpdateLecturerAsync(int id, UpdateLecturerRequest request, CancellationToken cancellationToken = default);
    }

    public interface IStudentService
    {
        Task<PagedResult<StudentDto>> GetStudentsAsync(PaginationParams @params, int? classId = null, CancellationToken cancellationToken = default);
        Task<StudentDto> GetStudentByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<StudentDto> GetStudentByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);
        Task<StudentDto> CreateStudentAsync(CreateStudentRequest request, CancellationToken cancellationToken = default);
        Task<StudentDto> UpdateStudentAsync(int id, UpdateStudentRequest request, CancellationToken cancellationToken = default);
    }

    public interface IAcademicService : 
        IFacultyService, 
        IDepartmentService, 
        IMajorService, 
        IClassService, 
        IAcademicYearService, 
        ILecturerService, 
        IStudentService
    {
    }
}
