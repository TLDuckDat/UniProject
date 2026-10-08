import { html, setHtml, on, formData, icons, $ } from '../core/dom.js';
import { api, paged } from '../core/api.js';
import { auth } from '../core/auth.js';
import { toast } from '../core/toast.js';
import { fmtDateTime, label } from '../core/format.js';
import { pageHeader, emptyState, skeletonRows, pagination, badge, options, openModal, confirmDialog } from '../components/ui.js';

const ROLES = ['ADMIN', 'GIANG_VIEN', 'SINH_VIEN'];
const roleOptions = (selected) => options(ROLES.map((r) => ({ id: r, name: label('role', r) })), selected);

export async function render(root) {
  const state = { page: 1, search: '', data: null };

  setHtml(root, html`
    ${pageHeader({
      title: 'Quản trị tài khoản',
      subtitle: 'Tạo tài khoản, khóa / mở khóa, đặt lại mật khẩu và phân quyền',
      actions: html`<button class="btn btn-primary btn-sm" data-action="create">${icons.plus} Tạo tài khoản</button>`,
    })}
    <div class="card p-4 filter-bar">
      <div class="filter-grid">
        <div class="search-wrap">${icons.search}<input type="text" class="search-input" id="search" placeholder="Tìm theo tên đăng nhập, họ tên, email..." /></div>
        <button class="btn btn-sm" data-action="search">Tìm kiếm</button>
      </div>
    </div>
    <div id="list"></div>`);

  async function load() {
    setHtml($('#list', root), skeletonRows(5));
    state.data = paged(await api.get('/auth/users', { pageNumber: state.page, pageSize: 20, search: state.search, sortDirection: 'desc' }));
    const d = state.data;
    setHtml($('#list', root), d.items.length === 0 ? html`<div class="card p-6">${emptyState({ title: 'Không tìm thấy tài khoản' })}</div>` : html`
      <div class="table-container">
        <table>
          <thead><tr><th>Tên đăng nhập</th><th>Họ tên</th><th>Email</th><th>Vai trò</th><th>Đăng nhập gần nhất</th><th>Trạng thái</th><th></th></tr></thead>
          <tbody>${d.items.map((u) => {
            const self = u.id === auth.user?.id;
            return html`
              <tr>
                <td class="text-mono">${u.username}</td>
                <td><strong>${u.fullName}</strong>${self ? html` <span class="text-xs text-muted">(bạn)</span>` : ''}</td>
                <td class="text-secondary">${u.email}</td>
                <td>${badge(label('role', u.role), u.role === 'ADMIN' ? 'accent' : u.role === 'GIANG_VIEN' ? 'info' : 'default')}</td>
                <td>${fmtDateTime(u.lastLogin)}</td>
                <td>${u.isLocked ? badge('Đã khóa', 'danger') : badge('Hoạt động', 'success')}</td>
                <td class="actions">
                  <button class="btn btn-ghost btn-xs" data-reset="${u.id}">Đặt lại mật khẩu</button>
                  ${self ? '' : html`
                    <button class="btn btn-ghost btn-xs" data-role="${u.id}">Đổi vai trò</button>
                    ${u.isLocked ? html`<button class="btn btn-xs" data-unlock="${u.id}">Mở khóa</button>` : html`<button class="btn btn-danger btn-xs" data-lock="${u.id}">Khóa</button>`}`}
                </td>
              </tr>`;
          })}</tbody>
        </table>
        ${pagination(d)}
      </div>`);
  }

  const find = (id) => state.data.items.find((u) => u.id === Number(id));
  const applySearch = () => { state.search = $('#search', root).value.trim(); state.page = 1; load(); };

  on(root, 'click', '[data-page]', (e, el) => { state.page = Number(el.dataset.page); load(); });
  on(root, 'click', '[data-action]', (e, el) => {
    if (el.dataset.action === 'search') return applySearch();
    openModal({
      title: 'Tạo tài khoản', confirmText: 'Tạo tài khoản',
      body: html`<form>
        <p class="form-hint mb-3">Tài khoản tạo ở đây chưa gắn hồ sơ giảng viên / sinh viên. Để tạo kèm hồ sơ, dùng trang Giảng viên hoặc Sinh viên.</p>
        <div class="grid-2">
          <div class="form-group"><label class="required">Tên đăng nhập</label><input name="username" required minlength="3" maxlength="50" /></div>
          <div class="form-group"><label class="required">Vai trò</label><select name="role">${roleOptions('ADMIN')}</select></div>
        </div>
        <div class="form-group"><label class="required">Họ tên</label><input name="fullName" required maxlength="100" /></div>
        <div class="form-group"><label class="required">Email</label><input name="email" type="email" required maxlength="100" /></div>
        <div class="form-group"><label class="required">Mật khẩu</label><input name="password" type="password" required minlength="6" /></div>
      </form>`,
      async onConfirm(form) { await api.post('/auth/users', formData(form)); toast.success('Tạo tài khoản thành công.'); await load(); },
    });
  });
  on(root, 'click', '[data-reset]', (e, el) => {
    const u = find(el.dataset.reset);
    openModal({
      title: `Đặt lại mật khẩu · ${u.username}`, confirmText: 'Đặt lại',
      body: html`<form><div class="form-group"><label class="required">Mật khẩu mới</label><input name="newPassword" type="password" required minlength="6" /></div>
        <p class="form-hint">Người dùng sẽ phải đăng nhập lại ở mọi thiết bị.</p></form>`,
      async onConfirm(form) { await api.put(`/auth/users/${u.id}/reset-password`, formData(form)); toast.success('Đặt lại mật khẩu thành công.'); },
    });
  });
  on(root, 'click', '[data-role]', (e, el) => {
    const u = find(el.dataset.role);
    openModal({
      title: `Đổi vai trò · ${u.username}`, confirmText: 'Cập nhật',
      body: html`<form><div class="form-group"><label class="required">Vai trò mới</label><select name="role">${roleOptions(u.role)}</select></div></form>`,
      async onConfirm(form) { await api.put(`/auth/users/${u.id}/role`, formData(form)); toast.success('Cập nhật vai trò thành công.'); await load(); },
    });
  });
  on(root, 'click', '[data-lock]', (e, el) => {
    const u = find(el.dataset.lock);
    confirmDialog({
      title: 'Khóa tài khoản', message: `Khóa tài khoản ${u.username}? Phiên đăng nhập hiện tại sẽ mất hiệu lực ngay.`, confirmText: 'Khóa', danger: true,
      async onConfirm() { await api.put(`/auth/users/${u.id}/lock`); toast.success('Đã khóa tài khoản.'); await load(); },
    });
  });
  on(root, 'click', '[data-unlock]', async (e, el) => {
    await api.put(`/auth/users/${el.dataset.unlock}/unlock`);
    toast.success('Đã mở khóa tài khoản.');
    await load();
  });
  $('#search', root).addEventListener('keyup', (e) => { if (e.key === 'Enter') applySearch(); });

  await load();
}
