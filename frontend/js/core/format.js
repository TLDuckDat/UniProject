// Định dạng ngày giờ và nhãn trạng thái tiếng Việt

const pad = (n) => String(n).padStart(2, '0');

export function fmtDate(value) {
  if (!value) return '—';
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return '—';
  return `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()}`;
}

// Backend lưu thời điểm theo UTC nhưng trả về chuỗi không kèm múi giờ ("2026-10-08T04:05:00")
export const parseUtc = (value) =>
  typeof value === 'string' && value.includes('T') && !/[zZ]|[+-]\d{2}:?\d{2}$/.test(value) ? new Date(value + 'Z') : new Date(value);

export function fmtDateTime(value) {
  if (!value) return '—';
  const d = parseUtc(value);
  if (Number.isNaN(d.getTime())) return '—';
  return `${pad(d.getHours())}:${pad(d.getMinutes())} · ${fmtDate(d)}`;
}

export function fmtTime(span) {
  // TimeSpan "08:30:00" -> "08:30"
  return span ? String(span).slice(0, 5) : '—';
}

// Giá trị cho <input type="date">
export function toDateInput(value) {
  if (!value) return '';
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return '';
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}

export function fmtScore(v) {
  return v === null || v === undefined ? '—' : Number(v).toFixed(2);
}

const STATUS = {
  // Đợt đồ án
  CHUA_MO: ['Chưa mở', 'default'],
  DANG_MO_DANG_KY: ['Đang mở đăng ký', 'accent'],
  DANG_THUC_HIEN: ['Đang thực hiện', 'info'],
  DANG_BAO_VE: ['Đang bảo vệ', 'warning'],
  DA_KET_THUC: ['Đã kết thúc', 'default'],
  DA_HUY: ['Đã hủy', 'danger'],
  // Đề tài
  CHO_DUYET: ['Chờ duyệt', 'warning'],
  DA_DUYET: ['Đã duyệt', 'success'],
  TU_CHOI: ['Từ chối', 'danger'],
  NGUNG_SU_DUNG: ['Ngừng sử dụng', 'default'],
  DANG_MO: ['Đang mở', 'accent'],
  DA_DONG: ['Đã đóng', 'default'],
  TAM_KHOA: ['Tạm khóa', 'warning'],
  // Đăng ký
  NHAP: ['Nháp', 'default'],
  DUOC_CHON: ['Được chọn', 'success'],
  // Đồ án
  CHO_THUC_HIEN: ['Chờ thực hiện', 'default'],
  CHO_BAO_VE: ['Chờ bảo vệ', 'warning'],
  DA_BAO_VE: ['Đã bảo vệ', 'info'],
  HOAN_THANH: ['Hoàn thành', 'success'],
  TAM_DUNG: ['Tạm dừng', 'warning'],
  // Yêu cầu chỉnh sửa
  CHUA_THUC_HIEN: ['Chưa thực hiện', 'warning'],
  DA_HOAN_THANH: ['Đã hoàn thành', 'info'],
  DA_XAC_NHAN: ['Đã xác nhận', 'success'],
  // Hội đồng / lịch
  CHUA_BAO_VE: ['Chưa bảo vệ', 'default'],
  LEN_LICH: ['Đã lên lịch', 'info'],
  // Kết quả
  CHO_KET_QUA: ['Chờ kết quả', 'default'],
  DAT: ['Đạt', 'success'],
  KHONG_DAT: ['Không đạt', 'danger'],
  // Phản biện
  CHUA_PHAN_BIEN: ['Chưa phản biện', 'default'],
  DANG_PHAN_BIEN: ['Đang phản biện', 'info'],
  DA_PHAN_BIEN: ['Đã phản biện', 'success'],
};

export const statusLabel = (s) => STATUS[s]?.[0] ?? s ?? '—';
export const statusVariant = (s) => STATUS[s]?.[1] ?? 'default';

export const LABELS = {
  projectKind: { DO_AN_CO_SO: 'Đồ án cơ sở', DO_AN_CHUYEN_NGANH: 'Đồ án chuyên ngành', DO_AN_MON_HOC: 'Đồ án môn học', DO_AN_TOT_NGHIEP: 'Đồ án tốt nghiệp' },
  gradingType: { HUONG_DAN: 'Hướng dẫn', PHAN_BIEN: 'Phản biện', HOI_DONG: 'Hội đồng' },
  committeeRole: { CHU_TICH: 'Chủ tịch', THU_KY: 'Thư ký', UY_VIEN: 'Ủy viên' },
  memberRole: { TRUONG_NHOM: 'Trưởng nhóm', THANH_VIEN: 'Thành viên' },
  advisorType: { CHINH: 'Hướng dẫn chính', PHU: 'Hướng dẫn phụ' },
  evaluation: { KHONG_DAT: 'Không đạt', CAN_CAI_THIEN: 'Cần cải thiện', DAT: 'Đạt', TOT: 'Tốt', RAT_TOT: 'Rất tốt' },
  documentType: {
    DE_CUONG: 'Đề cương', BAO_CAO_TIEN_DO: 'Báo cáo tiến độ', BAO_CAO: 'Báo cáo cuối kỳ',
    SLIDE: 'Slide', SOURCE_CODE: 'Mã nguồn', PHU_LUC: 'Phụ lục', KHAC: 'Khác',
  },
  rank: { XUAT_SAC: 'Xuất sắc', GIOI: 'Giỏi', KHA: 'Khá', TRUNG_BINH: 'Trung bình', YEU: 'Yếu' },
  changeType: { DOI_DE_TAI: 'Đổi đề tài', DOI_NHOM: 'Đổi nhóm' },
  priority: { BINH_THUONG: 'Bình thường', QUAN_TRONG: 'Quan trọng', KHAN_CAP: 'Khẩn cấp' },
  role: { ADMIN: 'Quản trị viên', GIANG_VIEN: 'Giảng viên', SINH_VIEN: 'Sinh viên' },
};

export const label = (group, key) => LABELS[group]?.[key] ?? key ?? '—';
