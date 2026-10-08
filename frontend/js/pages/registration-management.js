import { html, setHtml, on, formData, $ } from '../core/dom.js';
import { api, paged, unwrap } from '../core/api.js';
import { auth } from '../core/auth.js';
import { toast } from '../core/toast.js';
import { lookup, studentName, clearLookups } from '../core/lookup.js';
import { fmtDate, fmtDateTime, label } from '../core/format.js';
import { pageHeader, statusBadge, badge, emptyState, skeletonRows, segmented, tabs, pagination, options, openModal, promptDialog, confirmDialog } from '../components/ui.js';

const FILTERS = [
  { key: 'CHO_DUYET', label: 'Chờ duyệt' },
  { key: 'DA_DUYET', label: 'Đã duyệt' },
  { key: 'TU_CHOI', label: 'Từ chối' },
  { key: 'NHAP', label: 'Nháp' },
  { key: 'DA_HUY', label: 'Đã hủy' },
  { key: '', label: 'Tất cả' },
];

export async function render(root) {
  const [periods, lecturers, me] = await Promise.all([
    lookup.periods(), lookup.lecturers(), auth.isLecturer ? lookup.myLecturer().catch(() => null) : null,
  ]);
  const active = periods.filter((p) => !['DA_HUY'].includes(p.status));
  const state = {
    view: 'registrations', periodId: active.find((p) => p.status === 'DANG_MO_DANG_KY')?.id ?? active[0]?.id ?? '',
    status: 'CHO_DUYET', page: 1, data: null, changeStatus: 'CHO_DUYET', changes: [],
  };

  setHtml(root, html`
    ${pageHeader({
      title: auth.isLecturer ? 'Đăng ký chờ xử lý' : 'Quản lý đăng ký đề tài',
      subtitle: auth.isLecturer
        ? 'Duyệt hoặc từ chối nguyện vọng của các nhóm đăng ký vào đề tài bạn ra'
        : 'Theo dõi đơn đăng ký nhóm, nguyện vọng đề tài, yêu cầu đổi đề tài / đổi nhóm và tạo đồ án',
      actions: html`<select id="period" style="width:300px">${options(active, state.periodId, { placeholder: 'Tất cả đợt' })}</select>`,
    })}
    ${auth.isAdmin ? html`<div id="views"></div>` : ''}
    <div id="content"></div>`);

  const periodName = (id) => periods.find((p) => p.id === id)?.name ?? `Đợt #${id}`;

  // ---------- Đơn đăng ký ----------
  async function loadRegistrations() {
    const content = $('#content', root);
    setHtml(content, html`<div class="card p-4 filter-bar">${segmented(FILTERS, state.status)}</div>${skeletonRows(3)}`);
    state.data = paged(await api.get('/registrations', { periodId: state.periodId, status: state.status, pageNumber: state.page, pageSize: 20 }));
    const d = state.data;
    const students = await lookup.studentMap(d.items.flatMap((r) => r.members.filter((m) => m.role === 'TRUONG_NHOM').map((m) => m.studentId)));
    setHtml(content, html`
      <div class="card p-4 filter-bar">${segmented(FILTERS, state.status)}</div>
      ${d.items.length === 0 ? html`<div class="card p-6">${emptyState({ title: 'Không có đơn đăng ký', description: 'Không có đơn nào phù hợp với bộ lọc.' })}</div>` : html`
        <div class="table-container">
          <table>
            <thead><tr><th>Mã đơn</th><th>Nhóm</th><th>Trưởng nhóm</th><th>Thành viên</th><th>Nguyện vọng</th><th>Ngày đăng ký</th><th>Trạng thái</th><th></th></tr></thead>
            <tbody>${d.items.map((r) => {
              const leader = r.members.find((m) => m.role === 'TRUONG_NHOM');
              const pending = r.preferences.filter((p) => p.status === 'CHO_DUYET').length;
              return html`
                <tr>
                  <td class="text-mono">#${r.id}</td>
                  <td><strong>${r.groupName || 'Nhóm không tên'}</strong><div class="text-xs text-muted">${periodName(r.periodId)}</div></td>
                  <td>${leader ? studentName(students, leader.studentId) : '—'}</td>
                  <td>${r.members.length}</td>
                  <td>${r.preferences.length}${pending ? html` · <span class="text-warning">${pending} chờ duyệt</span>` : ''}</td>
                  <td>${fmtDate(r.registrationDate)}</td>
                  <td>${statusBadge(r.status)}</td>
                  <td class="actions"><button class="btn btn-ghost btn-xs" data-open="${r.id}">Xem & xử lý</button></td>
                </tr>`;
            })}</tbody>
          </table>
          ${pagination(d)}
        </div>`}`);
  }

  async function openRegistration(id) {
    const r = unwrap(await api.get(`/registrations/${id}`));
    const [students, periodTopics, projects] = await Promise.all([
      lookup.studentMap(r.members.map((m) => m.studentId)),
      lookup.periodTopics(r.periodId),
      auth.isAdmin && r.status === 'DA_DUYET' ? api.get('/projects', { periodId: r.periodId, pageSize: 100 }).then((x) => paged(x).items) : [],
    ]);
    const topics = await Promise.all(r.preferences.map((p) => {
      const pt = periodTopics.find((t) => t.id === p.periodTopicId);
      return pt ? lookup.topic(pt.topicId).then((t) => ({ pt, t })).catch(() => ({ pt, t: null })) : { pt: null, t: null };
    }));
    const canDecide = (t) => auth.isAdmin || (me && t?.proposedByLecturerId === me.id);
    const hasProject = projects.some((p) => p.registrationId === r.id && p.status !== 'DA_HUY');
    const prefs = r.preferences.map((p, i) => ({ ...p, ...topics[i] })).sort((a, b) => a.priorityOrder - b.priorityOrder);

    const modal = openModal({
      title: `Đơn đăng ký #${r.id} · ${r.groupName || 'Nhóm không tên'}`,
      maxWidth: '720px',
      hasFooter: false,
      body: html`
        <div class="space-y-4">
          <div class="detail-header-tags">${statusBadge(r.status)}<span class="text-xs text-muted">${periodName(r.periodId)} · đăng ký ${fmtDateTime(r.registrationDate)}</span></div>
          <div>
            <h4 class="section-label">Thành viên</h4>
            <div class="member-list">${r.members.map((m) => html`
              <div class="member-row"><span class="member-name" style="flex:1">${studentName(students, m.studentId)}</span>${badge(label('memberRole', m.role), m.role === 'TRUONG_NHOM' ? 'accent' : 'default')}</div>`)}
            </div>
          </div>
          <div>
            <h4 class="section-label">Nguyện vọng</h4>
            ${prefs.length === 0 ? html`<p class="text-sm text-muted">Chưa có nguyện vọng.</p>` : prefs.map((p) => html`
              <div class="pref-item">
                <div class="pref-order">${p.priorityOrder}</div>
                <div style="flex:1">
                  <div class="member-name">${p.pt?.topicTitle ?? `Đề tài #${p.periodTopicId}`}</div>
                  <div class="member-sub">${p.pt?.topicCode ?? ''}${p.rejectReason ? html` · <span class="text-danger">${p.rejectReason}</span>` : ''}</div>
                </div>
                ${statusBadge(p.status)}
                ${r.status === 'CHO_DUYET' && p.status === 'CHO_DUYET' && canDecide(p.t) ? html`
                  <button class="btn btn-accent btn-xs" data-approve-pref="${p.id}">Chọn</button>
                  <button class="btn btn-danger btn-xs" data-reject-pref="${p.id}">Từ chối</button>` : ''}
              </div>`)}
          </div>
          ${auth.isAdmin ? html`
            <div class="flex justify-end gap-2">
              ${['NHAP', 'CHO_DUYET', 'DA_DUYET'].includes(r.status) ? html`<button class="btn btn-danger btn-sm" data-cancel-reg>Hủy đơn</button>` : ''}
              ${r.status === 'DA_DUYET' && !hasProject ? html`<button class="btn btn-primary btn-sm" data-create-project>Tạo đồ án từ đơn này</button>` : ''}
              ${hasProject ? html`<span class="text-sm text-muted">Đã có đồ án cho đơn này.</span>` : ''}
            </div>` : ''}
        </div>`,
    });

    const refresh = async () => { modal.close(); clearLookups(); await loadRegistrations(); };
    modal.el.addEventListener('click', async (e) => {
      const approve = e.target.closest('[data-approve-pref]');
      const reject = e.target.closest('[data-reject-pref]');
      if (approve) {
        await api.post(`/registration-preferences/${approve.dataset.approvePref}/approve`);
        toast.success('Đã duyệt nguyện vọng. Đơn đăng ký được duyệt, các nguyện vọng khác tự động bị từ chối.');
        await refresh();
      } else if (reject) {
        promptDialog({
          title: 'Từ chối nguyện vọng', label: 'Lý do', confirmText: 'Từ chối', danger: true,
          async onConfirm(reason) { await api.post(`/registration-preferences/${reject.dataset.rejectPref}/reject`, { reason }); toast.success('Đã từ chối nguyện vọng.'); await refresh(); },
        });
      } else if (e.target.closest('[data-cancel-reg]')) {
        confirmDialog({
          title: 'Hủy đơn đăng ký', message: 'Đơn sẽ chuyển sang trạng thái Đã hủy.', confirmText: 'Hủy đơn', danger: true,
          async onConfirm() { await api.post(`/registrations/${r.id}/cancel`); toast.success('Đã hủy đơn.'); await refresh(); },
        });
      } else if (e.target.closest('[data-create-project]')) {
        const chosen = prefs.find((p) => p.status === 'DUOC_CHON');
        modal.close();
        createProject(r, chosen, students);
      }
    });
  }

  function createProject(r, chosen, students) {
    const period = periods.find((p) => p.id === r.periodId);
    const leader = r.members.find((m) => m.role === 'TRUONG_NHOM');
    openModal({
      title: 'Tạo đồ án từ đơn đăng ký', maxWidth: '620px', confirmText: 'Tạo đồ án',
      body: html`<form>
        <div class="info-list mb-4">
          <div class="info-row"><span class="info-label">Đề tài được chọn</span><span class="info-value">${chosen?.pt?.topicTitle ?? '—'}</span></div>
          <div class="info-row"><span class="info-label">Thành viên</span><span class="info-value">${r.members.map((m) => studentName(students, m.studentId)).join(', ')}</span></div>
        </div>
        <div class="form-group"><label class="required">Mã đồ án</label><input name="code" required maxlength="30" value="${period?.code ?? 'DA'}-${r.id}" /></div>
        <div class="form-group"><label class="required">Tên đồ án</label><input name="title" required maxlength="300" value="${chosen?.pt?.topicTitle ?? ''}" /></div>
        <div class="form-group"><label>Giảng viên hướng dẫn chính</label>
          <select name="primaryAdvisorLecturerId" data-type="int">${options(lecturers, chosen?.t?.proposedByLecturerId, { placeholder: '-- Phân công sau --', text: (l) => `${l.fullName} (${l.degreeName})` })}</select></div>
      </form>`,
      async onConfirm(form) {
        if (!chosen?.pt) { toast.error('Đơn chưa có nguyện vọng được chọn.'); return false; }
        const d = formData(form);
        await api.post('/projects', {
          ...d,
          periodId: r.periodId,
          topicId: chosen.pt.topicId,
          registrationId: r.id,
          members: r.members.map((m) => ({ studentId: m.studentId, role: m.studentId === leader?.studentId ? 'TRUONG_NHOM' : 'THANH_VIEN' })),
        });
        toast.success('Tạo đồ án thành công.');
        await loadRegistrations();
      },
    });
  }

  // ---------- Yêu cầu đổi đề tài / đổi nhóm (Admin) ----------
  async function loadChanges() {
    const content = $('#content', root);
    const filters = [{ key: 'CHO_DUYET', label: 'Chờ duyệt' }, { key: 'DA_DUYET', label: 'Đã duyệt' }, { key: 'TU_CHOI', label: 'Từ chối' }, { key: '', label: 'Tất cả' }];
    setHtml(content, html`<div class="card p-4 filter-bar">${segmented(filters, state.changeStatus)}</div>${skeletonRows(3)}`);
    state.changes = unwrap(await api.get('/registration-change-requests', { periodId: state.periodId, status: state.changeStatus })) ?? [];
    const students = await lookup.studentMap(state.changes.map((c) => c.studentId));
    const topicsByPeriod = state.periodId ? await lookup.periodTopics(state.periodId) : [];
    setHtml(content, html`
      <div class="card p-4 filter-bar">${segmented(filters, state.changeStatus)}</div>
      ${state.changes.length === 0 ? html`<div class="card p-6">${emptyState({ title: 'Không có yêu cầu nào' })}</div>` : html`
        <div class="table-container">
          <table>
            <thead><tr><th>Loại</th><th>Sinh viên</th><th>Đơn hiện tại</th><th>Thay đổi</th><th>Lý do</th><th>Ngày gửi</th><th>Trạng thái</th><th></th></tr></thead>
            <tbody>${state.changes.map((c) => html`
              <tr>
                <td>${badge(label('changeType', c.type), c.type === 'DOI_DE_TAI' ? 'info' : 'accent')}</td>
                <td>${studentName(students, c.studentId)}</td>
                <td class="text-mono">#${c.registrationId}</td>
                <td>${c.type === 'DOI_DE_TAI' ? (topicsByPeriod.find((t) => t.id === c.newPeriodTopicId)?.topicTitle ?? `Đề tài #${c.newPeriodTopicId}`) : `Sang đơn #${c.targetRegistrationId}`}</td>
                <td class="text-sm">${c.reason}${c.processNote ? html`<div class="text-xs text-muted">Ghi chú: ${c.processNote}</div>` : ''}</td>
                <td>${fmtDate(c.requestedDate)}</td>
                <td>${statusBadge(c.status)}</td>
                <td class="actions">${c.status === 'CHO_DUYET' ? html`
                  <button class="btn btn-accent btn-xs" data-approve-change="${c.id}">Duyệt</button>
                  <button class="btn btn-danger btn-xs" data-reject-change="${c.id}">Từ chối</button>` : ''}</td>
              </tr>`)}</tbody>
          </table>
        </div>`}`);
  }

  function drawViews() {
    if (!auth.isAdmin) return;
    setHtml($('#views', root), tabs([{ id: 'registrations', label: 'Đơn đăng ký' }, { id: 'changes', label: 'Yêu cầu đổi đề tài / nhóm' }], state.view));
  }
  const loadView = () => (state.view === 'registrations' ? loadRegistrations() : loadChanges());

  on(root, 'click', '[data-tab]', (e, el) => { state.view = el.dataset.tab; drawViews(); loadView(); });
  on(root, 'click', '[data-filter]', (e, el) => {
    if (state.view === 'registrations') { state.status = el.dataset.filter; state.page = 1; } else state.changeStatus = el.dataset.filter;
    loadView();
  });
  on(root, 'click', '[data-page]', (e, el) => { state.page = Number(el.dataset.page); loadRegistrations(); });
  on(root, 'click', '[data-open]', (e, el) => openRegistration(Number(el.dataset.open)));
  on(root, 'click', '[data-approve-change]', (e, el) => {
    promptDialog({
      title: 'Duyệt yêu cầu thay đổi', label: 'Ghi chú (tùy chọn)', required: false, confirmText: 'Duyệt',
      async onConfirm(note) {
        await api.post(`/registration-change-requests/${el.dataset.approveChange}/approve`, { note: note || null });
        toast.success('Đã duyệt và áp dụng thay đổi.');
        clearLookups();
        await loadChanges();
      },
    });
  });
  on(root, 'click', '[data-reject-change]', (e, el) => {
    promptDialog({
      title: 'Từ chối yêu cầu', label: 'Lý do', confirmText: 'Từ chối', danger: true,
      async onConfirm(reason) { await api.post(`/registration-change-requests/${el.dataset.rejectChange}/reject`, { reason }); toast.success('Đã từ chối yêu cầu.'); await loadChanges(); },
    });
  });
  $('#period', root).addEventListener('change', (e) => { state.periodId = e.target.value; state.page = 1; loadView(); });

  drawViews();
  await loadView();
}
