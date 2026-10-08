import { html, raw, setHtml, on, formData, icons, $ } from '../core/dom.js';
import { api, paged } from '../core/api.js';
import { toast } from '../core/toast.js';
import { lookup, clearLookups } from '../core/lookup.js';
import { pageHeader, tabs, pagination, emptyState, skeletonRows, badge, options, openModal, confirmDialog } from '../components/ui.js';

// Cấu hình cho từng tab: endpoint, cột, form
const TABS = {
  faculties: {
    label: 'Khoa',
    endpoint: '/faculties',
    columns: ['Mã', 'Tên khoa', 'Email', 'Điện thoại', 'Trạng thái'],
    row: (x) => [html`<span class="text-mono">${x.code}</span>`, x.name, x.email || '—', x.phoneNumber || '—', x.isActive ? badge('Hoạt động', 'success') : badge('Ngừng', 'default')],
    deletable: true,
    async form(item) {
      return html`
        <div class="grid-2">
          <div class="form-group"><label class="required">Mã khoa</label><input name="code" value="${item?.code ?? ''}" ${raw(item ? 'disabled' : 'required')} maxlength="20" /></div>
          <div class="form-group"><label>Điện thoại</label><input name="phoneNumber" value="${item?.phoneNumber ?? ''}" maxlength="20" /></div>
        </div>
        <div class="form-group"><label class="required">Tên khoa</label><input name="name" value="${item?.name ?? ''}" required maxlength="100" /></div>
        <div class="form-group"><label>Email</label><input name="email" type="email" value="${item?.email ?? ''}" maxlength="100" /></div>`;
    },
  },
  departments: {
    label: 'Bộ môn',
    endpoint: '/departments',
    filter: { name: 'facultyId', placeholder: 'Tất cả khoa', load: () => lookup.faculties() },
    columns: ['Mã', 'Tên bộ môn', 'Thuộc khoa', 'Trạng thái'],
    row: (x) => [html`<span class="text-mono">${x.code}</span>`, x.name, x.facultyName, x.isActive ? badge('Hoạt động', 'success') : badge('Ngừng', 'default')],
    deletable: true,
    async form(item) {
      const faculties = await lookup.faculties();
      return html`
        <div class="form-group"><label class="required">Mã bộ môn</label><input name="code" value="${item?.code ?? ''}" ${raw(item ? 'disabled' : 'required')} maxlength="20" /></div>
        <div class="form-group"><label class="required">Tên bộ môn</label><input name="name" value="${item?.name ?? ''}" required maxlength="100" /></div>
        <div class="form-group"><label class="required">Thuộc khoa</label><select name="facultyId" data-type="int" required>${options(faculties, item?.facultyId, { placeholder: '-- Chọn khoa --' })}</select></div>`;
    },
  },
  majors: {
    label: 'Ngành / Chuyên ngành',
    endpoint: '/majors',
    filter: { name: 'departmentId', placeholder: 'Tất cả bộ môn', load: () => lookup.departments() },
    columns: ['Mã', 'Tên ngành / chuyên ngành', 'Bộ môn', 'Trạng thái'],
    row: (x) => [html`<span class="text-mono">${x.code}</span>`, x.name, x.departmentName, x.isActive ? badge('Hoạt động', 'success') : badge('Ngừng', 'default')],
    async form(item) {
      const departments = await lookup.departments();
      return html`
        <div class="form-group"><label class="required">Mã</label><input name="code" value="${item?.code ?? ''}" ${raw(item ? 'disabled' : 'required')} maxlength="20" placeholder="VD: 7480201 hoặc 7480201-CNPM" /></div>
        <div class="form-group"><label class="required">Tên</label><input name="name" value="${item?.name ?? ''}" required maxlength="100" /></div>
        <div class="form-group"><label class="required">Bộ môn quản lý</label><select name="departmentId" data-type="int" required>${options(departments, item?.departmentId, { placeholder: '-- Chọn bộ môn --' })}</select></div>`;
    },
  },
  classes: {
    label: 'Lớp',
    endpoint: '/classes',
    filter: { name: 'majorId', placeholder: 'Tất cả ngành', load: () => lookup.majors() },
    columns: ['Mã lớp', 'Tên lớp', 'Năm nhập học', 'Ngành', ''],
    row: (x) => [html`<span class="text-mono">${x.code}</span>`, x.name, x.admissionYear, x.majorName, html`<a class="btn btn-ghost btn-xs" href="#/academic/students?classId=${x.id}">Xem sinh viên</a>`],
    async form(item) {
      const majors = await lookup.majors();
      return html`
        <div class="grid-2">
          <div class="form-group"><label class="required">Mã lớp</label><input name="code" value="${item?.code ?? ''}" ${raw(item ? 'disabled' : 'required')} maxlength="20" placeholder="VD: CQ.66.CNTT" /></div>
          <div class="form-group"><label class="required">Năm nhập học</label><input name="admissionYear" type="number" min="2000" max="2100" value="${item?.admissionYear ?? new Date().getFullYear()}" required /></div>
        </div>
        <div class="form-group"><label class="required">Tên lớp</label><input name="name" value="${item?.name ?? ''}" required maxlength="100" /></div>
        <div class="form-group"><label class="required">Ngành</label><select name="majorId" data-type="int" required>${options(majors, item?.majorId, { placeholder: '-- Chọn ngành --' })}</select></div>`;
    },
  },
};

