import { html, setHtml, on, $ } from '../core/dom.js';
import { api, paged, unwrap } from '../core/api.js';
import { auth } from '../core/auth.js';
import { toast } from '../core/toast.js';
import { lookup, studentName } from '../core/lookup.js';
import { fmtDate, fmtScore, label } from '../core/format.js';
import { pageHeader, statusBadge, emptyState, skeletonRows, options, confirmDialog } from '../components/ui.js';

export async function render(root) {
  const [periods, me] = await Promise.all([lookup.periods(), auth.isStudent ? lookup.myStudent().catch(() => null) : null]);
  const active = periods.filter((p) => p.status !== 'DA_HUY');
  const state = { periodId: auth.isStudent ? '' : (active.find((p) => ['DANG_BAO_VE', 'DA_KET_THUC'].includes(p.status))?.id ?? active[0]?.id ?? ''), rows: [] };

  setHtml(root, html`
    ${pageHeader({
      title: auth.isStudent ? 'Kết quả đồ án' : 'Bảng điểm tổng kết',
      subtitle: 'Điểm tổng kết = trọng số × (hướng dẫn, phản biện, hội đồng) theo loại đồ án; sinh viên chỉ xem được sau khi công bố',
      actions: auth.isStudent ? '' : html`<select id="period" style="width:300px">${options(active, state.periodId, { placeholder: 'Tất cả đợt' })}</select>`,
    })}
    <div id="content"></div>`);

  async function load() {
    setHtml($('#content', root), skeletonRows(4));
    const projects = paged(await api.get('/projects', { periodId: state.periodId, pageSize: 100 })).items;
    const results = await Promise.all(projects.map((p) => api.get(`/projects/${p.id}/results`, null, { silent: true }).then(unwrap).catch(() => [])));
    const rows = projects.flatMap((p, i) => (results[i] ?? []).map((r) => ({ ...r, project: p })))
      .filter((r) => !auth.isStudent || r.studentId === me?.id);
    const students = await lookup.studentMap(rows.map((r) => r.studentId));
    state.rows = rows;

    if (!rows.length) {
      setHtml($('#content', root), html`<div class="card p-6">${emptyState({
        title: 'Chưa có kết quả',
        description: auth.isStudent ? 'Kết quả sẽ hiển thị sau khi Admin công bố.' : 'Chưa có điểm nào được nhập trong đợt này.',
      })}</div>`);
      return;
    }
    const unpublished = [...new Set(rows.filter((r) => !r.publishedDate).map((r) => r.projectId))];
    setHtml($('#content', root), html`
      ${auth.isAdmin && unpublished.length ? html`<div class="card p-4 mb-4 flex justify-between items-center">
        <span class="text-sm">${unpublished.length} đồ án chưa công bố kết quả.</span></div>` : ''}
      <div class="table-container">
        <table>
          <thead><tr><th>Đồ án</th><th>Sinh viên</th><th>Hướng dẫn</th><th>Phản biện</th><th>Hội đồng</th><th>Tổng kết</th><th>Xếp loại</th><th>Kết quả</th><th>Công bố</th></tr></thead>
          <tbody>${rows.map((r) => html`
            <tr>
              <td><a href="#/projects/${r.projectId}" class="text-mono">${r.project.code}</a></td>
              <td>${studentName(students, r.studentId)}</td>
              <td>${fmtScore(r.advisorScore)}</td><td>${fmtScore(r.reviewerScore)}</td><td>${fmtScore(r.committeeScore)}</td>
              <td><strong>${fmtScore(r.finalScore)}</strong></td>
              <td>${r.finalScore !== null ? label('rank', r.rank) : '—'}</td>
              <td>${statusBadge(r.result)}</td>
              <td>${r.publishedDate ? fmtDate(r.publishedDate)
                : auth.isAdmin ? html`<button class="btn btn-primary btn-xs" data-publish="${r.projectId}">Công bố</button>` : html`<span class="text-muted">Chưa</span>`}</td>
            </tr>`)}</tbody>
        </table>
      </div>`);
  }

  on(root, 'click', '[data-publish]', (e, el) => {
    const project = state.rows.find((r) => r.projectId === Number(el.dataset.publish))?.project;
    confirmDialog({
      title: 'Công bố kết quả', message: `Công bố kết quả đồ án ${project?.code}? Sau khi công bố, điểm bị khóa.`, confirmText: 'Công bố',
      async onConfirm() { await api.post(`/projects/${el.dataset.publish}/publish-result`); toast.success('Đã công bố kết quả.'); await load(); },
    });
  });
  $('#period', root)?.addEventListener('change', (e) => { state.periodId = e.target.value; load(); });

  await load();
}
