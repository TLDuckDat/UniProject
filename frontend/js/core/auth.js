const TOKEN_KEY = 'uniproject_access_token';
const REFRESH_TOKEN_KEY = 'uniproject_refresh_token';
const USER_KEY = 'uniproject_user';

export const ROLES = {
  ADMIN: 'ADMIN',
  GIANG_VIEN: 'GIANG_VIEN',
  SINH_VIEN: 'SINH_VIEN',
};

const ROLE_NAMES = {
  ADMIN: 'Quản trị viên',
  GIANG_VIEN: 'Giảng viên',
  SINH_VIEN: 'Sinh viên',
};

function readUser() {
  try { return JSON.parse(localStorage.getItem(USER_KEY)); } catch { return null; }
}

export const auth = {
  get token() { return localStorage.getItem(TOKEN_KEY); },
  get refreshToken() { return localStorage.getItem(REFRESH_TOKEN_KEY); },
  get user() { return readUser(); },
  get role() { return (readUser()?.role || '').toUpperCase(); },
  get isAuthenticated() { return !!localStorage.getItem(TOKEN_KEY); },
  get isAdmin() { return this.role === ROLES.ADMIN; },
  get isLecturer() { return this.role === ROLES.GIANG_VIEN; },
  get isStudent() { return this.role === ROLES.SINH_VIEN; },
  get roleName() { return ROLE_NAMES[this.role] || 'Khách'; },

  setAuth(res) {
    localStorage.setItem(TOKEN_KEY, res.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, res.refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify({
      id: res.userId,
      username: res.username,
      fullName: res.fullName,
      email: res.email,
      role: res.role.toUpperCase(),
    }));
  },

  setTokens(accessToken, refreshToken) {
    localStorage.setItem(TOKEN_KEY, accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
  },

  clear() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  },

  hasRole(...roles) {
    return roles.includes(this.role);
  },
};

export function initials(name) {
  if (!name) return 'U';
  const parts = name.trim().split(/\s+/);
  if (parts.length === 1) return parts[0].charAt(0).toUpperCase();
  return (parts[0].charAt(0) + parts[parts.length - 1].charAt(0)).toUpperCase();
}