export async function render(root, { query }) {
  const state = { tab: TABS[query.tab] ? query.tab : 'faculties', page: 1, search: '', filter: '', data: null, filterItems: [] };

  setHtml(root, html`
    ${pageHeader({
      title: 'Khoa, Bộ môn & Lớp',
      subtitle: 'Danh mục cơ cấu đào tạo: khoa, bộ môn, ngành / chuyên ngành và lớp sinh viên',
      actions: html`<button class="btn btn-primary btn-sm" data-action="create">${icons.plus} Thêm mới</button>`,
    })}
    <div id="tabs"></div>
    <div class="card p-4 filter-bar">
      <div class="filter-grid">
        <div class="search-wrap">${icons.search}<input type="text" class="search-input" id="search" placeholder="Tìm theo mã hoặc tên..." /></div>
        <select class="filter-select hidden" id="filter"></select>
        <button class="btn btn-sm" data-action="search">Tìm kiếm</button>
      </div>
    </div>
    <div id="list"></div>`);

  const cfg = () => TABS[state.tab];

  async function drawTabs() {
    setHtml($('#tabs', root), tabs(Object.entries(TABS).map(([id, t]) => ({ id, label: t.label })), state.tab));
    const f = cfg().filter;
    const select = $('#filter', root);
    select.classList.toggle('hidden', !f);
    if (f) {
      state.filterItems = await f.load();
      setHtml(select, options(state.filterItems, state.filter, { placeholder: f.placeholder }));
    }
  }

  async function load() {
    setHtml($('#list', root), skeletonRows(4));
    const c = cfg();
    const q = { pageNumber: state.page, pageSize: 20, search: state.search };
    if (c.filter && state.filter) q[c.filter.name] = state.filter;
    state.data = paged(await api.get(c.endpoint, q));
    draw();
  }

  function draw() {
    const c = cfg();
    const d = state.data;
    if (!d.items.length) {
      setHtml($('#list', root), html`<div class="card p-6">${emptyState({ title: 'Không có dữ liệu', description: 'Chưa có mục nào phù hợp với bộ lọc hiện tại.' })}</div>`);
      return;
    }
    setHtml($('#list', root), html`
      <div class="table-container">
        <table>
          <thead><tr>${c.columns.map((h) => html`<th>${h}</th>`)}<th></th></tr></thead>
          <tbody>
            ${d.items.map((x) => html`
              <tr>
                ${c.row(x).map((v) => html`<td>${v}</td>`)}
                <td class="actions">
                  <button class="btn btn-ghost btn-xs" data-action="edit" data-id="${x.id}">Sửa</button>
                  ${c.deletable ? html`<button class="btn btn-danger btn-xs" data-action="delete" data-id="${x.id}">Xóa</button>` : ''}
                </td>
              </tr>`)}
          </tbody>
        </table>
        ${pagination(d)}
      </div>`);
  }

  async function openForm(item) {
    const c = cfg();
    openModal({
      title: `${item ? 'Cập nhật' : 'Thêm'} ${c.label.toLowerCase()}`,
      confirmText: item ? 'Lưu thay đổi' : 'Tạo mới',
      body: html`<form>${await c.form(item)}</form>`,
      async onConfirm(form) {
        const data = formData(form);
        const res = item ? await api.put(`${c.endpoint}/${item.id}`, data) : await api.post(c.endpoint, data);
        toast.success(res?.message || 'Lưu thành công.');
        clearLookups();
        await drawTabs();
        await load();
      },
    });
  }

  on(root, 'click', '[data-tab]', async (e, el) => {
    state.tab = el.dataset.tab;
    state.page = 1;
    state.filter = '';
    state.search = '';
    $('#search', root).value = '';
    await drawTabs();
    await load();
  });
  on(root, 'click', '[data-page]', (e, el) => { state.page = Number(el.dataset.page); load(); });
  on(root, 'click', '[data-action]', (e, el) => {
    const action = el.dataset.action;
    const item = state.data?.items.find((x) => x.id === Number(el.dataset.id));
    if (action === 'create') openForm(null);
    else if (action === 'edit') openForm(item);
    else if (action === 'search') { state.search = $('#search', root).value.trim(); state.page = 1; load(); }
    else if (action === 'delete') {
      confirmDialog({
        title: 'Xóa mục này?',
        message: `Bạn chắc chắn muốn xóa "${item.name}"? Không thể xóa nếu đang có dữ liệu tham chiếu.`,
        confirmText: 'Xóa', danger: true,
        async onConfirm() {
          await api.delete(`${cfg().endpoint}/${item.id}`);
          toast.success('Đã xóa.');
          clearLookups();
          await load();
        },
      });
    }
  });
  $('#search', root).addEventListener('keyup', (e) => { if (e.key === 'Enter') { state.search = e.target.value.trim(); state.page = 1; load(); } });
  $('#filter', root).addEventListener('change', (e) => { state.filter = e.target.value; state.page = 1; load(); });

  await drawTabs();
  await load();
}
