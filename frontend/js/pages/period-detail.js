import { html, setHtml, on, formData, icons, $ } from '../core/dom.js';
import { api, paged, unwrap } from '../core/api.js';
import { auth } from '../core/auth.js';
import { toast } from '../core/toast.js';
import { lookup, clearLookups } from '../core/lookup.js';
import { fmtDate, label, statusLabel } from '../core/format.js';
import { pageHeader, statusBadge, emptyState, skeletonRows, tabs, options, openModal, confirmDialog } from '../components/ui.js';
import { periodForm, changePeriodStatus, NEXT_STATUS } from './periods.js';

const TOPIC_STATUSES = ['CHUA_MO', 'DANG_MO', 'DA_DONG', 'TAM_KHOA'];

export async function render(root, { params }) {
  const id = Number(params.id);
  const state = { tab: 'overview', period: null, classIds: [], topics: [] };
  setHtml(root, skeletonRows(4));

  const [classes, types] = await Promise.all([lookup.classes(), lookup.projectTypes()]);

  async function load() {
    const [period, classIds, topics] = await Promise.all([
      api.get(`/project-periods/${id}`).then(unwrap),
      api.get(`/project-periods/${id}/classes`).then(unwrap),
      api.get(`/topics/periods/${id}`).then(unwrap),
    ]);
    state.period = period;
    state.classIds = classIds ?? [];
    state.topics = topics ?? [];
    draw();
  }

  function overview() {
    const p = state.period;
    const type = types.find((t) => t.kind === p.projectKind);
    return html`
      <div class="detail-grid">
        <div class="card">
          <div class="card-header"><h3 class="card-title">Mốc thời gian</h3></div>
          <div class="card-body info-list">
            ${[
              ['Thời gian thực hiện', `${fmtDate(p.startDate)} — ${fmtDate(p.endDate)}`],
              ['Hạn đăng ký nhóm', fmtDate(p.registrationDeadline)],
              ['Hạn nộp đề cương', fmtDate(p.outlineDeadline)],
              ['Hạn nộp báo cáo', fmtDate(p.reportDeadline)],
              ['Thời gian bảo vệ', `${fmtDate(p.defenseStartDate)} — ${fmtDate(p.defenseEndDate)}`],
              ['Trạng thái', statusLabel(p.status)],
            ].map(([k, v]) => html`<div class="info-row"><span class="info-label">${k}</span><span class="info-value">${v}</span></div>`)}
          </div>
        </div>
        <div class="card">
          <div class="card-header"><h3 class="card-title">Quy định loại đồ án</h3></div>
          <div class="card-body info-list">
            ${type ? [
              ['Loại đồ án', type.name],
              ['Số SV tối đa / nhóm', type.maxMembers],
              ['Giảng viên phản biện', type.requiresReviewer ? 'Có' : 'Không'],
              ['Trọng số HD / PB / HĐ', `${type.advisorWeight} / ${type.reviewerWeight} / ${type.committeeWeight}`],
              ['Điểm đạt', type.passingScore],
              ['Điểm tối thiểu mỗi cột', type.minComponentScore ?? 'Không áp dụng'],
              ['Chế độ chấm', type.gradingMode === 'CHAM_CHUNG' ? 'Chấm chung cả nhóm' : 'Chấm riêng từng SV'],
            ].map(([k, v]) => html`<div class="info-row"><span class="info-label">${k}</span><span class="info-value">${v}</span></div>`) : 'Không có cấu hình'}
          </div>
        </div>
      </div>`;
  }

  function classesTab() {
    const list = state.classIds.map((cid) => classes.find((c) => c.id === cid)).filter(Boolean);
    return html`
      ${auth.isAdmin ? html`<div class="flex justify-end mb-3"><button class="btn btn-primary btn-sm" data-action="add-class">${icons.plus} Thêm lớp tham gia</button></div>` : ''}
      ${list.length === 0 ? emptyState({ title: 'Chưa có lớp tham gia', description: 'Sinh viên chỉ đăng ký được khi lớp của mình thuộc đợt đồ án.' }) : html`
        <div class="table-container">
          <table>
            <thead><tr><th>Mã lớp</th><th>Tên lớp</th><th>Năm nhập học</th><th>Ngành</th>${auth.isAdmin ? html`<th></th>` : ''}</tr></thead>
            <tbody>${list.map((c) => html`
              <tr><td class="text-mono">${c.code}</td><td>${c.name}</td><td>${c.admissionYear}</td><td>${c.majorName}</td>
              ${auth.isAdmin ? html`<td class="actions"><button class="btn btn-danger btn-xs" data-action="remove-class" data-id="${c.id}">Gỡ</button></td>` : ''}</tr>`)}
            </tbody>
          </table>
        </div>`}`;
  }

  function topicsTab() {
    return html`
      ${auth.isAdmin ? html`<div class="flex justify-end mb-3"><button class="btn btn-primary btn-sm" data-action="add-topic">${icons.plus} Mở đề tài trong đợt</button></div>` : ''}
      ${state.topics.length === 0 ? emptyState({ title: 'Chưa có đề tài nào được mở', description: 'Admin mở các đề tài đã duyệt (cùng loại đồ án) để sinh viên đăng ký.' }) : html`
        <div class="table-container">
          <table>
            <thead><tr><th>Mã đề tài</th><th>Tên đề tài</th><th>SV tối đa</th><th>Trạng thái</th>${auth.isAdmin ? html`<th></th>` : ''}</tr></thead>
            <tbody>${state.topics.map((t) => html`
              <tr>
                <td class="text-mono">${t.topicCode}</td><td>${t.topicTitle}</td><td>${t.maxStudents}</td><td>${statusBadge(t.status)}</td>
                ${auth.isAdmin ? html`<td class="actions">
                  <select class="form-input" style="width:140px;height:28px;font-size:12px" data-topic-status="${t.id}">
                    ${options(TOPIC_STATUSES.map((s) => ({ id: s, name: statusLabel(s) })), t.status)}
                  </select></td>` : ''}
              </tr>`)}
            </tbody>
          </table>
        </div>`}`;
  }

  function draw() {
    const p = state.period;
    const next = NEXT_STATUS[p.status];
    setHtml(root, html`
      ${pageHeader({
        title: p.name,
        subtitle: `${p.code} · ${p.projectKindName || label('projectKind', p.projectKind)}`,
        backUrl: '#/periods',
        actions: html`${statusBadge(p.status)}
          ${auth.isAdmin ? html`
            <button class="btn btn-sm" data-action="edit">Sửa thông tin</button>
            ${next ? html`<button class="btn btn-sm ${next.cls}" data-action="status" data-status="${next.status}">${next.text}</button>` : ''}
            ${!['DA_KET_THUC', 'DA_HUY'].includes(p.status) ? html`<button class="btn btn-sm btn-danger" data-action="status" data-status="DA_HUY">Hủy đợt</button>` : ''}` : ''}`,
      })}
      ${tabs([
        { id: 'overview', label: 'Tổng quan' },
        { id: 'classes', label: 'Lớp tham gia', count: state.classIds.length },
        { id: 'topics', label: 'Đề tài trong đợt', count: state.topics.length },
      ], state.tab)}
      <div id="tab-body">${state.tab === 'overview' ? overview() : state.tab === 'classes' ? classesTab() : topicsTab()}</div>`);
  }

  on(root, 'click', '[data-tab]', (e, el) => { state.tab = el.dataset.tab; draw(); });
  on(root, 'change', '[data-topic-status]', async (e, el) => {
    await api.patch(`/topics/period-topics/${el.dataset.topicStatus}/status`, { status: el.value });
    toast.success('Đã cập nhật trạng thái đề tài trong đợt.');
    clearLookups();
    await load();
  });
  on(root, 'click', '[data-action]', async (e, el) => {
    const a = el.dataset.action;
    if (a === 'status') {
      confirmDialog({
        message: `Chuyển đợt sang trạng thái "${statusLabel(el.dataset.status)}"?`,
        danger: el.dataset.status === 'DA_HUY',
        async onConfirm() { await changePeriodStatus(id, el.dataset.status); await load(); },
      });
    } else if (a === 'edit') {
      openModal({
        title: 'Cập nhật đợt đồ án', maxWidth: '640px', confirmText: 'Lưu thay đổi',
        body: await periodForm(state.period),
        async onConfirm(form) {
          await api.put(`/project-periods/${id}`, formData(form));
          toast.success('Cập nhật đợt đồ án thành công.');
          clearLookups();
          await load();
        },
      });
    } else if (a === 'add-class') {
      const available = classes.filter((c) => !state.classIds.includes(c.id))
        .sort((x, y) => y.admissionYear - x.admissionYear || x.code.localeCompare(y.code));
      openModal({
        title: 'Thêm lớp tham gia', confirmText: 'Thêm lớp',
        body: html`<form><div class="form-group"><label class="required">Lớp</label>
          <select name="classId" required>${options(available, '', { placeholder: '-- Chọn lớp --', text: (c) => `${c.code} - ${c.name}` })}</select></div></form>`,
        async onConfirm(form) {
          await api.post(`/project-periods/${id}/classes/${form.elements.classId.value}`);
          toast.success('Đã thêm lớp vào đợt.');
          await load();
        },
      });
    } else if (a === 'remove-class') {
      await api.delete(`/project-periods/${id}/classes/${el.dataset.id}`);
      toast.success('Đã gỡ lớp khỏi đợt.');
      await load();
    } else if (a === 'add-topic') {
      const approved = paged(await api.get('/topics', { status: 'DA_DUYET', projectKind: state.period.projectKind, pageSize: 100 })).items
        .filter((t) => !state.topics.some((pt) => pt.topicId === t.id));
      openModal({
        title: 'Mở đề tài trong đợt', confirmText: 'Mở đề tài',
        body: approved.length ? html`<form>
          <div class="form-group"><label class="required">Đề tài đã duyệt (${label('projectKind', state.period.projectKind)})</label>
            <select name="topicId" data-type="int" required>${options(approved, '', { placeholder: '-- Chọn đề tài --', text: (t) => `${t.code} - ${t.title}` })}</select></div>
          <div class="form-group"><label>Trạng thái ban đầu</label>
            <select name="status">${options(TOPIC_STATUSES.map((s) => ({ id: s, name: statusLabel(s) })), 'DANG_MO')}</select></div>
        </form>` : emptyState({ title: 'Không còn đề tài phù hợp', description: 'Không có đề tài đã duyệt cùng loại đồ án chưa được mở trong đợt.' }),
        async onConfirm(form) {
          if (!form) return true;
          await api.post('/topics/periods', { ...formData(form), periodId: id });
          toast.success('Đã mở đề tài trong đợt.');
          clearLookups();
          await load();
        },
      });
    }
  });

  await load();
}
