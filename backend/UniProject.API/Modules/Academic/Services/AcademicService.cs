using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UniProject.Share.Application.Pagination;
using UniProject.Share.Domain;
using UniProject.Share.Infrastructure.Exceptions;
using UniProject.Share.Infrastructure.Extensions;
using UniProject.API.Data;
using UniProject.Share.Infrastructure.Constants;

namespace UniProject.API.Modules.Academic
{
    /// <summary>
    /// Hiện thực nhiều interface nhỏ (IFacultyService, ILecturerService, ...) để controller
    /// chỉ phụ thuộc vào phần mình dùng. Xem cách đăng ký trong AcademicModule.
    /// </summary>
    public class AcademicService : IAcademicService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<AcademicService> _logger;

        public AcademicService(AppDbContext dbContext, ILogger<AcademicService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // Faculty
        public async Task<PagedResult<FacultyDto>> GetFacultiesAsync(PaginationParams @params, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.Khoas.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(@params.Search))
            {
                var s = @params.Search.Trim().ToLower();
                query = query.Where(x => x.MaKhoaCode.ToLower().Contains(s) || x.TenKhoa.ToLower().Contains(s));
            }

            query = @params.SortDirection.ToLower() == "desc" ? query.OrderByDescending(x => x.TenKhoa) : query.OrderBy(x => x.TenKhoa);

            return await query.ToPagedResultAsync(
                @params.PageNumber,
                @params.PageSize,
                x => new FacultyDto(x.Id, x.MaKhoaCode, x.TenKhoa, x.Email, x.SoDienThoai, x.IsActive),
                cancellationToken);
        }

