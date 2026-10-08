import { html, setHtml, on, icons, $ } from '../core/dom.js';
import { auth, initials } from '../core/auth.js';
import { api, paged, unwrap } from '../core/api.js';
import { navigate } from '../core/router.js';
import { toast } from '../core/toast.js';
import { clearLookups } from '../core/lookup.js';
import { APP_NAME, APP_SUBTITLE, APP_LOGO } from '../config.js';
import { parseUtc } from '../core/format.js';

const SIDEBAR_KEY = 'uniproject_sidebar_collapsed';

function navSections() {
  if (auth.isAdmin) {
    return [
      { items: [{ label: 'Tổng quan', route: '/dashboard' }] },
      { title: 'Học vụ', items: [
        { label: 'Khoa & Bộ môn', route: '/academic/faculties' },
        { label: 'Giảng viên', route: '/academic/lecturers' },
        { label: 'Sinh viên', route: '/academic/students' },
      ] },
      { title: 'Đồ án', items: [
        { label: 'Đợt đồ án', route: '/periods' },
        { label: 'Đề tài', route: '/topics' },
        { label: 'Duyệt đề tài', route: '/topics/approval' },
        { label: 'Đăng ký', route: '/registrations/management' },
        { label: 'Đồ án thực hiện', route: '/projects' },
      ] },
      { title: 'Bảo vệ & Đánh giá', items: [
        { label: 'Hội đồng & Lịch', route: '/defense' },
        { label: 'Bảng điểm tổng kết', route: '/grading/results' },
      ] },
      { title: 'Hệ thống', items: [
        { label: 'Thông báo', route: '/notifications' },
        { label: 'Quản trị tài khoản', route: '/admin/users' },
      ] },
    ];
  }
  if (auth.isLecturer) {
    return [
      { items: [{ label: 'Tổng quan', route: '/dashboard' }] },
      { title: 'Đề tài & Đồ án', items: [
        { label: 'Danh mục đề tài', route: '/topics' },
        { label: 'Đề xuất đề tài', route: '/topics/proposals' },
        { label: 'Duyệt đề tài', route: '/topics/approval' },
        { label: 'Đồ án hướng dẫn', route: '/projects' },
        { label: 'Đăng ký chờ xử lý', route: '/registrations/management' },
      ] },
      { title: 'Bảo vệ & Chấm điểm', items: [
        { label: 'Phản biện & Hội đồng', route: '/defense' },
        { label: 'Chấm điểm đồ án', route: '/grading/entry' },
        { label: 'Kết quả đồ án', route: '/grading/results' },
      ] },
      { title: 'Danh bạ', items: [
        { label: 'Giảng viên', route: '/academic/lecturers' },
        { label: 'Sinh viên', route: '/academic/students' },
      ] },
      { title: 'Hệ thống', items: [{ label: 'Thông báo', route: '/notifications' }] },
    ];
  }
  return [
    { items: [{ label: 'Tổng quan', route: '/dashboard' }] },
    { title: 'Đồ án', items: [
      { label: 'Đợt đồ án', route: '/periods' },
      { label: 'Khám phá đề tài', route: '/topics' },
      { label: 'Đề xuất đề tài', route: '/topics/proposals' },
      { label: 'Đăng ký đề tài nhóm', route: '/registrations/wizard' },
      { label: 'Đồ án của tôi', route: '/projects' },
    ] },
    { title: 'Lịch & Kết quả', items: [
      { label: 'Lịch bảo vệ', route: '/defense' },
      { label: 'Kết quả đồ án', route: '/grading/results' },
    ] },
    { title: 'Hệ thống', items: [{ label: 'Thông báo', route: '/notifications' }] },
  ];
}

