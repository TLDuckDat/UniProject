import { API_BASE_URL } from '../config.js';
import { auth } from './auth.js';
import { toast } from './toast.js';

export class ApiError extends Error {
  constructor(status, message, body) {
    super(message);
    this.status = status;
    this.body = body;
  }
}

function buildUrl(path, query) {
  const url = new URL(API_BASE_URL + path);
  if (query) {
    for (const [k, v] of Object.entries(query)) {
      if (v !== null && v !== undefined && v !== '') url.searchParams.set(k, v);
    }
  }
  return url.toString();
}

// Backend trả lỗi dạng { success, message, errors: { field: [msg] } }
function errorMessage(body, fallback) {
  let msg = body?.message || fallback;
  const errors = body?.errors;
  if (errors && typeof errors === 'object') {
    const list = Object.values(errors).flat().filter(Boolean);
    if (list.length) msg = `${msg} (${list.join(', ')})`;
  }
  return msg;
}

let refreshing = null;

async function refreshTokens() {
  if (!auth.refreshToken || !auth.token) return false;
  refreshing ??= fetch(buildUrl('/auth/refresh'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ accessToken: auth.token, refreshToken: auth.refreshToken }),
  })
    .then(async (res) => {
      if (!res.ok) return false;
      const body = await res.json();
      if (!body?.data?.accessToken) return false;
      auth.setTokens(body.data.accessToken, body.data.refreshToken);
      return true;
    })
    .catch(() => false)
    .finally(() => { refreshing = null; });
  return refreshing;
}

function logoutAndRedirect() {
  auth.clear();
  if (!location.hash.startsWith('#/login')) location.hash = '#/login';
}

async function request(method, path, { body, query, silent = false, raw = false, retried = false } = {}) {
  const headers = {};
  if (auth.token) headers.Authorization = `Bearer ${auth.token}`;
  const isForm = body instanceof FormData;
  if (body !== undefined && !isForm) headers['Content-Type'] = 'application/json';

  let res;
  try {
    res = await fetch(buildUrl(path, query), {
      method,
      headers,
      body: body === undefined ? undefined : (isForm ? body : JSON.stringify(body)),
    });
  } catch {
    if (!silent) toast.error('Không kết nối được tới máy chủ. Kiểm tra backend đã chạy chưa.', 'Lỗi kết nối');
    throw new ApiError(0, 'Network error');
  }

  const isAuthCall = path.startsWith('/auth/login') || path.startsWith('/auth/refresh');
  if (res.status === 401 && !isAuthCall) {
    if (!retried && await refreshTokens()) {
      return request(method, path, { body, query, silent, raw, retried: true });
    }
    logoutAndRedirect();
    throw new ApiError(401, 'Phiên đăng nhập đã hết hạn.');
  }

  if (raw && res.ok) return res;

  const text = await res.text();
  let data = null;
  if (text) {
    try { data = JSON.parse(text); } catch { data = text; }
  }

  if (!res.ok) {
    const msg = errorMessage(data, 'Đã có lỗi xảy ra trên hệ thống.');
    if (!silent) {
      switch (res.status) {
        case 400: toast.error(msg, 'Yêu cầu không hợp lệ'); break;
        case 401: toast.error(msg || 'Tên đăng nhập hoặc mật khẩu không chính xác.', 'Đăng nhập thất bại'); break;
        case 403: toast.error(data?.message || 'Bạn không có quyền thực hiện thao tác này.', 'Truy cập bị từ chối'); break;
        case 404: toast.warning(msg, 'Không tìm thấy'); break;
        case 409: toast.warning(msg, 'Xung đột dữ liệu'); break;
        case 429: toast.warning('Bạn thao tác quá nhanh, vui lòng thử lại sau ít phút.', 'Quá nhiều yêu cầu'); break;
        case 503: toast.error(msg, 'Mất kết nối cơ sở dữ liệu'); break;
        default: toast.error('Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.', 'Lỗi máy chủ');
      }
    }
    throw new ApiError(res.status, msg, data);
  }
  return data;
}

export const api = {
  get: (path, query, opts) => request('GET', path, { query, ...opts }),
  post: (path, body, opts) => request('POST', path, { body: body ?? {}, ...opts }),
  put: (path, body, opts) => request('PUT', path, { body: body ?? {}, ...opts }),
  patch: (path, body, opts) => request('PATCH', path, { body: body ?? {}, ...opts }),
  delete: (path, opts) => request('DELETE', path, opts),
  upload: (path, formData, opts) => request('POST', path, { body: formData, ...opts }),

  async download(path, fileName) {
    const res = await request('GET', path, { raw: true });
    const blob = await res.blob();
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName || 'tai-lieu';
    a.click();
    URL.revokeObjectURL(url);
  },
};

// Danh sách phân trang: backend trả thẳng PagedResult { items, pageNumber, pageSize, totalItems, totalPages }
export function paged(res) {
  const p = res?.items ? res : (res?.data?.items ? res.data : null);
  return {
    items: p?.items ?? [],
    pageNumber: p?.pageNumber ?? 1,
    pageSize: p?.pageSize ?? 20,
    totalItems: p?.totalItems ?? p?.totalCount ?? 0,
    totalPages: p?.totalPages ?? 1,
  };
}

// Kết quả đơn lẻ: { success, message, data }
export const unwrap = (res) => (res && typeof res === 'object' && 'data' in res ? res.data : res);
