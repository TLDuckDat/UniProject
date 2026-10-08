import { html, setHtml, on, formData, icons, $ } from '../core/dom.js';
import { api, paged } from '../core/api.js';
import { auth } from '../core/auth.js';
import { toast } from '../core/toast.js';
import { lookup, clearLookups } from '../core/lookup.js';
import { fmtDate, label } from '../core/format.js';
import { pageHeader, pagination, emptyState, skeletonRows, statusBadge, segmented, options, openModal } from '../components/ui.js';
import { navigate } from '../core/router.js';

const FILTERS = [
  { key: '', label: 'Tất cả' },
  { key: 'CHUA_MO', label: 'Chưa mở' },
  { key: 'DANG_MO_DANG_KY', label: 'Đang đăng ký' },
  { key: 'DANG_THUC_HIEN', label: 'Đang thực hiện' },
  { key: 'DANG_BAO_VE', label: 'Đang bảo vệ' },
  { key: 'DA_KET_THUC', label: 'Đã kết thúc' },
];

export const NEXT_STATUS = {
  CHUA_MO: { status: 'DANG_MO_DANG_KY', text: 'Mở đăng ký', cls: 'btn-primary' },
  DANG_MO_DANG_KY: { status: 'DANG_THUC_HIEN', text: 'Bắt đầu thực hiện', cls: 'btn-accent' },
  DANG_THUC_HIEN: { status: 'DANG_BAO_VE', text: 'Chuyển sang bảo vệ', cls: 'btn-primary' },
  DANG_BAO_VE: { status: 'DA_KET_THUC', text: 'Đóng đợt', cls: '' },
};

export async function periodForm(item) {
  const [semesters, types] = await Promise.all([lookup.semesters(), lookup.projectTypes()]);
  const d = (v) => (v ? String(v).slice(0, 10) : '');
  return html`
    <form>
      <div class="grid-2">
        <div class="form-group"><label class="required">Mã đợt</label><input name="code" value="${item?.code ?? ''}" ${item ? 'disabled' : 'required'} maxlength="20" placeholder="VD: DACN-HK1-2627" /></div>
        <div class="form-group"><label class="required">Loại đồ án</label>
          <select name="projectKind" ${item ? 'disabled' : 'required'}>${options(types, item?.projectKind, { value: 'kind', text: 'name', placeholder: '-- Chọn loại đồ án --' })}</select>
        </div>
      </div>
      <div class="form-group"><label class="required">Tên đợt đồ án</label><input name="name" value="${item?.name ?? ''}" required maxlength="200" /></div>
      <div class="form-group"><label class="required">Học kỳ</label>
        <select name="semesterId" data-type="int" required>${options(semesters, item?.semesterId, { text: (s) => `${s.name} - ${s.academicYearName}`, placeholder: '-- Chọn học kỳ --' })}</select>
      </div>
      <div class="grid-2">
        <div class="form-group"><label class="required">Bắt đầu</label><input type="date" name="startDate" value="${d(item?.startDate)}" required /></div>
        <div class="form-group"><label class="required">Kết thúc</label><input type="date" name="endDate" value="${d(item?.endDate)}" required /></div>
        <div class="form-group"><label class="required">Hạn đăng ký</label><input type="date" name="registrationDeadline" value="${d(item?.registrationDeadline)}" required /></div>
        <div class="form-group"><label class="required">Hạn nộp đề cương</label><input type="date" name="outlineDeadline" value="${d(item?.outlineDeadline)}" required /></div>
        <div class="form-group"><label class="required">Hạn nộp báo cáo</label><input type="date" name="reportDeadline" value="${d(item?.reportDeadline)}" required /></div>
        <div class="form-group"></div>
        <div class="form-group"><label class="required">Bảo vệ từ</label><input type="date" name="defenseStartDate" value="${d(item?.defenseStartDate)}" required /></div>
        <div class="form-group"><label class="required">Bảo vệ đến</label><input type="date" name="defenseEndDate" value="${d(item?.defenseEndDate)}" required /></div>
      </div>
    </form>`;
}

export async function changePeriodStatus(id, status) {
  const res = await api.patch(`/project-periods/${id}/status`, { status });
  toast.success(res?.message || 'Cập nhật trạng thái thành công.');
  clearLookups();
}