function sidebar(collapsed) {
  const user = auth.user;
  return html`
    <aside class="app-sidebar ${collapsed ? 'collapsed' : ''}" id="app-sidebar">
      <div class="sidebar-header">
        <a class="logo-area" href="#/dashboard">
          <img class="logo-badge" src="${APP_LOGO}" alt="${APP_NAME}" />
          <div class="logo-text">
            <span class="brand-title">${APP_NAME}</span>
            <span class="brand-sub">${APP_SUBTITLE}</span>
          </div>
        </a>
      </div>
      <nav class="sidebar-nav">
        ${navSections().map((section) => html`
          <div class="nav-section">
            ${section.title ? html`<div class="section-title">${section.title}</div>` : ''}
            <div class="section-items">
              ${section.items.map((item) => html`
                <a class="nav-item" href="#${item.route}" data-route="${item.route}" title="${item.label}">
                  <span class="nav-dot"></span>
                  <span class="nav-label">${item.label}</span>
                </a>`)}
            </div>
          </div>`)}
      </nav>
      <div class="sidebar-footer">
        <div class="user-card">
          <div class="user-avatar">${initials(user?.fullName)}</div>
          <div class="user-info">
            <div class="user-name">${user?.fullName}</div>
            <div class="user-role">${auth.roleName}</div>
          </div>
        </div>
      </div>
    </aside>`;
}

function topbar() {
  const user = auth.user;
  return html`
    <header class="app-topbar">
      <div class="topbar-left">
        <button class="btn btn-ghost btn-icon btn-sm" data-action="toggle-sidebar" title="Mở/đóng thanh menu">${icons.menu}</button>
        <div class="topbar-search">
          ${icons.search}
          <input type="text" class="search-input" id="topbar-search" placeholder="Tìm kiếm đề tài theo tên, mã..." />
        </div>
      </div>
      <div class="topbar-right">
        <button class="btn btn-ghost btn-icon btn-sm notif-btn" data-action="toggle-notifications" title="Thông báo">
          ${icons.bell}
          <span class="unread-dot hidden" id="unread-dot"></span>
        </button>
        <div class="user-dropdown-container">
          <button class="btn btn-ghost btn-sm user-btn" data-action="toggle-dropdown">
            <span class="user-name">${user?.fullName}</span>
            <span class="user-role-badge">${auth.roleName}</span>
          </button>
          <div class="dropdown-menu hidden" id="user-dropdown">
            <div class="dropdown-header">
              <div class="name">${user?.fullName}</div>
              <div class="email">${user?.email}</div>
            </div>
            <div class="dropdown-divider"></div>
            <a class="dropdown-item" href="#/profile/change-password">Đổi mật khẩu</a>
            <button class="dropdown-item text-danger" data-action="logout">Đăng xuất</button>
          </div>
        </div>
      </div>
    </header>`;
}

async function refreshUnread() {
  try {
    const count = unwrap(await api.get('/notifications/unread-count', null, { silent: true }));
    $('#unread-dot')?.classList.toggle('hidden', !(count > 0));
  } catch { /* bỏ qua */ }
}

function formatNotifTime(value) {
  const d = parseUtc(value);
  return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')} · ${d.getDate()}/${d.getMonth() + 1}`;
}

