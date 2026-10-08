import { html, raw, setHtml, on, formData, icons, $ } from '../core/dom.js';
import { api, paged, unwrap } from '../core/api.js';
import { auth } from '../core/auth.js';
import { toast } from '../core/toast.js';
import { lookup, clearLookups } from '../core/lookup.js';
import { pageHeader, pagination, emptyState, skeletonRows, badge, options, openModal } from '../components/ui.js';

export async function render(root) {
  const state = { page: 1, search: '', departmentId: '', data: null };
  const [departments, degrees, ranks] = await Promise.all([lookup.departments(), lookup.degrees(), lookup.ranks()]);

  setHtml(root, html`
    ${pageHeader({
      title: 'Giảng viên',
      subtitle: 'Danh sách giảng viên cơ hữu và thỉnh giảng tham gia hướng dẫn, phản biện, hội đồng',
      actions: auth.isAdmin ? html`<button class="btn btn-primary btn-sm" data-action="create">${icons.plus} Thêm giảng viên</button>` : '',
    })}
    <div class="card p-4 filter-bar">
      <div class="filter-grid">
        <div class="search-wrap">${icons.search}<input type="text" class="search-input" id="search" placeholder="Tìm theo mã, họ tên, email..." /></div>
        <select class="filter-select" id="department">${options(departments, '', { placeholder: 'Tất cả bộ môn' })}</select>
        <button class="btn btn-sm" data-action="search">Tìm kiếm</button>
      </div>
    </div>
    <div id="list"></div>`);

  async function load() {
    setHtml($('#list', root), skeletonRows(4));
    state.data = paged(await api.get('/lecturers', { pageNumber: state.page, pageSize: 20, search: state.search, departmentId: state.departmentId }));
    const d = state.data;
    if (!d.items.length) {
      setHtml($('#list', root), html`<div class="card p-6">${emptyState({ title: 'Không tìm thấy giảng viên', description: 'Thử đổi từ khóa hoặc bộ môn.' })}</div>`);
      return;
    }
    setHtml($('#list', root), html`
      <div class="table-container">
        <table>
          <thead><tr><th>Mã GV</th><th>Họ tên</th><th>Học vị / Học hàm</th><th>Bộ môn</th><th>Email</th>${auth.isAdmin ? html`<th></th>` : ''}</tr></thead>
          <tbody>
            ${d.items.map((x) => html`
              <tr>
                <td class="text-mono">${x.code}</td>
                <td><strong>${x.fullName}</strong></td>
                <td>${x.academicRankName ? badge(x.academicRankName, 'accent') : ''} ${badge(x.degreeName, 'default')}</td>
                <td>${x.departmentName}</td>
                <td class="text-secondary">${x.email}</td>
                ${auth.isAdmin ? html`<td class="actions"><button class="btn btn-ghost btn-xs" data-action="edit" data-id="${x.id}">Sửa</button></td>` : ''}
              </tr>`)}
          </tbody>
        </table>
        ${pagination(d)}
      </div>`);
  }

  function formBody(item) {
    return html`
      <form>
        ${item ? '' : html`
          <p class="form-hint mb-3">Hệ thống tạo tài khoản đăng nhập vai trò Giảng viên cùng lúc với hồ sơ.</p>
          <div class="grid-2">
            <div class="form-group"><label class="required">Mã giảng viên</label><input name="code" required maxlength="20" placeholder="VD: GV010" /></div>
            <div class="form-group"><label class="required">Tên đăng nhập</label><input name="username" required minlength="3" maxlength="50" placeholder="VD: nvan" /></div>
          </div>
          <div class="form-group"><label class="required">Mật khẩu ban đầu</label><input name="password" type="password" required minlength="6" value="Lecturer@123456" /></div>`}
        <div class="form-group"><label class="required">Họ tên</label><input name="fullName" value="${item?.fullName ?? ''}" required maxlength="100" /></div>
        <div class="grid-2">
          <div class="form-group"><label class="required">Email</label><input name="email" type="email" value="${item?.email ?? ''}" required maxlength="100" /></div>
          <div class="form-group"><label>Điện thoại</label><input name="phoneNumber" value="${item?.phoneNumber ?? ''}" maxlength="20" /></div>
        </div>
        <div class="form-group"><label class="required">Bộ môn</label><select name="departmentId" data-type="int" required>${options(departments, item?.departmentId, { placeholder: '-- Chọn bộ môn --' })}</select></div>
        <div class="grid-2">
          <div class="form-group"><label class="required">Học vị</label><select name="degreeId" data-type="int" required>${options(degrees, item?.degreeId, { placeholder: '-- Chọn --' })}</select></div>
          <div class="form-group"><label>Học hàm</label><select name="academicRankId" data-type="int">${options(ranks, item?.academicRankId, { placeholder: 'Không' })}</select></div>
        </div>
      </form>`;
  }

  function openForm(item) {
    openModal({
      title: item ? 'Cập nhật giảng viên' : 'Thêm giảng viên',
      confirmText: item ? 'Lưu thay đổi' : 'Tạo giảng viên',
      body: formBody(item),
      async onConfirm(form) {
        const d = formData(form);
        if (item) {
          await api.put(`/lecturers/${item.id}`, d);
          toast.success('Cập nhật giảng viên thành công.');
        } else {
          const account = unwrap(await api.post('/auth/users', {
            username: d.username, password: d.password, email: d.email, fullName: d.fullName, role: 'GIANG_VIEN',
          }));
          await api.post('/lecturers', { ...d, accountId: account.id });
          toast.success('Thêm giảng viên thành công.');
        }
        clearLookups();
        await load();
      },
    });
  }

  on(root, 'click', '[data-page]', (e, el) => { state.page = Number(el.dataset.page); load(); });
  on(root, 'click', '[data-action]', (e, el) => {
    const a = el.dataset.action;
    if (a === 'create') openForm(null);
    else if (a === 'edit') openForm(state.data.items.find((x) => x.id === Number(el.dataset.id)));
    else if (a === 'search') { state.search = $('#search', root).value.trim(); state.page = 1; load(); }
  });
  $('#search', root).addEventListener('keyup', (e) => { if (e.key === 'Enter') { state.search = e.target.value.trim(); state.page = 1; load(); } });
  $('#department', root).addEventListener('change', (e) => { state.departmentId = e.target.value; state.page = 1; load(); });

  await load();
}
