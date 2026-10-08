import { html, setHtml, on, formData, icons, $ } from '../core/dom.js';
import { api, paged, unwrap } from '../core/api.js';
import { auth } from '../core/auth.js';
import { toast } from '../core/toast.js';
import { lookup } from '../core/lookup.js';
import { fmtDate, toDateInput } from '../core/format.js';
import { pageHeader, pagination, emptyState, skeletonRows, options, openModal } from '../components/ui.js';

export async function render(root, { query }) {
  const state = { page: 1, search: '', classId: query.classId || '', data: null };
  const classes = await lookup.classes();
  const sortedClasses = [...classes].sort((a, b) => b.admissionYear - a.admissionYear || a.code.localeCompare(b.code));

  setHtml(root, html`
    ${pageHeader({
      title: 'Sinh viên',
      subtitle: 'Tra cứu sinh viên theo lớp, mã số sinh viên hoặc họ tên',
      actions: auth.isAdmin ? html`<button class="btn btn-primary btn-sm" data-action="create">${icons.plus} Thêm sinh viên</button>` : '',
    })}
    <div class="card p-4 filter-bar">
      <div class="filter-grid">
        <div class="search-wrap">${icons.search}<input type="text" class="search-input" id="search" placeholder="Tìm theo MSSV, họ tên, email..." /></div>
        <select class="filter-select" id="class" style="width:260px">${options(sortedClasses, state.classId, { placeholder: 'Tất cả lớp', text: (c) => `${c.code} - ${c.name}` })}</select>
        <button class="btn btn-sm" data-action="search">Tìm kiếm</button>
      </div>
    </div>
    <div id="list"></div>`);

  async function load() {
    setHtml($('#list', root), skeletonRows(5));
    state.data = paged(await api.get('/students', { pageNumber: state.page, pageSize: 20, search: state.search, classId: state.classId }));
    const d = state.data;
    if (!d.items.length) {
      setHtml($('#list', root), html`<div class="card p-6">${emptyState({ title: 'Không tìm thấy sinh viên', description: 'Thử đổi từ khóa hoặc chọn lớp khác.' })}</div>`);
      return;
    }
    setHtml($('#list', root), html`
      <div class="table-container">
        <table>
          <thead><tr><th>MSSV</th><th>Họ tên</th><th>Lớp</th><th>Ngày sinh</th><th>Email</th>${auth.isAdmin ? html`<th></th>` : ''}</tr></thead>
          <tbody>
            ${d.items.map((x) => html`
              <tr>
                <td class="text-mono">${x.code}</td>
                <td><strong>${x.fullName}</strong></td>
                <td>${x.className}</td>
                <td>${fmtDate(x.dateOfBirth)}</td>
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
          <p class="form-hint mb-3">Tài khoản đăng nhập mặc định: tên đăng nhập và mật khẩu đều là MSSV, email MSSV@st.utc2.edu.vn.</p>
          <div class="form-group"><label class="required">Mã số sinh viên</label><input name="code" required maxlength="20" placeholder="Nhập mã số sinh viên" /></div>`}
        <div class="form-group"><label class="required">Họ tên</label><input name="fullName" value="${item?.fullName ?? ''}" required maxlength="100" /></div>
        <div class="grid-2">
          <div class="form-group"><label>Ngày sinh</label><input name="dateOfBirth" type="date" value="${toDateInput(item?.dateOfBirth)}" /></div>
          <div class="form-group"><label>Giới tính</label>
            <select name="gender">${options([{ id: 'Nam', name: 'Nam' }, { id: 'Nữ', name: 'Nữ' }], item?.gender, { placeholder: '-- Chọn --' })}</select>
          </div>
        </div>
        <div class="grid-2">
          ${item ? html`<div class="form-group"><label class="required">Email</label><input name="email" type="email" value="${item.email}" required /></div>` : ''}
          <div class="form-group"><label>Điện thoại</label><input name="phoneNumber" value="${item?.phoneNumber ?? ''}" maxlength="20" /></div>
        </div>
        <div class="form-group"><label class="required">Lớp</label><select name="classId" data-type="int" required>${options(sortedClasses, item?.classId ?? state.classId, { placeholder: '-- Chọn lớp --', text: (c) => `${c.code} - ${c.name}` })}</select></div>
      </form>`;
  }

  function openForm(item) {
    openModal({
      title: item ? 'Cập nhật sinh viên' : 'Thêm sinh viên',
      confirmText: item ? 'Lưu thay đổi' : 'Tạo sinh viên',
      body: formBody(item),
      async onConfirm(form) {
        const d = formData(form);
        if (item) {
          await api.put(`/students/${item.id}`, d);
          toast.success('Cập nhật sinh viên thành công.');
        } else {
          const email = `${d.code.toLowerCase()}@st.utc2.edu.vn`;
          const account = unwrap(await api.post('/auth/users', {
            username: d.code, password: d.code, email, fullName: d.fullName, role: 'SINH_VIEN',
          }));
          await api.post('/students', { ...d, email, accountId: account.id });
          toast.success('Thêm sinh viên thành công.');
        }
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
  $('#class', root).addEventListener('change', (e) => { state.classId = e.target.value; state.page = 1; load(); });

  await load();
}