async function openNotificationPanel() {
  const backdrop = document.createElement('div');
  backdrop.className = 'notification-panel-backdrop';
  document.body.appendChild(backdrop);
  let items = [];

  const draw = (loading) => {
    const unread = items.filter((n) => !n.isRead).length;
    setHtml(backdrop, html`
      <div class="notification-panel">
        <div class="panel-header">
          <div class="panel-title-area">
            <h3 class="panel-title">Thông báo</h3>
            ${unread > 0 ? html`<span class="unread-badge">${unread} mới</span>` : ''}
          </div>
          <div class="panel-actions">
            ${unread > 0 ? html`<button class="btn btn-ghost btn-sm" data-action="mark-all">Đánh dấu tất cả đã đọc</button>` : ''}
            <button class="panel-close" data-action="close" aria-label="Đóng">&times;</button>
          </div>
        </div>
        <div class="panel-body">
          ${loading ? html`<div class="loading-state">Đang tải thông báo...</div>`
            : items.length === 0 ? html`<div class="empty-notifications"><p>Bạn không có thông báo nào.</p></div>`
            : html`<div class="notification-list">
                ${items.map((n) => html`
                  <div class="notification-item ${n.isRead ? '' : 'unread'}" data-id="${n.id}">
                    <div class="item-status">${n.isRead ? '' : html`<span class="dot"></span>`}</div>
                    <div class="item-content">
                      <div class="item-title">${n.title}</div>
                      <div class="item-text">${n.content}</div>
                      <div class="item-meta">
                        <span class="item-time">${formatNotifTime(n.createdDate)}</span>
                        ${n.priority !== 'BINH_THUONG' ? html`<span class="item-priority ${n.priority === 'KHAN_CAP' ? 'priority-high' : ''}">${n.priority === 'KHAN_CAP' ? 'Khẩn cấp' : 'Quan trọng'}</span>` : ''}
                      </div>
                    </div>
                  </div>`)}
              </div>`}
        </div>
      </div>`);
  };

  draw(true);
  backdrop.addEventListener('click', async (e) => {
    if (e.target === backdrop || e.target.closest('[data-action="close"]')) {
      backdrop.remove();
      return;
    }
    if (e.target.closest('[data-action="mark-all"]')) {
      await api.patch('/notifications/mark-all-read');
      items.forEach((n) => { n.isRead = true; });
      toast.success('Đã đánh dấu tất cả thông báo là đã đọc');
      draw(false);
      refreshUnread();
      return;
    }
    const row = e.target.closest('.notification-item');
    if (row) {
      const item = items.find((n) => n.id === Number(row.dataset.id));
      if (item && !item.isRead) {
        await api.patch(`/notifications/${item.id}/read`);
        item.isRead = true;
        draw(false);
        refreshUnread();
      }
    }
  });

  try {
    items = paged(await api.get('/notifications/my', { pageSize: 20 })).items;
  } catch { items = []; }
  draw(false);
}

let shellEl = null;
let outsideClickBound = false;

export function logout() {
  auth.clear();
  clearLookups();
  shellEl = null;
  navigate('/login');
}

/** Dựng khung ứng dụng (một lần) và trả về vùng nội dung để trang render vào. */
export function mountShell(app) {
  if (shellEl && app.contains(shellEl)) return $('#app-content', shellEl);

  let collapsed = localStorage.getItem(SIDEBAR_KEY) === '1' || window.innerWidth <= 768;
  setHtml(app, html`
    <div class="app-layout">
      ${sidebar(collapsed)}
      <div class="app-main">
        ${topbar()}
        <main class="app-content" id="app-content"></main>
      </div>
    </div>`);
  shellEl = $('.app-layout', app);

  on(shellEl, 'click', '[data-action]', (e, el) => {
    const action = el.dataset.action;
    if (action === 'toggle-sidebar') {
      collapsed = !collapsed;
      $('#app-sidebar').classList.toggle('collapsed', collapsed);
      localStorage.setItem(SIDEBAR_KEY, collapsed ? '1' : '0');
    } else if (action === 'toggle-dropdown') {
      e.stopPropagation();
      $('#user-dropdown').classList.toggle('hidden');
    } else if (action === 'toggle-notifications') {
      openNotificationPanel();
    } else if (action === 'logout') {
      logout();
    }
  });
  if (!outsideClickBound) {
    outsideClickBound = true;
    document.addEventListener('click', (e) => {
      if (!e.target.closest('.user-dropdown-container')) $('#user-dropdown')?.classList.add('hidden');
    });
  }
  $('#topbar-search', shellEl).addEventListener('keyup', (e) => {
    const q = e.target.value.trim();
    if (e.key === 'Enter' && q) navigate('/topics?search=' + encodeURIComponent(q));
  });

  refreshUnread();
  return $('#app-content', shellEl);
}

export function unmountShell() {
  shellEl = null;
}

export function setActiveNav(path) {
  if (!shellEl) return;
  // Mục khớp dài nhất được đánh dấu (vd /topics/approval thắng /topics)
  let best = null;
  for (const a of shellEl.querySelectorAll('.nav-item')) {
    const r = a.dataset.route;
    if ((path === r || path.startsWith(r + '/')) && (!best || r.length > best.dataset.route.length)) best = a;
    a.classList.remove('active');
  }
  best?.classList.add('active');
  $('#user-dropdown')?.classList.add('hidden');
}