export async function render(root) {
  const state = { page: 1, status: '', search: '', data: null };
  const types = await lookup.projectTypes().catch(() => []);

  setHtml(root, html`
    ${pageHeader({
      title: 'Đợt Đồ án',
      subtitle: 'Quản lý vòng đời các đợt Đồ án cơ sở, Đồ án chuyên ngành, Đồ án môn học và Đồ án tốt nghiệp',
      actions: auth.isAdmin ? html`<button class="btn btn-primary btn-sm" data-action="create">${icons.plus} Tạo đợt đồ án mới</button>` : '',
    })}
    <div class="card p-4 filter-bar">
      <div class="filter-row">
        <div id="filters"></div>
        <div class="search-box"><input type="text" id="search" placeholder="Tìm theo tên đợt, mã đợt..." /></div>
      </div>
    </div>
    <div id="list"></div>`);

  const drawFilters = () => setHtml($('#filters', root), segmented(FILTERS, state.status));

  async function load() {
    setHtml($('#list', root), skeletonRows(3));
    state.data = paged(await api.get('/project-periods', { pageNumber: state.page, pageSize: 12, status: state.status, search: state.search }));
    const d = state.data;
    if (!d.items.length) {
      setHtml($('#list', root), html`<div class="card p-6">${emptyState({
        title: 'Không tìm thấy đợt đồ án nào',
        description: 'Hiện tại chưa có đợt đồ án nào phù hợp với bộ lọc trạng thái được chọn.',
        actionText: auth.isAdmin ? 'Tạo đợt đầu tiên' : '', action: 'create',
      })}</div>`);
      return;
    }
    setHtml($('#list', root), html`
      <div class="periods-grid">
        ${d.items.map((p) => {
          const type = types.find((t) => t.kind === p.projectKind);
          const next = NEXT_STATUS[p.status];
          return html`
            <div class="card period-card">
              <div class="period-card-header">
                <div class="period-title-wrap">
                  <div class="period-code">${p.code}</div>
                  <h3 class="period-name" data-open="${p.id}">${p.name}</h3>
                </div>
                ${statusBadge(p.status)}
              </div>
              <div class="period-timeline-preview">
                <div class="timeline-metric"><span class="metric-label">Thời gian thực hiện:</span><span class="metric-val">${fmtDate(p.startDate)} — ${fmtDate(p.endDate)}</span></div>
                <div class="timeline-metric"><span class="metric-label">Hạn đăng ký nhóm:</span><span class="metric-val">${fmtDate(p.registrationDeadline)}</span></div>
                <div class="timeline-metric"><span class="metric-label">Hạn nộp đề cương / báo cáo:</span><span class="metric-val text-warning">${fmtDate(p.outlineDeadline)} / ${fmtDate(p.reportDeadline)}</span></div>
                <div class="timeline-metric"><span class="metric-label">Lịch bảo vệ hội đồng:</span><span class="metric-val">${fmtDate(p.defenseStartDate)} — ${fmtDate(p.defenseEndDate)}</span></div>
              </div>
              <div class="period-config-chips">
                <span class="config-chip">${p.projectKindName || label('projectKind', p.projectKind)}</span>
                ${type ? html`<span class="config-chip">Tối đa ${type.maxMembers} SV / nhóm</span>
                  <span class="config-chip">${type.requiresReviewer ? 'Có phản biện' : 'Không phản biện'}</span>` : ''}
              </div>
              <div class="period-card-footer">
                <button class="btn btn-xs" data-open="${p.id}">Chi tiết & Lớp tham gia</button>
                ${auth.isAdmin && next ? html`<div class="quick-status-actions"><button class="btn btn-xs ${next.cls}" data-status="${next.status}" data-id="${p.id}">${next.text}</button></div>` : ''}
              </div>
            </div>`;
        })}
      </div>
      ${d.totalPages > 1 ? html`<div class="mt-4 card">${pagination(d)}</div>` : ''}`);
  }

  async function openCreate() {
    openModal({
      title: 'Tạo đợt đồ án mới',
      maxWidth: '640px',
      confirmText: 'Khởi tạo đợt',
      body: await periodForm(null),
      async onConfirm(form) {
        await api.post('/project-periods', formData(form));
        toast.success('Tạo đợt đồ án thành công.');
        clearLookups();
        await load();
      },
    });
  }

  on(root, 'click', '[data-filter]', (e, el) => { state.status = el.dataset.filter; state.page = 1; drawFilters(); load(); });
  on(root, 'click', '[data-page]', (e, el) => { state.page = Number(el.dataset.page); load(); });
  on(root, 'click', '[data-open]', (e, el) => navigate(`/periods/${el.dataset.open}`));
  on(root, 'click', '[data-action="create"]', () => openCreate());
  on(root, 'click', '[data-status]', async (e, el) => {
    await changePeriodStatus(el.dataset.id, el.dataset.status);
    await load();
  });
  $('#search', root).addEventListener('keyup', (e) => { if (e.key === 'Enter') { state.search = e.target.value.trim(); state.page = 1; load(); } });

  drawFilters();
  await load();
}
