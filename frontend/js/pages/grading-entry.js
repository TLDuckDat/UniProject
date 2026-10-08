import { html, setHtml, on, $, $$ } from '../core/dom.js';
import { api, paged, unwrap } from '../core/api.js';
import { toast } from '../core/toast.js';
import { lookup, studentName } from '../core/lookup.js';
import { fmtScore, label } from '../core/format.js';
import { pageHeader, statusBadge, badge, emptyState, skeletonRows } from '../components/ui.js';

const ACTIVE = ['DANG_MO_DANG_KY', 'DANG_THUC_HIEN', 'DANG_BAO_VE', 'DA_KET_THUC'];

export async function render(root, { query }) {
  setHtml(root, html`${pageHeader({ title: 'Chấm điểm đồ án', subtitle: 'Đang xác định các đồ án bạn được chấm...' })}${skeletonRows(3)}`);
  const [me, periods, types] = await Promise.all([lookup.myLecturer(), lookup.periods(), lookup.projectTypes()]);
  const periodIds = periods.filter((p) => ACTIVE.includes(p.status)).map((p) => p.id);

  // Vai trò chấm của tôi trong từng đồ án: hướng dẫn, phản biện, hội đồng
  const perPeriod = await Promise.all(periodIds.map(async (pid) => {
    const [projects, schedules, committees] = await Promise.all([
      api.get('/projects', { periodId: pid, pageSize: 100 }).then((r) => paged(r).items),
      api.get('/defenses/schedules', { periodId: pid }).then((r) => unwrap(r) ?? []),
      api.get('/defenses/committees', { periodId: pid, pageSize: 100 }).then((r) => paged(r).items),
    ]);
    const myCommitteeIds = committees.filter((c) => c.members.some((m) => m.lecturerId === me.id)).map((c) => c.id);
    return Promise.all(projects.map(async (p) => {
      const roles = [];
      if (p.advisors.some((a) => a.isActive && a.lecturerId === me.id)) roles.push('HUONG_DAN');
      const reviewers = unwrap(await api.get(`/defenses/projects/${p.id}/reviewers`, null, { silent: true }).catch(() => null)) ?? [];
      if (reviewers.some((r) => r.lecturerId === me.id)) roles.push('PHAN_BIEN');
      if (schedules.some((s) => s.projectId === p.id && myCommitteeIds.includes(s.committeeId))) roles.push('HOI_DONG');
      return { project: p, roles, period: periods.find((x) => x.id === pid) };
    }));
  }));
  const gradable = perPeriod.flat().filter((x) => x.roles.length);
  const state = { selectedId: Number(query.projectId) || gradable[0]?.project.id, grades: [], students: new Map() };

  function listView() {
    return html`
      <div class="card">
        <div class="card-header"><h3 class="card-title">Đồ án được chấm (${gradable.length})</h3></div>
        <div class="card-body" style="padding:8px">
          ${gradable.map(({ project, roles }) => html`
            <a class="nav-item ${project.id === state.selectedId ? 'active' : ''}" href="javascript:void(0)" data-select="${project.id}" style="flex-direction:column;align-items:flex-start;gap:4px">
              <span><span class="text-mono text-xs">${project.code}</span> · ${project.title}</span>
              <span class="flex gap-1">${roles.map((r) => badge(label('gradingType', r), 'default'))}</span>
            </a>`)}
        </div>
      </div>`;
  }

  function formView() {
    const item = gradable.find((x) => x.project.id === state.selectedId);
    if (!item) return emptyState({ title: 'Chọn một đồ án để chấm điểm' });
    const { project, roles, period } = item;
    const type = types.find((t) => t.kind === period?.projectKind);
    const shared = type?.gradingMode === 'CHAM_CHUNG';
    const mine = (studentId, gt) => state.grades.find((g) => g.lecturerId === me.id && g.gradingType === gt && (shared || g.studentId === studentId));
    const rows = shared ? [{ studentId: null, label: 'Cả nhóm (chấm chung)' }] : project.members.map((m) => ({ studentId: m.studentId, label: studentName(state.students, m.studentId) }));
    return html`
      <div class="card">
        <div class="card-header">
          <div><div class="topic-code">${project.code}</div><h3 class="card-title">${project.title}</h3></div>
          ${statusBadge(project.status)}
        </div>
        <div class="card-body">
          <p class="text-sm text-secondary mb-4">
            ${type?.name ?? ''} · ${shared ? 'Chấm chung: một điểm áp dụng cho mọi thành viên' : 'Chấm riêng từng sinh viên'} ·
            Trọng số HD / PB / HĐ: ${type ? `${type.advisorWeight} / ${type.reviewerWeight} / ${type.committeeWeight}` : '—'}
          </p>
          <form id="grade-form">
            <div class="table-container">
              <table>
                <thead><tr><th>Sinh viên</th>${roles.map((r) => html`<th>${label('gradingType', r)}</th>`)}<th>Nhận xét</th></tr></thead>
                <tbody>${rows.map((row) => html`
                  <tr>
                    <td><strong>${row.label}</strong></td>
                    ${roles.map((r) => {
                      const g = mine(row.studentId, r);
                      return html`<td><input class="score-input" type="number" min="0" max="10" step="0.1" data-student="${row.studentId ?? ''}" data-type="${r}" value="${g ? g.score : ''}" placeholder="0 - 10" /></td>`;
                    })}
                    <td><input type="text" data-comment="${row.studentId ?? ''}" value="${mine(row.studentId, roles[0])?.comments ?? ''}" placeholder="Nhận xét (tùy chọn)" /></td>
                  </tr>`)}</tbody>
              </table>
            </div>
            <div class="flex justify-end mt-4"><button class="btn btn-primary btn-sm" type="submit">Lưu điểm</button></div>
          </form>
          ${state.grades.length ? html`
            <h4 class="section-label mt-6">Điểm đã nhập của đồ án</h4>
            <div class="info-list">${state.grades.map((g) => html`
              <div class="info-row"><span class="info-label">${g.studentId ? studentName(state.students, g.studentId) : 'Cả nhóm'} · ${label('gradingType', g.gradingType)}</span><span class="info-value">${fmtScore(g.score)}</span></div>`)}</div>` : ''}
        </div>
      </div>`;
  }

  async function select(id) {
    state.selectedId = id;
    const item = gradable.find((x) => x.project.id === id);
    if (item) {
      [state.grades, state.students] = await Promise.all([
        api.get(`/projects/${id}/grades`, null, { silent: true }).then(unwrap).catch(() => []),
        lookup.studentMap(item.project.members.map((m) => m.studentId)),
      ]);
    }
    draw();
  }

  function draw() {
    setHtml(root, html`
      ${pageHeader({ title: 'Chấm điểm đồ án', subtitle: 'Bạn chỉ chấm được đúng vai trò của mình: hướng dẫn, phản biện được phân công, hoặc thành viên hội đồng có lịch bảo vệ đồ án' })}
      ${gradable.length === 0 ? html`<div class="card p-6">${emptyState({ title: 'Không có đồ án nào cần chấm', description: 'Bạn chưa được phân công hướng dẫn, phản biện hoặc chưa có lịch bảo vệ trong hội đồng.' })}</div>` : html`
        <div class="detail-grid" style="grid-template-columns:1fr 2fr">
          <div>${listView()}</div>
          <div>${formView()}</div>
        </div>`}`);

    $('#grade-form', root)?.addEventListener('submit', async (e) => {
      e.preventDefault();
      const btn = $('button[type=submit]', e.target);
      btn.disabled = true;
      let saved = 0;
      try {
        for (const input of $$('input[data-type]', e.target)) {
          if (input.value === '') continue;
          const studentId = input.dataset.student ? Number(input.dataset.student) : null;
          const comments = $(`[data-comment="${input.dataset.student}"]`, e.target)?.value.trim() || null;
          await api.post(`/projects/${state.selectedId}/grades`, { studentId, gradingType: input.dataset.type, score: Number(input.value), comments });
          saved++;
        }
        toast.success(saved ? `Đã lưu ${saved} điểm.` : 'Chưa nhập điểm nào.');
        await select(state.selectedId);
      } catch {
        btn.disabled = false;
      }
    });
  }

  on(root, 'click', '[data-select]', (e, el) => select(Number(el.dataset.select)));
  if (state.selectedId) await select(state.selectedId); else draw();
}
