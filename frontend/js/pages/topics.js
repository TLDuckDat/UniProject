import { html, setHtml, on, formData, icons, $ } from '../core/dom.js';
import { api, paged } from '../core/api.js';
import { auth } from '../core/auth.js';
import { toast } from '../core/toast.js';
import { lookup, lecturerName, clearLookups } from '../core/lookup.js';
import { fmtDate, label, statusLabel } from '../core/format.js';
import { pageHeader, pagination, emptyState, skeletonCards, statusBadge, options, openModal } from '../components/ui.js';
import { navigate } from '../core/router.js';
import { topicFormBody } from './topic-proposal.js';

const parseTechs = (s) => (s || '').split(/[,;]/).map((x) => x.trim()).filter(Boolean);

export function topicDetailBody(t, lecturers, majors) {
  const major = majors.find((m) => m.id === t.majorId);
  return html`
    <div class="space-y-4">
      <div class="detail-header-tags">
        <span class="code-pill">${t.code}</span>
        ${statusBadge(t.status)}
        <span class="text-xs text-muted">${label('projectKind', t.projectKind)} · Tối đa ${t.maxStudents} sinh viên</span>
      </div>
      <div class="detail-section"><h4 class="section-label">Mô tả đề tài</h4><p class="section-text">${t.description}</p></div>
      ${t.objectives ? html`<div class="detail-section"><h4 class="section-label">Mục tiêu / Sản phẩm</h4><p class="section-text">${t.objectives}</p></div>` : ''}
      ${t.requirements ? html`<div class="detail-section"><h4 class="section-label">Yêu cầu kiến thức & kỹ năng</h4><p class="section-text">${t.requirements}</p></div>` : ''}
      ${t.expectedTechnologies ? html`<div class="detail-section"><h4 class="section-label">Công nghệ dự kiến</h4>
        <div class="tech-tags">${parseTechs(t.expectedTechnologies).map((x) => html`<span class="tech-tag">${x}</span>`)}</div></div>` : ''}
      ${t.rejectReason ? html`<div class="detail-section"><h4 class="section-label">Lý do từ chối</h4><p class="section-text text-danger">${t.rejectReason}</p></div>` : ''}
      <div class="info-list">
        <div class="info-row"><span class="info-label">Ngành</span><span class="info-value">${major?.name ?? '—'}</span></div>
        <div class="info-row"><span class="info-label">Giảng viên ${t.proposedByStudentId ? 'dự kiến hướng dẫn' : 'đề xuất'}</span><span class="info-value">${lecturerName(lecturers, t.proposedByLecturerId)}</span></div>
        ${t.proposedByStudentId ? html`<div class="info-row"><span class="info-label">Nguồn đề xuất</span><span class="info-value">Sinh viên đề xuất</span></div>` : ''}
        <div class="info-row"><span class="info-label">Ngày tạo</span><span class="info-value">${fmtDate(t.createdAt)}</span></div>
        ${t.approvedDate ? html`<div class="info-row"><span class="info-label">Ngày duyệt</span><span class="info-value">${fmtDate(t.approvedDate)} · ${lecturerName(lecturers, t.approvedByLecturerId)}</span></div>` : ''}
      </div>
    </div>`;
}

