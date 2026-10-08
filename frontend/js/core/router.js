import { auth, ROLES } from './auth.js';
import { toast } from './toast.js';

const { ADMIN, GIANG_VIEN, SINH_VIEN } = ROLES;

// page: tên file trong js/pages; roles: bỏ trống = mọi tài khoản đã đăng nhập
export const routes = [
  { path: '/login', page: 'login', public: true },
  { path: '/dashboard', page: 'dashboard' },

  { path: '/academic/faculties', page: 'faculties', roles: [ADMIN] },
  { path: '/academic/lecturers', page: 'lecturers', roles: [ADMIN, GIANG_VIEN] },
  { path: '/academic/students', page: 'students', roles: [ADMIN, GIANG_VIEN] },

  { path: '/periods', page: 'periods' },
  { path: '/periods/:id', page: 'period-detail' },

  { path: '/topics', page: 'topics' },
  { path: '/topics/proposals', page: 'topic-proposal', roles: [ADMIN, GIANG_VIEN, SINH_VIEN] },
  { path: '/topics/approval', page: 'topic-approval', roles: [ADMIN, GIANG_VIEN] },

  { path: '/registrations/wizard', page: 'registration-wizard', roles: [SINH_VIEN] },
  { path: '/registrations/management', page: 'registration-management', roles: [ADMIN, GIANG_VIEN] },

  { path: '/projects', page: 'projects' },
  { path: '/projects/:id', page: 'project-detail' },

  { path: '/defense', page: 'defense' },
  { path: '/grading/entry', page: 'grading-entry', roles: [GIANG_VIEN] },
  { path: '/grading/results', page: 'grading-results' },

  { path: '/notifications', page: 'notifications' },
  { path: '/admin/users', page: 'users', roles: [ADMIN] },
  { path: '/profile/change-password', page: 'change-password' },
];

function match(path) {
  for (const route of routes) {
    const names = [];
    const pattern = new RegExp('^' + route.path.replace(/:(\w+)/g, (_, n) => { names.push(n); return '([^/]+)'; }) + '/?$');
    const m = path.match(pattern);
    if (m) {
      const params = Object.fromEntries(names.map((n, i) => [n, decodeURIComponent(m[i + 1])]));
      return { route, params };
    }
  }
  return null;
}

export function currentLocation() {
  const hash = location.hash.replace(/^#/, '') || '/dashboard';
  const [path, qs = ''] = hash.split('?');
  return { path, query: Object.fromEntries(new URLSearchParams(qs)) };
}

export function navigate(path) {
  location.hash = '#' + path;
}

/**
 * Xác định route cần hiển thị, áp dụng guard đăng nhập / vai trò.
 * Trả về null nếu đã chuyển hướng sang route khác.
 */
export function resolve() {
  const { path, query } = currentLocation();
  const found = match(path);

  if (!found) {
    navigate('/dashboard');
    return null;
  }
  const { route, params } = found;

  if (!route.public && !auth.isAuthenticated) {
    navigate('/login?returnUrl=' + encodeURIComponent(path));
    return null;
  }
  if (route.public && auth.isAuthenticated) {
    navigate('/dashboard');
    return null;
  }
  if (route.roles && !route.roles.includes(auth.role)) {
    toast.error('Bạn không có quyền truy cập vào chức năng này.');
    navigate('/dashboard');
    return null;
  }
  return { route, params, query, path };
}
