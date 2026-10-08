import { html, setHtml, on, $ } from '../core/dom.js';
import { api, paged } from '../core/api.js';
import { auth } from '../core/auth.js';
import { toast } from '../core/toast.js';
import { lookup, lecturerName } from '../core/lookup.js';
import { fmtDate, label } from '../core/format.js';
import { pageHeader, statusBadge, emptyState, skeletonRows, tabs, openModal, promptDialog, confirmDialog } from '../components/ui.js';
import { topicDetailBody } from './topics.js';

export async function render(root) {
  const [lecturers, majors, me] = await Promise.all([
    lookup.lecturerMap(), lookup.majors(), auth.isLecturer ? lookup.myLecturer().catch(() => null) : null,
  ]);
  const state = { tab: 'CHO_DUYET', items: [] };

  setHtml(root, html`
    ${pageHeader({
      title: 'Duyệt đề tài',
      subtitle: auth.isLecturer
        ? 'Giảng viên duyệt đề tài của giảng viên khác, và đề tài sinh viên đề xuất mà mình là giảng viên dự kiến hướng dẫn'
        : 'Phê duyệt, từ chối hoặc ngừng sử dụng đề tài',
    })}
    <div id="tabs"></div>
    <div id="list"></div>`);

  // Giảng viên không duyệt được đề tài chính mình đề xuất; với đề tài SV đề xuất thì ngược lại
  const canReview = (t) => auth.isAdmin || (me && (t.proposedByStudentId ? t.proposedByLecturerId === me.id : t.proposedByLecturerId !== me.id));

  async function load() {
    setHtml($('#tabs', root), tabs([
      { id: 'CHO_DUYET', label: 'Chờ duyệt' },
      { id: 'DA_DUYET', label: 'Đã duyệt' },
      { id: 'TU_CHOI', label: 'Đã từ chối' },
    ], state.tab));
    setHtml($('#list', root), skeletonRows(3));
    state.items = paged(await api.get('/topics', { status: state.tab, pageSize: 100 })).items;
    const items = state.tab === 'CHO_DUYET' ? state.items.filter(canReview) : state.items;
    setHtml($('#list', root), items.length === 0
      ? html`<div class="card p-6">${emptyState({ title: 'Không có đề tài nào', description: state.tab === 'CHO_DUYET' ? 'Không có đề tài nào đang chờ bạn duyệt.' : 'Danh sách trống.' })}</div>`
      : html`
        <div class="table-container">
          <table>
            <thead><tr><th>Mã</th><th>Tên đề tài</th><th>Loại</th><th>Người đề xuất</th><th>Ngày tạo</th><th>Trạng thái</th><th></th></tr></thead>
            <tbody>${items.map((t) => html`
              <tr>
                <td class="text-mono">${t.code}</td>
                <td><strong>${t.title}</strong></td>
                <td>${label('projectKind', t.projectKind)}</td>
                <td>${t.proposedByStudentId ? html`Sinh viên<div class="text-xs text-muted">GV dự kiến: ${lecturerName(lecturers, t.proposedByLecturerId)}</div>` : lecturerName(lecturers, t.proposedByLecturerId)}</td>
                <td>${fmtDate(t.createdAt)}</td>
                <td>${statusBadge(t.status)}</td>
                <td class="actions">
                  <button class="btn btn-ghost btn-xs" data-detail="${t.id}">Chi tiết</button>
                  ${t.status === 'CHO_DUYET' ? html`
                    <button class="btn btn-accent btn-xs" data-approve="${t.id}">Duyệt</button>
                    <button class="btn btn-danger btn-xs" data-reject="${t.id}">Từ chối</button>` : ''}
                  ${auth.isAdmin && t.status === 'DA_DUYET' ? html`<button class="btn btn-danger btn-xs" data-disable="${t.id}">Ngừng sử dụng</button>` : ''}
                </td>
              </tr>`)}
            </tbody>
          </table>
        </div>`);
  }

  const find = (id) => state.items.find((x) => x.id === Number(id));

  on(root, 'click', '[data-tab]', (e, el) => { state.tab = el.dataset.tab; load(); });
  on(root, 'click', '[data-detail]', (e, el) => {
    const t = find(el.dataset.detail);
    openModal({ title: t.title, maxWidth: '680px', hasFooter: false, body: topicDetailBody(t, lecturers, majors) });
  });
  on(root, 'click', '[data-approve]', (e, el) => {
    const t = find(el.dataset.approve);
    confirmDialog({
      title: 'Duyệt đề tài', message: `Duyệt đề tài "${t.title}"?`, confirmText: 'Duyệt',
      async onConfirm() { await api.post(`/topics/${t.id}/approve`); toast.success('Đã duyệt đề tài.'); await load(); },
    });
  });
  on(root, 'click', '[data-reject]', (e, el) => {
    const t = find(el.dataset.reject);
    promptDialog({
      title: 'Từ chối đề tài', label: 'Lý do từ chối', confirmText: 'Từ chối', danger: true,
      async onConfirm(reason) { await api.post(`/topics/${t.id}/reject`, { reason }); toast.success('Đã từ chối đề tài.'); await load(); },
    });
  });
  on(root, 'click', '[data-disable]', (e, el) => {
    const t = find(el.dataset.disable);
    confirmDialog({
      title: 'Ngừng sử dụng đề tài', message: `Đề tài "${t.title}" sẽ không thể mở trong các đợt mới.`, confirmText: 'Ngừng sử dụng', danger: true,
      async onConfirm() { await api.post(`/topics/${t.id}/disable`); toast.success('Đã ngừng sử dụng đề tài.'); await load(); },
    });
  });

  await load();
}
