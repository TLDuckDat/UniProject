import { HttpErrorResponse } from '@angular/common/http';

/**
 * ProblemDetails (RFC 9457) do API UPM trả về, có thêm `code` nghiệp vụ và `traceId`.
 * Ví dụ: `{ status: 422, title: 'Vi phạm quy định nghiệp vụ.', detail: '...', code: 'UPM_A02_NO_QUA_10_TC' }`.
 */
export interface ApiProblemDetails {
  type?: string | null;
  title?: string | null;
  status?: number | null;
  detail?: string | null;
  instance?: string | null;
  code?: string | null;
  traceId?: string | null;
  /** Chỉ có ở lỗi validation (400): tên trường → danh sách thông báo. */
  errors?: Record<string, string[]> | null;
}

export function isProblemDetails(value: unknown): value is ApiProblemDetails {
  if (value === null || typeof value !== 'object') {
    return false;
  }
  const candidate = value as Record<string, unknown>;
  return typeof candidate['title'] === 'string' || typeof candidate['status'] === 'number';
}

const STATUS_MESSAGES: Record<number, string> = {
  400: 'Yêu cầu không hợp lệ.',
  401: 'Bạn chưa đăng nhập hoặc phiên đăng nhập đã hết hạn.',
  403: 'Bạn không có quyền thực hiện thao tác này.',
  404: 'Không tìm thấy tài nguyên yêu cầu.',
  408: 'Máy chủ phản hồi quá lâu. Vui lòng thử lại.',
  409: 'Dữ liệu bị xung đột, vui lòng tải lại và thử lại.',
  413: 'Dữ liệu gửi lên quá lớn.',
  422: 'Vi phạm quy định nghiệp vụ.',
  429: 'Bạn thao tác quá nhanh. Vui lòng thử lại sau ít phút.',
};

export const NETWORK_ERROR_MESSAGE =
  'Không kết nối được máy chủ. Vui lòng kiểm tra mạng hoặc thử lại sau.';
export const SERVER_ERROR_MESSAGE = 'Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.';

/** Chuyển lỗi HTTP thành một câu thông báo tiếng Việt để hiển thị cho người dùng. */
export function toVietnameseMessage(error: HttpErrorResponse): string {
  if (error.status === 0) {
    return NETWORK_ERROR_MESSAGE;
  }

  const body: unknown = error.error;
  if (isProblemDetails(body)) {
    const validationMessages = Object.values(body.errors ?? {}).flat();
    if (validationMessages.length > 0) {
      return [body.title, ...validationMessages.slice(0, 3)].filter(Boolean).join(' ');
    }
    const message = body.detail || body.title;
    if (message) {
      return message;
    }
  }

  return (
    STATUS_MESSAGES[error.status] ?? (error.status >= 500 ? SERVER_ERROR_MESSAGE : 'Đã xảy ra lỗi.')
  );
}
