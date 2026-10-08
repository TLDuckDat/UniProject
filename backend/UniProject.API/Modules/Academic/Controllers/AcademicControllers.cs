using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UniProject.API.Common;
using UniProject.Share.Application.Pagination;
using UniProject.Share.Domain;
using UniProject.Share.Infrastructure.Constants;

namespace UniProject.API.Modules.Academic
{
    [ApiController]
    [Route("api/v1/faculties")]
    [Tags("Academic - Faculties")]
    public class FacultiesController : ControllerBase
    {
        private readonly IAcademicService _service;
        public FacultiesController(IAcademicService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] PaginationParams @params, CancellationToken ct) =>
            Ok(await _service.GetFacultiesAsync(@params, ct));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct) =>
            Ok(Result<FacultyDto>.Ok(await _service.GetFacultyByIdAsync(id, ct)));

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFacultyRequest request, CancellationToken ct)
        {
            var result = await _service.CreateFacultyAsync(request, ct);
            return StatusCode(201, Result<FacultyDto>.Ok(result, "Tạo khoa thành công."));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateFacultyRequest request, CancellationToken ct) =>
            Ok(Result<FacultyDto>.Ok(await _service.UpdateFacultyAsync(id, request, ct), "Cập nhật khoa thành công."));

        [Authorize(Roles = AppRoles.Admin)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
            Ok(await _service.DeleteFacultyAsync(id, ct));
    }

    [ApiController]
    [Route("api/v1/departments")]
    [Tags("Academic - Departments")]
    public class DepartmentsController : ControllerBase
    {
        private readonly IAcademicService _service;
        public DepartmentsController(IAcademicService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] PaginationParams @params, [FromQuery] int? facultyId, CancellationToken ct) =>
            Ok(await _service.GetDepartmentsAsync(@params, facultyId, ct));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct) =>
            Ok(Result<DepartmentDto>.Ok(await _service.GetDepartmentByIdAsync(id, ct)));

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest request, CancellationToken ct)
        {
            var result = await _service.CreateDepartmentAsync(request, ct);
            return StatusCode(201, Result<DepartmentDto>.Ok(result, "Tạo bộ môn thành công."));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDepartmentRequest request, CancellationToken ct) =>
            Ok(Result<DepartmentDto>.Ok(await _service.UpdateDepartmentAsync(id, request, ct), "Cập nhật bộ môn thành công."));

        [Authorize(Roles = AppRoles.Admin)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
            Ok(await _service.DeleteDepartmentAsync(id, ct));
    }

    [ApiController]
    [Route("api/v1/majors")]
    [Tags("Academic - Majors")]
    public class MajorsController : ControllerBase
    {
        private readonly IAcademicService _service;
        public MajorsController(IAcademicService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] PaginationParams @params, [FromQuery] int? departmentId, CancellationToken ct) =>
            Ok(await _service.GetMajorsAsync(@params, departmentId, ct));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct) =>
            Ok(Result<MajorDto>.Ok(await _service.GetMajorByIdAsync(id, ct)));

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMajorRequest request, CancellationToken ct)
        {
            var result = await _service.CreateMajorAsync(request, ct);
            return StatusCode(201, Result<MajorDto>.Ok(result, "Tạo chuyên ngành thành công."));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateMajorRequest request, CancellationToken ct) =>
            Ok(Result<MajorDto>.Ok(await _service.UpdateMajorAsync(id, request, ct), "Cập nhật chuyên ngành thành công."));
    }

    [ApiController]
    [Route("api/v1/classes")]
    [Tags("Academic - Classes")]
    public class ClassesController : ControllerBase
    {
        private readonly IAcademicService _service;
        public ClassesController(IAcademicService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] PaginationParams @params, [FromQuery] int? majorId, CancellationToken ct) =>
            Ok(await _service.GetClassesAsync(@params, majorId, ct));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct) =>
            Ok(Result<ClassDto>.Ok(await _service.GetClassByIdAsync(id, ct)));

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateClassRequest request, CancellationToken ct)
        {
            var result = await _service.CreateClassAsync(request, ct);
            return StatusCode(201, Result<ClassDto>.Ok(result, "Tạo lớp thành công."));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateClassRequest request, CancellationToken ct) =>
            Ok(Result<ClassDto>.Ok(await _service.UpdateClassAsync(id, request, ct), "Cập nhật lớp thành công."));
    }

    [ApiController]
    [Route("api/v1/academic-years")]
    [Tags("Academic - Years & Semesters")]
    public class AcademicYearsController : ControllerBase
    {
        private readonly IAcademicService _service;
        public AcademicYearsController(IAcademicService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken ct) =>
            Ok(Result<IReadOnlyList<AcademicYearDto>>.Ok(await _service.GetAcademicYearsAsync(ct)));

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAcademicYearRequest request, CancellationToken ct)
        {
            var result = await _service.CreateAcademicYearAsync(request, ct);
            return StatusCode(201, Result<AcademicYearDto>.Ok(result, "Tạo năm học thành công."));
        }
    }

    [ApiController]
    [Route("api/v1/semesters")]
    [Tags("Academic - Years & Semesters")]
    public class SemestersController : ControllerBase
    {
        private readonly IAcademicService _service;
        public SemestersController(IAcademicService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int? academicYearId, CancellationToken ct) =>
            Ok(Result<IReadOnlyList<SemesterDto>>.Ok(await _service.GetSemestersAsync(academicYearId, ct)));

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSemesterRequest request, CancellationToken ct)
        {
            var result = await _service.CreateSemesterAsync(request, ct);
            return StatusCode(201, Result<SemesterDto>.Ok(result, "Tạo học kỳ thành công."));
        }
    }

    [ApiController]
    [Route("api/v1/degrees")]
    [Tags("Academic - Degrees & Ranks")]
    public class DegreesController : ControllerBase
    {
        private readonly IAcademicService _service;
        public DegreesController(IAcademicService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken ct) =>
            Ok(Result<IReadOnlyList<DegreeDto>>.Ok(await _service.GetDegreesAsync(ct)));

        [HttpGet("ranks")]
        public async Task<IActionResult> GetRanks(CancellationToken ct) =>
            Ok(Result<IReadOnlyList<AcademicRankDto>>.Ok(await _service.GetAcademicRanksAsync(ct)));
    }

    [ApiController]
    [Route("api/v1/lecturers")]
    [Tags("Academic - Lecturers")]
    public class LecturersController : ControllerBase
    {
        private readonly IAcademicService _service;
        public LecturersController(IAcademicService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] PaginationParams @params, [FromQuery] int? departmentId, CancellationToken ct) =>
            Ok(await _service.GetLecturersAsync(@params, departmentId, ct));

        /// <summary>Hồ sơ giảng viên của tài khoản đang đăng nhập.</summary>
        [Authorize(Roles = AppRoles.GiangVien)]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe([FromServices] CurrentActor actor, CancellationToken ct) =>
            Ok(Result<LecturerDto>.Ok(await _service.GetLecturerByAccountIdAsync(actor.AccountId, ct)));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct) =>
            Ok(Result<LecturerDto>.Ok(await _service.GetLecturerByIdAsync(id, ct)));

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateLecturerRequest request, CancellationToken ct)
        {
            var result = await _service.CreateLecturerAsync(request, ct);
            return StatusCode(201, Result<LecturerDto>.Ok(result, "Thêm giảng viên thành công."));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateLecturerRequest request, CancellationToken ct) =>
            Ok(Result<LecturerDto>.Ok(await _service.UpdateLecturerAsync(id, request, ct), "Cập nhật giảng viên thành công."));
    }

    [ApiController]
    [Route("api/v1/students")]
    [Tags("Academic - Students")]
    public class StudentsController : ControllerBase
    {
        private readonly IAcademicService _service;
        public StudentsController(IAcademicService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] PaginationParams @params, [FromQuery] int? classId, CancellationToken ct) =>
            Ok(await _service.GetStudentsAsync(@params, classId, ct));

        /// <summary>Hồ sơ sinh viên của tài khoản đang đăng nhập.</summary>
        [Authorize(Roles = AppRoles.SinhVien)]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe([FromServices] CurrentActor actor, CancellationToken ct) =>
            Ok(Result<StudentDto>.Ok(await _service.GetStudentByAccountIdAsync(actor.AccountId, ct)));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct) =>
            Ok(Result<StudentDto>.Ok(await _service.GetStudentByIdAsync(id, ct)));

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStudentRequest request, CancellationToken ct)
        {
            var result = await _service.CreateStudentAsync(request, ct);
            return StatusCode(201, Result<StudentDto>.Ok(result, "Thêm sinh viên thành công."));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateStudentRequest request, CancellationToken ct) =>
            Ok(Result<StudentDto>.Ok(await _service.UpdateStudentAsync(id, request, ct), "Cập nhật sinh viên thành công."));
    }
}
