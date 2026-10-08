import { html, setHtml, on, icons, $ } from '../core/dom.js';
import { api, paged } from '../core/api.js';
import { auth } from '../core/auth.js';
import { lookup, lecturerName } from '../core/lookup.js';
import { fmtDate, statusLabel } from '../core/format.js';
import { pageHeader, statusBadge, emptyState, skeletonRows, pagination, options } from '../components/ui.js';
import { navigate } from '../core/router.js';

const STATUSES = ['CHO_THUC_HIEN', 'DANG_THUC_HIEN', 'CHO_BAO_VE', 'DA_BAO_VE', 'HOAN_THANH', 'TAM_DUNG', 'DA_HUY'];

export async function render(root) {
  const [periods, lecturers, me] = await Promise.all([
    lookup.periods(), lookup.lecturerMap(), auth.isLecturer ? lookup.myLecturer().catch(() => null) : null,
  ]);
  const state = { page: 1, search: '', periodId: '', status: '', mine: auth.isLecturer, data: null };

  setHtml(root, html`
    ${pageHeader({
      title: auth.isStudent ? 'Đồ án của tôi' : auth.isLecturer ? 'Đồ án hướng dẫn' : 'Đồ án thực hiện',
      subtitle: auth.isStudent
        ? 'Theo dõi đồ án, nộp báo cáo tiến độ và tài liệu'
        : 'Danh sách đồ án theo đợt: thành viên, giảng viên hướng dẫn, tiến độ và trạng thái',
      actions: auth.isAdmin ? html`<a class="btn btn-primary btn-sm" href="#/registrations/management">${icons.plus} Tạo đồ án từ đơn đã duyệt</a>` : '',
    })}
    ${auth.isStudent ? '' : html`
      <div class="card p-4 filter-bar">
        <div class="filter-grid">
          <div class="search-wrap">${icons.search}<input type="text" class="search-input" id="search" placeholder="Tìm theo mã hoặc tên đồ án..." /></div>
          <select class="filter-select" id="period" style="width:260px">${options(periods, '', { placeholder: 'Tất cả đợt' })}</select>
          <select class="filter-select" id="status">${options(STATUSES.map((s) => ({ id: s, name: statusLabel(s) })), '', { placeholder: 'Tất cả trạng thái' })}</select>
          ${auth.isLecturer ? html`<label class="flex items-center gap-2 text-sm"><input type="checkbox" id="mine" checked style="width:auto;height:auto" /> Chỉ đồ án tôi hướng dẫn</label>` : ''}
        </div>
      </div>`}
    <div id="list"></div>`);

  async function load() {
    setHtml($('#list', root), skeletonRows(4));
    state.data = paged(await api.get('/projects', {
      pageNumber: state.page, pageSize: 20, search: state.search, periodId: state.periodId, status: state.status,
      lecturerId: state.mine && me ? me.id : '',
    }));
    const d = state.data;
    if (!d.items.length) {
      setHtml($('#list', root), html`<div class="card p-6">${emptyState({
        title: 'Chưa có đồ án nào',
        description: auth.isStudent ? 'Đồ án được tạo sau khi đơn đăng ký nhóm của bạn được duyệt.' : 'Không có đồ án phù hợp với bộ lọc.',
      })}</div>`);
      return;
    }
    setHtml($('#list', root), html`
      <div class="table-container">
        <table>
          <thead><tr><th>Mã</th><th>Tên đồ án</th><th>Đợt</th><th>Thành viên</th><th>GV hướng dẫn</th><th>Ngày tạo</th><th>Trạng thái</th></tr></thead>
          <tbody>${d.items.map((p) => {
            const advisor = p.advisors.find((a) => a.isActive && a.advisorType === 'CHINH') ?? p.advisors.find((a) => a.isActive);
            return html`
              <tr style="cursor:pointer" data-open="${p.id}">
                <td class="text-mono">${p.code}</td>
                <td><strong>${p.title}</strong></td>
                <td class="text-secondary">${periods.find((x) => x.id === p.periodId)?.name ?? ''}</td>
                <td>${p.members.length}</td>
                <td>${advisor ? lecturerName(lecturers, advisor.lecturerId) : html`<span class="text-warning">Chưa phân công</span>`}</td>
                <td>${fmtDate(p.createdDate)}</td>
                <td>${statusBadge(p.status)}</td>
              </tr>`;
          })}</tbody>
        </table>
        ${pagination(d)}
      </div>`);
  }

  on(root, 'click', '[data-open]', (e, el) => navigate(`/projects/${el.dataset.open}`));
  on(root, 'click', '[data-page]', (e, el) => { state.page = Number(el.dataset.page); load(); });
  $('#search', root)?.addEventListener('keyup', (e) => { if (e.key === 'Enter') { state.search = e.target.value.trim(); state.page = 1; load(); } });
  $('#period', root)?.addEventListener('change', (e) => { state.periodId = e.target.value; state.page = 1; load(); });
  $('#status', root)?.addEventListener('change', (e) => { state.status = e.target.value; state.page = 1; load(); });
  $('#mine', root)?.addEventListener('change', (e) => { state.mine = e.target.checked; state.page = 1; load(); });

  await load();
}