        public async Task<FacultyDto> GetFacultyByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var item = await _dbContext.Khoas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (item == null) throw new NotFoundException("Khoa", id);
            return new FacultyDto(item.Id, item.MaKhoaCode, item.TenKhoa, item.Email, item.SoDienThoai, item.IsActive);
        }

        public async Task<FacultyDto> CreateFacultyAsync(CreateFacultyRequest request, CancellationToken cancellationToken = default)
        {
            if (await _dbContext.Khoas.AnyAsync(x => x.MaKhoaCode == request.Code.Trim(), cancellationToken))
                throw new ConflictException($"Mã khoa '{request.Code}' đã tồn tại.");
            if (await _dbContext.Khoas.AnyAsync(x => x.TenKhoa == request.Name.Trim(), cancellationToken))
                throw new ConflictException($"Tên khoa '{request.Name}' đã tồn tại.");

            var entity = new Khoa(request.Code, request.Name, request.Email, request.PhoneNumber);
            _dbContext.Khoas.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new FacultyDto(entity.Id, entity.MaKhoaCode, entity.TenKhoa, entity.Email, entity.SoDienThoai, entity.IsActive);
        }

        public async Task<FacultyDto> UpdateFacultyAsync(int id, UpdateFacultyRequest request, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.Khoas.FindAsync(new object[] { id }, cancellationToken);
            if (entity == null) throw new NotFoundException("Khoa", id);

            if (await _dbContext.Khoas.AnyAsync(x => x.Id != id && x.TenKhoa == request.Name.Trim(), cancellationToken))
                throw new ConflictException($"Tên khoa '{request.Name}' đã tồn tại.");

            entity.Update(request.Name, request.Email, request.PhoneNumber);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new FacultyDto(entity.Id, entity.MaKhoaCode, entity.TenKhoa, entity.Email, entity.SoDienThoai, entity.IsActive);
        }

        public async Task<Result> DeleteFacultyAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.Khoas.FindAsync(new object[] { id }, cancellationToken);
            if (entity == null) throw new NotFoundException("Khoa", id);

            if (await _dbContext.BoMons.AnyAsync(x => x.MaKhoa == id, cancellationToken))
                throw new ConflictException("Không thể xóa khoa đang có bộ môn trực thuộc.");

            _dbContext.Khoas.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Ok("Xóa khoa thành công.");
        }

        // Department
        public async Task<PagedResult<DepartmentDto>> GetDepartmentsAsync(PaginationParams @params, int? facultyId = null, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.BoMons.AsNoTracking().Include(x => x.Khoa).AsQueryable();
            if (facultyId.HasValue)
                query = query.Where(x => x.MaKhoa == facultyId.Value);

            if (!string.IsNullOrWhiteSpace(@params.Search))
            {
                var s = @params.Search.Trim().ToLower();
                query = query.Where(x => x.MaBoMonCode.ToLower().Contains(s) || x.TenBoMon.ToLower().Contains(s));
            }

            query = @params.SortDirection.ToLower() == "desc" ? query.OrderByDescending(x => x.TenBoMon) : query.OrderBy(x => x.TenBoMon);

            return await query.ToPagedResultAsync(
                @params.PageNumber,
                @params.PageSize,
                x => new DepartmentDto(x.Id, x.MaBoMonCode, x.TenBoMon, x.MaKhoa, x.Khoa.TenKhoa, x.IsActive),
                cancellationToken);
        }

        public async Task<DepartmentDto> GetDepartmentByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var item = await _dbContext.BoMons.AsNoTracking().Include(x => x.Khoa).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (item == null) throw new NotFoundException("Bộ môn", id);
            return new DepartmentDto(item.Id, item.MaBoMonCode, item.TenBoMon, item.MaKhoa, item.Khoa.TenKhoa, item.IsActive);
        }

        public async Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default)
        {
            if (!await _dbContext.Khoas.AnyAsync(x => x.Id == request.FacultyId, cancellationToken))
                throw new NotFoundException("Khoa", request.FacultyId);

            if (await _dbContext.BoMons.AnyAsync(x => x.MaBoMonCode == request.Code.Trim(), cancellationToken))
                throw new ConflictException($"Mã bộ môn '{request.Code}' đã tồn tại.");

            if (await _dbContext.BoMons.AnyAsync(x => x.MaKhoa == request.FacultyId && x.TenBoMon == request.Name.Trim(), cancellationToken))
                throw new ConflictException($"Tên bộ môn '{request.Name}' đã tồn tại trong khoa này.");

            var entity = new BoMon(request.Code, request.Name, request.FacultyId);
            _dbContext.BoMons.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var faculty = await _dbContext.Khoas.FindAsync(new object[] { entity.MaKhoa }, cancellationToken);
            return new DepartmentDto(entity.Id, entity.MaBoMonCode, entity.TenBoMon, entity.MaKhoa, faculty!.TenKhoa, entity.IsActive);
        }

        public async Task<DepartmentDto> UpdateDepartmentAsync(int id, UpdateDepartmentRequest request, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.BoMons.Include(x => x.Khoa).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity == null) throw new NotFoundException("Bộ môn", id);

            if (!await _dbContext.Khoas.AnyAsync(x => x.Id == request.FacultyId, cancellationToken))
                throw new NotFoundException("Khoa", request.FacultyId);

            if (await _dbContext.BoMons.AnyAsync(x => x.Id != id && x.MaKhoa == request.FacultyId && x.TenBoMon == request.Name.Trim(), cancellationToken))
                throw new ConflictException($"Tên bộ môn '{request.Name}' đã tồn tại trong khoa này.");

            entity.Update(request.Name, request.FacultyId);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var faculty = await _dbContext.Khoas.FindAsync(new object[] { entity.MaKhoa }, cancellationToken);
            return new DepartmentDto(entity.Id, entity.MaBoMonCode, entity.TenBoMon, entity.MaKhoa, faculty!.TenKhoa, entity.IsActive);
        }

        public async Task<Result> DeleteDepartmentAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.BoMons.FindAsync(new object[] { id }, cancellationToken);
            if (entity == null) throw new NotFoundException("Bộ môn", id);

            if (await _dbContext.ChuyenNganhs.AnyAsync(x => x.MaBoMon == id, cancellationToken) ||
                await _dbContext.GiangViens.AnyAsync(x => x.MaBoMon == id, cancellationToken))
                throw new ConflictException("Không thể xóa bộ môn đang có chuyên ngành hoặc giảng viên trực thuộc.");

            _dbContext.BoMons.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Ok("Xóa bộ môn thành công.");
        }

        // Major
        public async Task<PagedResult<MajorDto>> GetMajorsAsync(PaginationParams @params, int? departmentId = null, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.ChuyenNganhs.AsNoTracking().Include(x => x.BoMon).AsQueryable();
            if (departmentId.HasValue)
                query = query.Where(x => x.MaBoMon == departmentId.Value);

            if (!string.IsNullOrWhiteSpace(@params.Search))
            {
                var s = @params.Search.Trim().ToLower();
                query = query.Where(x => x.MaChuyenNganhCode.ToLower().Contains(s) || x.TenChuyenNganh.ToLower().Contains(s));
            }

            query = @params.SortDirection.ToLower() == "desc" ? query.OrderByDescending(x => x.TenChuyenNganh) : query.OrderBy(x => x.TenChuyenNganh);

            return await query.ToPagedResultAsync(
                @params.PageNumber,
                @params.PageSize,
                x => new MajorDto(x.Id, x.MaChuyenNganhCode, x.TenChuyenNganh, x.MaBoMon, x.BoMon.TenBoMon, x.IsActive),
                cancellationToken);
        }

        public async Task<MajorDto> GetMajorByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var item = await _dbContext.ChuyenNganhs.AsNoTracking().Include(x => x.BoMon).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (item == null) throw new NotFoundException("Chuyên ngành", id);
            return new MajorDto(item.Id, item.MaChuyenNganhCode, item.TenChuyenNganh, item.MaBoMon, item.BoMon.TenBoMon, item.IsActive);
        }

        public async Task<MajorDto> CreateMajorAsync(CreateMajorRequest request, CancellationToken cancellationToken = default)
        {
            if (!await _dbContext.BoMons.AnyAsync(x => x.Id == request.DepartmentId, cancellationToken))
                throw new NotFoundException("Bộ môn", request.DepartmentId);

            if (await _dbContext.ChuyenNganhs.AnyAsync(x => x.MaChuyenNganhCode == request.Code.Trim(), cancellationToken))
                throw new ConflictException($"Mã chuyên ngành '{request.Code}' đã tồn tại.");

            if (await _dbContext.ChuyenNganhs.AnyAsync(x => x.MaBoMon == request.DepartmentId && x.TenChuyenNganh == request.Name.Trim(), cancellationToken))
                throw new ConflictException($"Tên chuyên ngành '{request.Name}' đã tồn tại trong bộ môn này.");

            var entity = new ChuyenNganh(request.Code, request.Name, request.DepartmentId);
            _dbContext.ChuyenNganhs.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var dept = await _dbContext.BoMons.FindAsync(new object[] { entity.MaBoMon }, cancellationToken);
            return new MajorDto(entity.Id, entity.MaChuyenNganhCode, entity.TenChuyenNganh, entity.MaBoMon, dept!.TenBoMon, entity.IsActive);
        }

        public async Task<MajorDto> UpdateMajorAsync(int id, UpdateMajorRequest request, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.ChuyenNganhs.FindAsync(new object[] { id }, cancellationToken);
            if (entity == null) throw new NotFoundException("Chuyên ngành", id);

            if (!await _dbContext.BoMons.AnyAsync(x => x.Id == request.DepartmentId, cancellationToken))
                throw new NotFoundException("Bộ môn", request.DepartmentId);

            entity.Update(request.Name, request.DepartmentId);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var dept = await _dbContext.BoMons.FindAsync(new object[] { entity.MaBoMon }, cancellationToken);
            return new MajorDto(entity.Id, entity.MaChuyenNganhCode, entity.TenChuyenNganh, entity.MaBoMon, dept!.TenBoMon, entity.IsActive);
        }

        // Class
        public async Task<PagedResult<ClassDto>> GetClassesAsync(PaginationParams @params, int? majorId = null, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.Lops.AsNoTracking().Include(x => x.ChuyenNganh).AsQueryable();
            if (majorId.HasValue)
                query = query.Where(x => x.MaChuyenNganh == majorId.Value);

            if (!string.IsNullOrWhiteSpace(@params.Search))
            {
                var s = @params.Search.Trim().ToLower();
                query = query.Where(x => x.MaLopCode.ToLower().Contains(s) || x.TenLop.ToLower().Contains(s));
            }

            query = @params.SortDirection.ToLower() == "desc" ? query.OrderByDescending(x => x.TenLop) : query.OrderBy(x => x.TenLop);

            return await query.ToPagedResultAsync(
                @params.PageNumber,
                @params.PageSize,
                x => new ClassDto(x.Id, x.MaLopCode, x.TenLop, x.NamNhapHoc, x.MaChuyenNganh, x.ChuyenNganh.TenChuyenNganh, x.IsActive),
                cancellationToken);
        }

        public async Task<ClassDto> GetClassByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var item = await _dbContext.Lops.AsNoTracking().Include(x => x.ChuyenNganh).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (item == null) throw new NotFoundException("Lớp", id);
            return new ClassDto(item.Id, item.MaLopCode, item.TenLop, item.NamNhapHoc, item.MaChuyenNganh, item.ChuyenNganh.TenChuyenNganh, item.IsActive);
        }

        public async Task<ClassDto> CreateClassAsync(CreateClassRequest request, CancellationToken cancellationToken = default)
        {
            if (request.AdmissionYear < 2000 || request.AdmissionYear > 2100)
                throw new ValidationException("AdmissionYear", "Năm nhập học phải từ 2000 đến 2100.");

            if (!await _dbContext.ChuyenNganhs.AnyAsync(x => x.Id == request.MajorId, cancellationToken))
                throw new NotFoundException("Chuyên ngành", request.MajorId);

            if (await _dbContext.Lops.AnyAsync(x => x.MaLopCode == request.Code.Trim(), cancellationToken))
                throw new ConflictException($"Mã lớp '{request.Code}' đã tồn tại.");

            var entity = new Lop(request.Code, request.Name, request.AdmissionYear, request.MajorId);
            _dbContext.Lops.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var major = await _dbContext.ChuyenNganhs.FindAsync(new object[] { entity.MaChuyenNganh }, cancellationToken);
            return new ClassDto(entity.Id, entity.MaLopCode, entity.TenLop, entity.NamNhapHoc, entity.MaChuyenNganh, major!.TenChuyenNganh, entity.IsActive);
        }

        public async Task<ClassDto> UpdateClassAsync(int id, UpdateClassRequest request, CancellationToken cancellationToken = default)
        {
            if (request.AdmissionYear < 2000 || request.AdmissionYear > 2100)
                throw new ValidationException("AdmissionYear", "Năm nhập học phải từ 2000 đến 2100.");

            var entity = await _dbContext.Lops.FindAsync(new object[] { id }, cancellationToken);
            if (entity == null) throw new NotFoundException("Lớp", id);

            if (!await _dbContext.ChuyenNganhs.AnyAsync(x => x.Id == request.MajorId, cancellationToken))
                throw new NotFoundException("Chuyên ngành", request.MajorId);

            entity.Update(request.Name, request.AdmissionYear, request.MajorId);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var major = await _dbContext.ChuyenNganhs.FindAsync(new object[] { entity.MaChuyenNganh }, cancellationToken);
            return new ClassDto(entity.Id, entity.MaLopCode, entity.TenLop, entity.NamNhapHoc, entity.MaChuyenNganh, major!.TenChuyenNganh, entity.IsActive);
        }

        // Academic Year & Semester
        public async Task<IReadOnlyList<AcademicYearDto>> GetAcademicYearsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.NamHocs
                .AsNoTracking()
                .OrderByDescending(x => x.NgayBatDau)
                .Select(x => new AcademicYearDto(x.Id, x.TenNamHoc, x.NgayBatDau, x.NgayKetThuc, x.IsActive))
                .ToListAsync(cancellationToken);
        }

        public async Task<AcademicYearDto> CreateAcademicYearAsync(CreateAcademicYearRequest request, CancellationToken cancellationToken = default)
        {
            if (request.EndDate < request.StartDate)
                throw new ValidationException("EndDate", "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");

            if (await _dbContext.NamHocs.AnyAsync(x => x.TenNamHoc == request.Name.Trim(), cancellationToken))
                throw new ConflictException($"Năm học '{request.Name}' đã tồn tại.");

            var entity = new NamHoc(request.Name, request.StartDate, request.EndDate);
            _dbContext.NamHocs.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new AcademicYearDto(entity.Id, entity.TenNamHoc, entity.NgayBatDau, entity.NgayKetThuc, entity.IsActive);
        }

        public async Task<IReadOnlyList<SemesterDto>> GetSemestersAsync(int? academicYearId = null, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.HocKys.AsNoTracking().Include(x => x.NamHoc).AsQueryable();
            if (academicYearId.HasValue)
                query = query.Where(x => x.MaNamHoc == academicYearId.Value);

            return await query
                .OrderBy(x => x.MaNamHoc).ThenBy(x => x.HocKySo)
                .Select(x => new SemesterDto(x.Id, x.TenHocKy, x.HocKySo, x.MaNamHoc, x.NamHoc.TenNamHoc, x.NgayBatDau, x.NgayKetThuc, x.IsActive))
                .ToListAsync(cancellationToken);
        }

        public async Task<SemesterDto> CreateSemesterAsync(CreateSemesterRequest request, CancellationToken cancellationToken = default)
        {
            if (request.SemesterNumber < 1 || request.SemesterNumber > 3)
                throw new ValidationException("SemesterNumber", "Học kỳ số phải từ 1 đến 3.");
            if (request.EndDate < request.StartDate)
                throw new ValidationException("EndDate", "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");

            var year = await _dbContext.NamHocs.FindAsync(new object[] { request.AcademicYearId }, cancellationToken);
            if (year == null) throw new NotFoundException("Năm học", request.AcademicYearId);

            if (await _dbContext.HocKys.AnyAsync(x => x.MaNamHoc == request.AcademicYearId && x.HocKySo == request.SemesterNumber, cancellationToken))
                throw new ConflictException($"Học kỳ {request.SemesterNumber} của năm học '{year.TenNamHoc}' đã tồn tại.");

            var entity = new HocKy(request.Name, request.SemesterNumber, request.AcademicYearId, request.StartDate, request.EndDate);
            _dbContext.HocKys.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new SemesterDto(entity.Id, entity.TenHocKy, entity.HocKySo, entity.MaNamHoc, year.TenNamHoc, entity.NgayBatDau, entity.NgayKetThuc, entity.IsActive);
        }

        // Degrees & Ranks
        public async Task<IReadOnlyList<DegreeDto>> GetDegreesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.HocVis.AsNoTracking().Select(x => new DegreeDto(x.Id, x.TenHocVi)).ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<AcademicRankDto>> GetAcademicRanksAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.HocHams.AsNoTracking().Select(x => new AcademicRankDto(x.Id, x.TenHocHam)).ToListAsync(cancellationToken);
        }

        // Lecturer
        public async Task<PagedResult<LecturerDto>> GetLecturersAsync(PaginationParams @params, int? departmentId = null, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.GiangViens
                .AsNoTracking()
                .Include(x => x.BoMon)
                .Include(x => x.HocVi)
                .Include(x => x.HocHam)
                .AsQueryable();

            if (departmentId.HasValue)
                query = query.Where(x => x.MaBoMon == departmentId.Value);

            if (!string.IsNullOrWhiteSpace(@params.Search))
            {
                var s = @params.Search.Trim().ToLower();
                query = query.Where(x => x.MaGVCode.ToLower().Contains(s) || x.HoTen.ToLower().Contains(s) || x.Email.ToLower().Contains(s));
            }

            query = @params.SortDirection.ToLower() == "desc" ? query.OrderByDescending(x => x.HoTen) : query.OrderBy(x => x.HoTen);

            return await query.ToPagedResultAsync(
                @params.PageNumber,
                @params.PageSize,
                x => new LecturerDto(
                    x.Id,
                    x.MaGVCode,
                    x.HoTen,
                    x.Email,
                    x.SoDienThoai,
                    x.MaBoMon,
                    x.BoMon.TenBoMon,
                    x.MaHocVi,
                    x.HocVi.TenHocVi,
                    x.MaHocHam,
                    x.HocHam != null ? x.HocHam.TenHocHam : null,
                    x.MaTaiKhoan,
                    x.IsActive),
                cancellationToken);
        }

        public async Task<LecturerDto> GetLecturerByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var x = await _dbContext.GiangViens
                .AsNoTracking()
                .Include(l => l.BoMon)
                .Include(l => l.HocVi)
                .Include(l => l.HocHam)
                .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

            if (x == null) throw new NotFoundException("Giảng viên", id);

            return new LecturerDto(
                x.Id,
                x.MaGVCode,
                x.HoTen,
                x.Email,
                x.SoDienThoai,
                x.MaBoMon,
                x.BoMon.TenBoMon,
                x.MaHocVi,
                x.HocVi.TenHocVi,
                x.MaHocHam,
                x.HocHam != null ? x.HocHam.TenHocHam : null,
                x.MaTaiKhoan,
                x.IsActive);
        }

        /// <summary>
        /// Từ tài khoản đăng nhập tìm ra hồ sơ giảng viên tương ứng.
        /// </summary>
        public async Task<LecturerDto> GetLecturerByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
        {
            var x = await _dbContext.GiangViens
                .AsNoTracking()
                .Include(l => l.BoMon)
                .Include(l => l.HocVi)
                .Include(l => l.HocHam)
                .FirstOrDefaultAsync(l => l.MaTaiKhoan == accountId, cancellationToken);

            if (x == null) throw new NotFoundException($"Không tìm thấy hồ sơ giảng viên liên kết với tài khoản ID {accountId}.");

            return new LecturerDto(
                x.Id,
                x.MaGVCode,
                x.HoTen,
                x.Email,
                x.SoDienThoai,
                x.MaBoMon,
                x.BoMon.TenBoMon,
                x.MaHocVi,
                x.HocVi.TenHocVi,
                x.MaHocHam,
                x.HocHam != null ? x.HocHam.TenHocHam : null,
                x.MaTaiKhoan,
                x.IsActive);
        }

        public async Task<LecturerDto> CreateLecturerAsync(CreateLecturerRequest request, CancellationToken cancellationToken = default)
        {
            if (await _dbContext.GiangViens.AnyAsync(x => x.MaGVCode == request.Code.Trim(), cancellationToken))
                throw new ConflictException($"Mã giảng viên '{request.Code}' đã tồn tại.");
            if (await _dbContext.GiangViens.AnyAsync(x => x.Email == request.Email.Trim().ToLowerInvariant(), cancellationToken))
                throw new ConflictException($"Email '{request.Email}' đã được sử dụng bởi giảng viên khác.");
            if (await _dbContext.GiangViens.AnyAsync(x => x.MaTaiKhoan == request.AccountId, cancellationToken))
                throw new ConflictException($"Tài khoản ID {request.AccountId} đã được liên kết với giảng viên khác.");
            await EnsureAccountHasRoleAsync(request.AccountId, AppRoles.GiangVien, cancellationToken);

            if (!await _dbContext.BoMons.AnyAsync(x => x.Id == request.DepartmentId, cancellationToken))
                throw new NotFoundException("Bộ môn", request.DepartmentId);
            if (!await _dbContext.HocVis.AnyAsync(x => x.Id == request.DegreeId, cancellationToken))
                throw new NotFoundException("Học vị", request.DegreeId);
            if (request.AcademicRankId.HasValue && !await _dbContext.HocHams.AnyAsync(x => x.Id == request.AcademicRankId.Value, cancellationToken))
                throw new NotFoundException("Học hàm", request.AcademicRankId.Value);

            var entity = new GiangVien(
                request.Code,
                request.FullName,
                request.Email,
                request.PhoneNumber,
                request.DepartmentId,
                request.DegreeId,
                request.AcademicRankId,
                request.AccountId);

            _dbContext.GiangViens.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return await GetLecturerByIdAsync(entity.Id, cancellationToken);
        }

        public async Task<LecturerDto> UpdateLecturerAsync(int id, UpdateLecturerRequest request, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.GiangViens.FindAsync(new object[] { id }, cancellationToken);
            if (entity == null) throw new NotFoundException("Giảng viên", id);

            if (await _dbContext.GiangViens.AnyAsync(x => x.Id != id && x.Email == request.Email.Trim().ToLowerInvariant(), cancellationToken))
                throw new ConflictException($"Email '{request.Email}' đã được sử dụng bởi giảng viên khác.");

            if (!await _dbContext.BoMons.AnyAsync(x => x.Id == request.DepartmentId, cancellationToken))
                throw new NotFoundException("Bộ môn", request.DepartmentId);
            if (!await _dbContext.HocVis.AnyAsync(x => x.Id == request.DegreeId, cancellationToken))
                throw new NotFoundException("Học vị", request.DegreeId);
            if (request.AcademicRankId.HasValue && !await _dbContext.HocHams.AnyAsync(x => x.Id == request.AcademicRankId.Value, cancellationToken))
                throw new NotFoundException("Học hàm", request.AcademicRankId.Value);

            entity.Update(request.FullName, request.Email, request.PhoneNumber, request.DepartmentId, request.DegreeId, request.AcademicRankId);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return await GetLecturerByIdAsync(entity.Id, cancellationToken);
        }

        // Student
        public async Task<PagedResult<StudentDto>> GetStudentsAsync(PaginationParams @params, int? classId = null, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.SinhViens
                .AsNoTracking()
                .Include(x => x.Lop)
                .AsQueryable();

            if (classId.HasValue)
                query = query.Where(x => x.MaLop == classId.Value);

            if (!string.IsNullOrWhiteSpace(@params.Search))
            {
                var s = @params.Search.Trim().ToLower();
                query = query.Where(x => x.MaSVCode.ToLower().Contains(s) || x.HoTen.ToLower().Contains(s) || x.Email.ToLower().Contains(s));
            }

            query = @params.SortDirection.ToLower() == "desc" ? query.OrderByDescending(x => x.HoTen) : query.OrderBy(x => x.HoTen);

            return await query.ToPagedResultAsync(
                @params.PageNumber,
                @params.PageSize,
                x => new StudentDto(
                    x.Id,
                    x.MaSVCode,
                    x.HoTen,
                    x.NgaySinh,
                    x.GioiTinh,
                    x.Email,
                    x.SoDienThoai,
                    x.MaLop,
                    x.Lop.TenLop,
                    x.MaTaiKhoan,
                    x.IsActive),
                cancellationToken);
        }

        public async Task<StudentDto> GetStudentByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var x = await _dbContext.SinhViens
                .AsNoTracking()
                .Include(s => s.Lop)
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

            if (x == null) throw new NotFoundException("Sinh viên", id);

            return new StudentDto(
                x.Id,
                x.MaSVCode,
                x.HoTen,
                x.NgaySinh,
                x.GioiTinh,
                x.Email,
                x.SoDienThoai,
                x.MaLop,
                x.Lop.TenLop,
                x.MaTaiKhoan,
                x.IsActive);
        }

        /// <summary>
        /// Từ tài khoản đăng nhập tìm ra hồ sơ sinh viên tương ứng.
        /// </summary>
        public async Task<StudentDto> GetStudentByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
        {
            var x = await _dbContext.SinhViens
                .AsNoTracking()
                .Include(s => s.Lop)
                .FirstOrDefaultAsync(s => s.MaTaiKhoan == accountId, cancellationToken);

            if (x == null) throw new NotFoundException($"Không tìm thấy hồ sơ sinh viên liên kết với tài khoản ID {accountId}.");

            return new StudentDto(
                x.Id,
                x.MaSVCode,
                x.HoTen,
                x.NgaySinh,
                x.GioiTinh,
                x.Email,
                x.SoDienThoai,
                x.MaLop,
                x.Lop.TenLop,
                x.MaTaiKhoan,
                x.IsActive);
        }

        public async Task<StudentDto> CreateStudentAsync(CreateStudentRequest request, CancellationToken cancellationToken = default)
        {
            if (await _dbContext.SinhViens.AnyAsync(x => x.MaSVCode == request.Code.Trim(), cancellationToken))
                throw new ConflictException($"Mã sinh viên '{request.Code}' đã tồn tại.");
            if (await _dbContext.SinhViens.AnyAsync(x => x.Email == request.Email.Trim().ToLowerInvariant(), cancellationToken))
                throw new ConflictException($"Email '{request.Email}' đã được sử dụng bởi sinh viên khác.");
            if (await _dbContext.SinhViens.AnyAsync(x => x.MaTaiKhoan == request.AccountId, cancellationToken))
                throw new ConflictException($"Tài khoản ID {request.AccountId} đã được liên kết với sinh viên khác.");
            await EnsureAccountHasRoleAsync(request.AccountId, AppRoles.SinhVien, cancellationToken);

            if (!await _dbContext.Lops.AnyAsync(x => x.Id == request.ClassId, cancellationToken))
                throw new NotFoundException("Lớp", request.ClassId);

            var entity = new SinhVien(
                request.Code,
                request.FullName,
                request.DateOfBirth,
                request.Gender,
                request.Email,
                request.PhoneNumber,
                request.ClassId,
                request.AccountId);

            _dbContext.SinhViens.Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return await GetStudentByIdAsync(entity.Id, cancellationToken);
        }

        public async Task<StudentDto> UpdateStudentAsync(int id, UpdateStudentRequest request, CancellationToken cancellationToken = default)
        {
            var entity = await _dbContext.SinhViens.FindAsync(new object[] { id }, cancellationToken);
            if (entity == null) throw new NotFoundException("Sinh viên", id);

            if (await _dbContext.SinhViens.AnyAsync(x => x.Id != id && x.Email == request.Email.Trim().ToLowerInvariant(), cancellationToken))
                throw new ConflictException($"Email '{request.Email}' đã được sử dụng bởi sinh viên khác.");

            if (!await _dbContext.Lops.AnyAsync(x => x.Id == request.ClassId, cancellationToken))
                throw new NotFoundException("Lớp", request.ClassId);

            entity.Update(request.FullName, request.DateOfBirth, request.Gender, request.Email, request.PhoneNumber, request.ClassId);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return await GetStudentByIdAsync(entity.Id, cancellationToken);
        }

        /// <summary>
        /// Hồ sơ GV/SV quyết định quyền của tài khoản (xem CurrentActor), nên tài khoản liên kết phải tồn tại và đúng vai trò.
        /// </summary>
        private async Task EnsureAccountHasRoleAsync(int accountId, string role, CancellationToken cancellationToken)
        {
            var accountRole = await _dbContext.TaiKhoans
                .Where(x => x.Id == accountId)
                .Select(x => x.VaiTro.TenVaiTro)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Tài khoản", accountId);
            if (accountRole != role)
                throw new BusinessRuleException($"Tài khoản ID {accountId} có vai trò {accountRole}, không phải {role}.");
        }
    }
}
