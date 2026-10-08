import { html, setHtml, on, formData, icons, $ } from '../core/dom.js';
import { api, unwrap } from '../core/api.js';
import { auth } from '../core/auth.js';
import { toast } from '../core/toast.js';
import { lookup, lecturerName, studentName } from '../core/lookup.js';
import { fmtDate, fmtDateTime, fmtScore, label } from '../core/format.js';
import { pageHeader, statusBadge, badge, emptyState, skeletonRows, tabs, options, openModal, confirmDialog } from '../components/ui.js';

const EVALUATIONS = ['RAT_TOT', 'TOT', 'DAT', 'CAN_CAI_THIEN', 'KHONG_DAT'];
const DOCUMENT_TYPES = ['DE_CUONG', 'BAO_CAO_TIEN_DO', 'BAO_CAO', 'SLIDE', 'SOURCE_CODE', 'PHU_LUC', 'KHAC'];

export async function render(root, { params }) {
  const id = Number(params.id);
  setHtml(root, skeletonRows(4));
  const [lecturersList, periods, types, me] = await Promise.all([
    lookup.lecturers(), lookup.periods(), lookup.projectTypes(),
    auth.isLecturer ? lookup.myLecturer().catch(() => null) : auth.isStudent ? lookup.myStudent().catch(() => null) : null,
  ]);
  const lecturers = new Map(lecturersList.map((l) => [l.id, l]));
  const state = { tab: 'overview', project: null, reports: [], documents: [], reviewers: [], grades: [], results: [], students: new Map() };

  async function load() {
    const project = unwrap(await api.get(`/projects/${id}`));
    const [reports, documents, reviewers, grades, results, students] = await Promise.all([
      api.get(`/projects/${id}/progress-reports`, null, { silent: true }).then(unwrap).catch(() => []),
      api.get(`/projects/${id}/documents`, null, { silent: true }).then(unwrap).catch(() => []),
      api.get(`/defenses/projects/${id}/reviewers`, null, { silent: true }).then(unwrap).catch(() => []),
      auth.isStudent ? [] : api.get(`/projects/${id}/grades`, null, { silent: true }).then(unwrap).catch(() => []),
      api.get(`/projects/${id}/results`, null, { silent: true }).then(unwrap).catch(() => []),
      lookup.studentMap(project.members.map((m) => m.studentId)),
    ]);
    Object.assign(state, { project, reports: reports ?? [], documents: documents ?? [], reviewers: reviewers ?? [], grades: grades ?? [], results: results ?? [], students });
    draw();
  }

  const isAdvisor = () => auth.isLecturer && me && state.project.advisors.some((a) => a.isActive && a.lecturerId === me.id);
  const isMember = () => auth.isStudent && me && state.project.members.some((m) => m.studentId === me.id);

  function overview() {
    const p = state.project;
    const period = periods.find((x) => x.id === p.periodId);
    const type = types.find((t) => t.kind === period?.projectKind);
    return html`
      <div class="detail-grid">
        <div class="stack">
          <div class="card">
            <div class="card-header"><h3 class="card-title">Thông tin đồ án</h3></div>
            <div class="card-body info-list">
              ${[
                ['Mã đồ án', p.code],
                ['Đợt đồ án', period?.name ?? '—'],
                ['Loại đồ án', label('projectKind', period?.projectKind)],
                ['Ngày tạo', fmtDate(p.createdDate)],
                ['Bắt đầu / kết thúc', `${fmtDate(p.startDate)} — ${fmtDate(p.endDate)}`],
                ['Hạn nộp báo cáo', fmtDate(period?.reportDeadline)],
                ['Lịch bảo vệ của đợt', `${fmtDate(period?.defenseStartDate)} — ${fmtDate(period?.defenseEndDate)}`],
              ].map(([k, v]) => html`<div class="info-row"><span class="info-label">${k}</span><span class="info-value">${v}</span></div>`)}
            </div>
          </div>
          <div class="card">
            <div class="card-header"><h3 class="card-title">Thành viên (${p.members.length})</h3></div>
            <div class="card-body member-list">${p.members.map((m) => {
              const s = state.students.get(m.studentId);
              return html`<div class="member-row">
                <div class="user-avatar">${(s?.fullName || '?').split(' ').pop().charAt(0)}</div>
                <div class="flex-col" style="flex:1"><span class="member-name">${s?.fullName ?? `SV #${m.studentId}`}</span><span class="member-sub">${s?.code ?? ''} · ${s?.className ?? ''}</span></div>
                ${badge(label('memberRole', m.role), m.role === 'TRUONG_NHOM' ? 'accent' : 'default')}
              </div>`;
            })}</div>
          </div>
        </div>
        <div class="stack">
          <div class="card">
            <div class="card-header">
              <h3 class="card-title">Giảng viên hướng dẫn</h3>
              ${auth.isAdmin ? html`<button class="btn btn-xs" data-action="add-advisor">${icons.plus} Phân công</button>` : ''}
            </div>
            <div class="card-body member-list">
              ${p.advisors.filter((a) => a.isActive).length === 0 ? html`<p class="text-sm text-warning">Chưa phân công giảng viên hướng dẫn.</p>`
                : p.advisors.filter((a) => a.isActive).map((a) => html`
                  <div class="member-row">
                    <div class="flex-col" style="flex:1"><span class="member-name">${lecturerName(lecturers, a.lecturerId)}</span><span class="member-sub">${label('advisorType', a.advisorType)} · từ ${fmtDate(a.assignedDate)}</span></div>
                    ${auth.isAdmin ? html`<button class="btn btn-ghost btn-xs" data-remove-advisor="${a.id}">Gỡ</button>` : ''}
                  </div>`)}
            </div>
          </div>
          ${type?.requiresReviewer !== false ? html`
            <div class="card">
              <div class="card-header">
                <h3 class="card-title">Giảng viên phản biện</h3>
                ${auth.isAdmin ? html`<button class="btn btn-xs" data-action="add-reviewer">${icons.plus} Phân công</button>` : ''}
              </div>
              <div class="card-body member-list">
                ${state.reviewers.length === 0 ? html`<p class="text-sm text-muted">Chưa phân công phản biện.</p>`
                  : state.reviewers.map((r) => html`
                    <div class="member-row">
                      <div class="flex-col" style="flex:1"><span class="member-name">${lecturerName(lecturers, r.lecturerId)}</span><span class="member-sub">Từ ${fmtDate(r.assignedDate)}</span></div>
                      ${statusBadge(r.status)}
                    </div>`)}
              </div>
            </div>` : ''}
          ${isAdvisor() ? html`
            <div class="card">
              <div class="card-header"><h3 class="card-title">Yêu cầu chỉnh sửa</h3></div>
              <div class="card-body"><p class="text-sm text-secondary mb-3">Gửi yêu cầu chỉnh sửa kèm hạn hoàn thành cho nhóm.</p>
                <button class="btn btn-sm" data-action="revision">Tạo yêu cầu chỉnh sửa</button></div>
            </div>` : ''}
        </div>
      </div>`;
  }

  function progressTab() {
    const reports = [...state.reports].sort((a, b) => b.reportNumber - a.reportNumber);
    return html`
      ${isMember() ? html`<div class="flex justify-end mb-3"><button class="btn btn-primary btn-sm" data-action="report">${icons.plus} Nộp báo cáo tiến độ</button></div>` : ''}
      ${reports.length === 0 ? emptyState({ title: 'Chưa có báo cáo tiến độ', description: 'Sinh viên trong nhóm nộp báo cáo định kỳ để giảng viên nhận xét.' }) : html`
        <div class="card"><div class="card-body timeline-vertical">
          ${reports.map((r) => html`
            <div class="timeline-entry">
              <div class="timeline-marker">${r.reportNumber}</div>
              <div class="timeline-body">
                <div class="timeline-heading"><span class="title">${r.title}</span><span class="text-sm font-semibold">${r.completionPercentage}%</span></div>
                <div class="timeline-meta">${studentName(state.students, r.submittedByStudentId)} · ${fmtDateTime(r.submittedDate)}</div>
                <div class="progress-bar-container mt-2"><div class="progress-bar-fill" style="width:${r.completionPercentage}%"></div></div>
                <p class="section-text mt-2">${r.content}</p>
                ${r.workDone ? html`<div class="mt-2"><span class="section-label">Đã làm</span><p class="section-text">${r.workDone}</p></div>` : ''}
                ${r.nextTasks ? html`<div class="mt-2"><span class="section-label">Kế hoạch tiếp theo</span><p class="section-text">${r.nextTasks}</p></div>` : ''}
                ${r.difficulties ? html`<div class="mt-2"><span class="section-label">Khó khăn</span><p class="section-text">${r.difficulties}</p></div>` : ''}
                ${r.reviews.map((rv) => html`
                  <div class="review-box">
                    <div class="feedback-meta"><strong>${lecturerName(lecturers, rv.lecturerId)}</strong> · ${fmtDateTime(rv.reviewedDate)} · ${badge(label('evaluation', rv.evaluation), rv.evaluation === 'KHONG_DAT' ? 'danger' : rv.evaluation === 'CAN_CAI_THIEN' ? 'warning' : 'success')}</div>
                    <p class="section-text">${rv.reviewContent}</p>
                  </div>`)}
                ${isAdvisor() ? html`<button class="btn btn-xs mt-2" data-review="${r.id}">Nhận xét báo cáo</button>` : ''}
              </div>
            </div>`)}
        </div></div>`}`;
  }

  function documentsTab() {
    return html`
      ${isMember() ? html`<div class="flex justify-end mb-3"><button class="btn btn-primary btn-sm" data-action="upload">${icons.plus} Tải lên tài liệu</button></div>` : ''}
      ${state.documents.length === 0 ? emptyState({ title: 'Chưa có tài liệu', description: 'Đề cương, báo cáo, slide, mã nguồn... do nhóm tải lên.' }) : html`
        <div class="table-container">
          <table>
            <thead><tr><th>Tên tài liệu</th><th>Loại</th><th>Phiên bản</th><th>Người tải</th><th>Ngày tải</th><th></th></tr></thead>
            <tbody>${state.documents.map((d) => html`
              <tr>
                <td><strong>${d.documentName}</strong></td>
                <td>${label('documentType', d.documentType)}</td>
                <td>v${d.version}</td>
                <td>${studentName(state.students, d.uploadedByStudentId)}</td>
                <td>${fmtDateTime(d.uploadedDate)}</td>
                <td class="actions"><button class="btn btn-ghost btn-xs" data-download="${d.id}">Tải xuống</button></td>
              </tr>`)}</tbody>
          </table>
        </div>`}`;
  }

  function gradesTab() {
    const results = state.results;
    return html`
      ${auth.isAdmin && results.length && !results.some((r) => r.publishedDate) ? html`<div class="flex justify-end mb-3"><button class="btn btn-primary btn-sm" data-action="publish">Công bố kết quả</button></div>` : ''}
      <div class="card mb-4">
        <div class="card-header"><h3 class="card-title">Kết quả tổng kết</h3></div>
        ${results.length === 0 ? html`<div class="card-body text-sm text-muted">${auth.isStudent ? 'Kết quả sẽ hiển thị sau khi được công bố.' : 'Chưa có điểm.'}</div>` : html`
          <div class="table-container" style="border:none;border-radius:0">
            <table>
              <thead><tr><th>Sinh viên</th><th>Hướng dẫn</th><th>Phản biện</th><th>Hội đồng</th><th>Tổng kết</th><th>Xếp loại</th><th>Kết quả</th><th>Công bố</th></tr></thead>
              <tbody>${results.map((r) => html`
                <tr>
                  <td>${studentName(state.students, r.studentId)}</td>
                  <td>${fmtScore(r.advisorScore)}</td><td>${fmtScore(r.reviewerScore)}</td><td>${fmtScore(r.committeeScore)}</td>
                  <td><strong>${fmtScore(r.finalScore)}</strong></td>
                  <td>${r.finalScore !== null ? label('rank', r.rank) : '—'}</td>
                  <td>${statusBadge(r.result)}</td>
                  <td>${r.publishedDate ? fmtDate(r.publishedDate) : html`<span class="text-muted">Chưa</span>`}</td>
                </tr>`)}</tbody>
            </table>
          </div>`}
      </div>
      ${auth.isStudent ? '' : html`
        <div class="card">
          <div class="card-header"><h3 class="card-title">Phiếu chấm chi tiết</h3>${auth.isLecturer ? html`<a class="btn btn-xs" href="#/grading/entry?projectId=${id}">Nhập điểm</a>` : ''}</div>
          ${state.grades.length === 0 ? html`<div class="card-body text-sm text-muted">Chưa có phiếu chấm.</div>` : html`
            <div class="table-container" style="border:none;border-radius:0">
              <table>
                <thead><tr><th>Sinh viên</th><th>Loại</th><th>Giảng viên</th><th>Điểm</th><th>Nhận xét</th><th>Ngày chấm</th></tr></thead>
                <tbody>${state.grades.map((g) => html`
                  <tr><td>${studentName(state.students, g.studentId)}</td><td>${label('gradingType', g.gradingType)}</td>
                  <td>${lecturerName(lecturers, g.lecturerId)}</td><td><strong>${fmtScore(g.score)}</strong></td>
                  <td class="text-sm">${g.comments || '—'}</td><td>${fmtDate(g.gradedDate)}</td></tr>`)}</tbody>
              </table>
            </div>`}
        </div>`}`;
  }

  function draw() {
    const p = state.project;
    const body = { overview, progress: progressTab, documents: documentsTab, grades: gradesTab }[state.tab]();
    setHtml(root, html`
      ${pageHeader({ title: p.title, subtitle: p.code, backUrl: '#/projects', actions: statusBadge(p.status) })}
      ${tabs([
        { id: 'overview', label: 'Tổng quan' },
        { id: 'progress', label: 'Tiến độ', count: state.reports.length },
        { id: 'documents', label: 'Tài liệu', count: state.documents.length },
        { id: 'grades', label: 'Điểm & Kết quả' },
      ], state.tab)}
      ${body}`);
  }

  const lecturerOptions = (selected) => options(lecturersList, selected, { placeholder: '-- Chọn giảng viên --', text: (l) => `${l.fullName} (${l.degreeName} · ${l.departmentName})` });

  const actions = {
    'add-advisor': () => openModal({
      title: 'Phân công giảng viên hướng dẫn', confirmText: 'Phân công',
      body: html`<form>
        <div class="form-group"><label class="required">Giảng viên</label><select name="lecturerId" data-type="int" required>${lecturerOptions()}</select></div>
        <div class="form-group"><label class="required">Vai trò</label><select name="advisorType">${options([{ id: 'CHINH', name: 'Hướng dẫn chính' }, { id: 'PHU', name: 'Hướng dẫn phụ' }], 'CHINH')}</select></div>
      </form>`,
      async onConfirm(form) { await api.post(`/projects/${id}/advisors`, formData(form)); toast.success('Đã phân công giảng viên hướng dẫn.'); await load(); },
    }),
    'add-reviewer': () => openModal({
      title: 'Phân công giảng viên phản biện', confirmText: 'Phân công',
      body: html`<form><div class="form-group"><label class="required">Giảng viên</label><select name="lecturerId" data-type="int" required>${lecturerOptions()}</select>
        <span class="form-hint">Giảng viên hướng dẫn không được phản biện đồ án mình hướng dẫn.</span></div></form>`,
      async onConfirm(form) { await api.post(`/defenses/projects/${id}/reviewers`, formData(form)); toast.success('Đã phân công phản biện.'); await load(); },
    }),
    report: () => openModal({
      title: 'Nộp báo cáo tiến độ', maxWidth: '640px', confirmText: 'Nộp báo cáo',
      body: html`<form>
        <div class="grid-2">
          <div class="form-group"><label class="required">Lần báo cáo</label><input name="reportNumber" type="number" min="1" required value="${state.reports.reduce((m, r) => Math.max(m, r.reportNumber), 0) + 1}" /></div>
          <div class="form-group"><label class="required">Mức hoàn thành (%)</label><input name="completionPercentage" type="number" min="0" max="100" required value="${state.reports.reduce((m, r) => Math.max(m, r.completionPercentage), 0)}" /></div>
        </div>
        <div class="form-group"><label class="required">Tiêu đề</label><input name="title" required maxlength="200" /></div>
        <div class="form-group"><label class="required">Nội dung</label><textarea name="content" required></textarea></div>
        <div class="form-group"><label>Công việc đã làm</label><textarea name="workDone"></textarea></div>
        <div class="form-group"><label>Kế hoạch tiếp theo</label><textarea name="nextTasks"></textarea></div>
        <div class="form-group"><label>Khó khăn</label><textarea name="difficulties"></textarea></div>
      </form>`,
      async onConfirm(form) { await api.post(`/projects/${id}/progress-reports`, formData(form)); toast.success('Nộp báo cáo tiến độ thành công.'); await load(); },
    }),
    upload: () => openModal({
      title: 'Tải lên tài liệu', confirmText: 'Tải lên',
      body: html`<form>
        <div class="form-group"><label class="required">Tệp</label><input type="file" name="file" required /></div>
        <div class="form-group"><label class="required">Tên tài liệu</label><input name="documentName" required maxlength="200" /></div>
        <div class="grid-2">
          <div class="form-group"><label class="required">Loại tài liệu</label><select name="documentType">${options(DOCUMENT_TYPES.map((k) => ({ id: k, name: label('documentType', k) })), 'BAO_CAO')}</select></div>
          <div class="form-group"><label class="required">Phiên bản</label><input name="version" type="number" min="1" value="1" required /></div>
        </div>
      </form>`,
      onOpen(dialog) {
        const fileInput = $('[name=file]', dialog);
        fileInput.addEventListener('change', () => {
          const nameInput = $('[name=documentName]', dialog);
          if (!nameInput.value && fileInput.files[0]) nameInput.value = fileInput.files[0].name;
        });
      },
      async onConfirm(form) {
        const fd = new FormData(form);
        await api.upload(`/projects/${id}/documents`, fd);
        toast.success('Tải lên tài liệu thành công.');
        await load();
      },
    }),
    revision: () => openModal({
      title: 'Tạo yêu cầu chỉnh sửa', confirmText: 'Gửi yêu cầu',
      body: html`<form>
        <div class="form-group"><label class="required">Tiêu đề</label><input name="title" required maxlength="200" /></div>
        <div class="form-group"><label class="required">Nội dung</label><textarea name="content" required></textarea></div>
        <div class="form-group"><label>Hạn hoàn thành</label><input name="deadline" type="date" /></div>
      </form>`,
      async onConfirm(form) { await api.post(`/projects/${id}/revision-requests`, formData(form)); toast.success('Đã gửi yêu cầu chỉnh sửa.'); },
    }),
    publish: () => confirmDialog({
      title: 'Công bố kết quả', message: 'Sau khi công bố, điểm bị khóa và sinh viên xem được kết quả. Tiếp tục?', confirmText: 'Công bố',
      async onConfirm() { await api.post(`/projects/${id}/publish-result`); toast.success('Đã công bố kết quả.'); await load(); },
    }),
  };

  on(root, 'click', '[data-tab]', (e, el) => { state.tab = el.dataset.tab; draw(); });
  on(root, 'click', '[data-action]', (e, el) => actions[el.dataset.action]?.());
  on(root, 'click', '[data-remove-advisor]', (e, el) => confirmDialog({
    title: 'Gỡ giảng viên hướng dẫn', message: 'Gỡ phân công này?', confirmText: 'Gỡ', danger: true,
    async onConfirm() { await api.delete(`/projects/${id}/advisors/${el.dataset.removeAdvisor}`); toast.success('Đã gỡ phân công.'); await load(); },
  }));
  on(root, 'click', '[data-review]', (e, el) => openModal({
    title: 'Nhận xét báo cáo tiến độ', confirmText: 'Gửi nhận xét',
    body: html`<form>
      <div class="form-group"><label class="required">Đánh giá</label><select name="evaluation">${options(EVALUATIONS.map((k) => ({ id: k, name: label('evaluation', k) })), 'DAT')}</select></div>
      <div class="form-group"><label class="required">Nhận xét</label><textarea name="reviewContent" required></textarea></div>
    </form>`,
    async onConfirm(form) { await api.post(`/progress-reports/${el.dataset.review}/reviews`, formData(form)); toast.success('Đã gửi nhận xét.'); await load(); },
  }));
  on(root, 'click', '[data-download]', (e, el) => {
    const d = state.documents.find((x) => x.id === Number(el.dataset.download));
    api.download(`/projects/${id}/documents/${d.id}/file`, d.documentName);
  });

  await load();
}