export async function render(root, { query }) {
  const state = {
    page: 1, search: query.search || '', majorId: '', projectKind: '',
    status: auth.isStudent ? 'DA_DUYET' : '', data: null,
  };
  const [majors, lecturers, me] = await Promise.all([
    lookup.majors(), lookup.lecturerMap(), auth.isLecturer ? lookup.myLecturer().catch(() => null) : null,
  ]);

  setHtml(root, html`
    ${pageHeader({
      title: 'Tra cứu & Danh mục Đề tài',
      subtitle: 'Khám phá các hướng nghiên cứu, công nghệ ứng dụng và đề tài đồ án được phê duyệt',
      actions: html`
        ${auth.isLecturer || auth.isAdmin ? html`<a class="btn btn-primary btn-sm" href="#/topics/proposals">${icons.plus} Đề xuất đề tài mới</a>` : ''}
        ${auth.isStudent ? html`<a class="btn btn-sm" href="#/topics/proposals">Tự đề xuất đề tài</a><a class="btn btn-primary btn-sm" href="#/registrations/wizard">Đăng ký đề tài</a>` : ''}`,
    })}
    <div class="card p-4 filter-bar">
      <div class="filter-grid">
        <div class="search-wrap">${icons.search}<input type="text" class="search-input" id="search" value="${state.search}" placeholder="Tìm theo tên đề tài, mã, từ khóa..." /></div>
        <select class="filter-select" id="major">${options(majors, '', { placeholder: 'Tất cả ngành / chuyên ngành' })}</select>
        <select class="filter-select" id="kind">${options([{ id: 'DO_AN_CO_SO', name: 'Đồ án cơ sở' }, { id: 'DO_AN_CHUYEN_NGANH', name: 'Đồ án chuyên ngành' }, { id: 'DO_AN_MON_HOC', name: 'Đồ án môn học' }, { id: 'DO_AN_TOT_NGHIEP', name: 'Đồ án tốt nghiệp' }], '', { placeholder: 'Tất cả loại đồ án' })}</select>
        ${auth.isStudent ? '' : html`<select class="filter-select" id="status">${options(['DA_DUYET', 'CHO_DUYET', 'TU_CHOI', 'NGUNG_SU_DUNG'].map((s) => ({ id: s, name: statusLabel(s) })), '', { placeholder: 'Tất cả trạng thái' })}</select>`}
        <button class="btn btn-sm" data-action="search">Tìm kiếm</button>
      </div>
    </div>
    <div id="list"></div>`);

  async function load() {
    setHtml($('#list', root), skeletonCards(4));
    state.data = paged(await api.get('/topics', {
      pageNumber: state.page, pageSize: 12, search: state.search,
      majorId: state.majorId, projectKind: state.projectKind, status: state.status,
    }));
    const d = state.data;
    if (!d.items.length) {
      setHtml($('#list', root), html`<div class="card p-6">${emptyState({
        title: 'Không tìm thấy đề tài nào',
        description: 'Hiện chưa có đề tài phù hợp với tiêu chí tìm kiếm hoặc ngành đã chọn.',
        actionText: auth.isLecturer ? 'Đề xuất đề tài ngay' : '', action: 'propose',
      })}</div>`);
      return;
    }
    setHtml($('#list', root), html`
      <div class="topic-grid">
        ${d.items.map((t) => html`
          <div class="card topic-card">
            <div class="topic-card-header">
              <div class="topic-code">${t.code} · ${label('projectKind', t.projectKind)}</div>
              ${statusBadge(t.status)}
            </div>
            <div class="topic-card-body">
              <h3 class="topic-title" data-detail="${t.id}">${t.title}</h3>
              <p class="topic-desc">${t.description}</p>
              ${t.expectedTechnologies ? html`<div class="tech-tags">${parseTechs(t.expectedTechnologies).map((x) => html`<span class="tech-tag">${x}</span>`)}</div>` : ''}
            </div>
            <div class="topic-card-footer">
              <div class="topic-meta">
                <span class="meta-item">${icons.users} Tối đa ${t.maxStudents} SV</span>
                <span class="meta-item text-secondary">GV: ${lecturerName(lecturers, t.proposedByLecturerId)}</span>
              </div>
              <div class="topic-actions">
                <button class="btn btn-ghost btn-xs" data-detail="${t.id}">Xem chi tiết</button>
                ${(auth.isAdmin || (me && t.proposedByLecturerId === me.id)) && t.status !== 'NGUNG_SU_DUNG' ? html`<button class="btn btn-xs" data-edit="${t.id}">Sửa</button>` : ''}
                ${auth.isAdmin && t.status === 'DA_DUYET' ? html`<button class="btn btn-xs" data-assign="${t.id}">+ Mở trong đợt</button>` : ''}
                ${auth.isStudent && t.status === 'DA_DUYET' ? html`<a class="btn btn-primary btn-xs" href="#/registrations/wizard">Đăng ký</a>` : ''}
              </div>
            </div>
          </div>`)}
      </div>
      ${d.totalPages > 1 ? html`<div class="mt-4 card">${pagination(d)}</div>` : ''}`);
  }

  const find = (id) => state.data.items.find((t) => t.id === Number(id));
  const applySearch = () => { state.search = $('#search', root).value.trim(); state.page = 1; load(); };

  on(root, 'click', '[data-detail]', (e, el) => {
    const t = find(el.dataset.detail);
    openModal({ title: t.title, maxWidth: '680px', hasFooter: false, body: topicDetailBody(t, lecturers, majors) });
  });
  on(root, 'click', '[data-edit]', async (e, el) => {
    const t = find(el.dataset.edit);
    openModal({
      title: 'Cập nhật đề tài', maxWidth: '680px', confirmText: 'Lưu thay đổi',
      body: await topicFormBody(t),
      async onConfirm(form) {
        await api.put(`/topics/${t.id}`, formData(form));
        toast.success('Cập nhật đề tài thành công.');
        await load();
      },
    });
  });
  on(root, 'click', '[data-assign]', async (e, el) => {
    const t = find(el.dataset.assign);
    const periods = (await lookup.periods()).filter((p) => p.projectKind === t.projectKind && !['DA_KET_THUC', 'DA_HUY'].includes(p.status));
    openModal({
      title: 'Mở đề tài trong đợt đồ án', confirmText: 'Mở đề tài',
      body: html`<form>
        <p class="text-sm text-secondary mb-3"><strong>${t.code}</strong> · ${t.title}</p>
        <div class="form-group"><label class="required">Đợt đồ án (${label('projectKind', t.projectKind)})</label>
          <select name="periodId" data-type="int" required>${options(periods, '', { placeholder: '-- Chọn đợt --' })}</select></div>
      </form>`,
      async onConfirm(form) {
        await api.post('/topics/periods', { topicId: t.id, periodId: Number(form.elements.periodId.value), status: 'DANG_MO' });
        toast.success('Đã mở đề tài trong đợt.');
        clearLookups();
      },
    });
  });
  on(root, 'click', '[data-page]', (e, el) => { state.page = Number(el.dataset.page); load(); });
  on(root, 'click', '[data-action="search"]', applySearch);
  on(root, 'click', '[data-action="propose"]', () => navigate('/topics/proposals'));
  $('#search', root).addEventListener('keyup', (e) => { if (e.key === 'Enter') applySearch(); });
  $('#major', root).addEventListener('change', (e) => { state.majorId = e.target.value; state.page = 1; load(); });
  $('#kind', root).addEventListener('change', (e) => { state.projectKind = e.target.value; state.page = 1; load(); });
  $('#status', root)?.addEventListener('change', (e) => { state.status = e.target.value; state.page = 1; load(); });

  await load();
}
