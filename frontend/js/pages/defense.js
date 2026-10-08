import { html, setHtml, on, formData, icons, $ } from '../core/dom.js';
import { api, paged, unwrap } from '../core/api.js';
import { auth } from '../core/auth.js';
import { toast } from '../core/toast.js';
import { lookup, lecturerName } from '../core/lookup.js';
import { fmtDate, fmtTime, toDateInput, label } from '../core/format.js';
import { pageHeader, statusBadge, badge, emptyState, skeletonRows, tabs, options, openModal, confirmDialog } from '../components/ui.js';

const ROLES = ['CHU_TICH', 'THU_KY', 'UY_VIEN'];
const toTimeSpan = (v) => (v && v.length === 5 ? `${v}:00` : v);

export async function render(root) {
  const [periods, lecturersList, me] = await Promise.all([
    lookup.periods(), lookup.lecturers(), auth.isLecturer ? lookup.myLecturer().catch(() => null) : null,
  ]);
  const lecturers = new Map(lecturersList.map((l) => [l.id, l]));
  const active = periods.filter((p) => p.status !== 'DA_HUY');
  const state = {
    tab: auth.isStudent ? 'schedules' : 'committees',
    periodId: active.find((p) => p.status === 'DANG_BAO_VE')?.id ?? active.find((p) => p.status === 'DANG_THUC_HIEN')?.id ?? active[0]?.id ?? '',
    committees: [], schedules: [], projects: [],
  };

  setHtml(root, html`
    ${pageHeader({
      title: auth.isStudent ? 'Lịch bảo vệ' : 'Hội đồng & Lịch bảo vệ',
      subtitle: 'Hội đồng 3 - 5 thành viên gồm đúng một Chủ tịch, một Thư ký; lịch bảo vệ nằm trong khoảng bảo vệ của đợt',
      actions: html`<select id="period" style="width:300px">${options(active, state.periodId, { placeholder: 'Tất cả đợt' })}</select>`,
    })}
    <div id="tabs"></div>
    <div id="content"></div>`);

  async function load() {
    setHtml($('#tabs', root), auth.isStudent ? '' : tabs([{ id: 'committees', label: 'Hội đồng bảo vệ' }, { id: 'schedules', label: 'Lịch bảo vệ' }], state.tab));
    setHtml($('#content', root), skeletonRows(3));
    const [committees, schedules, projects] = await Promise.all([
      api.get('/defenses/committees', { periodId: state.periodId, pageSize: 100 }).then((r) => paged(r).items),
      api.get('/defenses/schedules', { periodId: state.periodId }).then((r) => unwrap(r) ?? []),
      api.get('/projects', { periodId: state.periodId, pageSize: 100 }).then((r) => paged(r).items),
    ]);
    Object.assign(state, { committees, schedules, projects });
    if (auth.isStudent) state.schedules = schedules.filter((s) => projects.some((p) => p.id === s.projectId));
    draw();
  }

  const projectOf = (id) => state.projects.find((p) => p.id === id);
  const committeeOf = (id) => state.committees.find((c) => c.id === id);

  function committeesView() {
    const list = auth.isLecturer && me ? [...state.committees].sort((a, b) => Number(b.members.some((m) => m.lecturerId === me.id)) - Number(a.members.some((m) => m.lecturerId === me.id))) : state.committees;
    return html`
      ${auth.isAdmin ? html`<div class="flex justify-end mb-3"><button class="btn btn-primary btn-sm" data-action="create-committee">${icons.plus} Lập hội đồng</button></div>` : ''}
      ${list.length === 0 ? html`<div class="card p-6">${emptyState({ title: 'Chưa có hội đồng nào', description: 'Admin lập hội đồng cho đợt đồ án, sau đó thêm thành viên và xếp lịch bảo vệ.' })}</div>` : html`
        <div class="committee-grid">
          ${list.map((c) => html`
            <div class="card committee-card">
              <div class="card-header">
                <div><div class="topic-code">${c.code}</div><h3 class="card-title">${c.name}</h3></div>
                ${c.isReadyForDefense ? badge('Đủ điều kiện', 'success') : badge('Chưa đủ thành phần', 'warning')}
              </div>
              <div class="card-body">
                <div class="text-xs text-muted mb-3">${c.location ? `Địa điểm: ${c.location} · ` : ''}${statusBadge(c.status)}</div>
                <div class="member-list">
                  ${c.members.length === 0 ? html`<p class="text-sm text-muted">Chưa có thành viên.</p>` : [...c.members]
                    .sort((a, b) => ROLES.indexOf(a.role) - ROLES.indexOf(b.role))
                    .map((m) => html`
                      <div class="member-row">
                        <span class="member-name" style="flex:1">${lecturerName(lecturers, m.lecturerId)} ${me && m.lecturerId === me.id ? '(bạn)' : ''}</span>
                        ${badge(label('committeeRole', m.role), m.role === 'CHU_TICH' ? 'accent' : m.role === 'THU_KY' ? 'info' : 'default')}
                        ${auth.isAdmin ? html`<button class="btn btn-ghost btn-xs" data-remove-member="${c.id}:${m.id}">Gỡ</button>` : ''}
                      </div>`)}
                </div>
                ${c.compositionErrors?.length ? html`<ul class="committee-errors">${c.compositionErrors.map((e) => html`<li>${e}</li>`)}</ul>` : ''}
                <div class="text-xs text-muted mt-3">${state.schedules.filter((s) => s.committeeId === c.id).length} lịch bảo vệ</div>
              </div>
              ${auth.isAdmin ? html`<div class="period-card-footer"><button class="btn btn-xs" data-add-member="${c.id}">${icons.plus} Thêm thành viên</button></div>` : ''}
            </div>`)}
        </div>`}`;
  }

  function schedulesView() {
    const list = [...state.schedules].sort((a, b) => new Date(a.defenseDate) - new Date(b.defenseDate) || String(a.startTime).localeCompare(String(b.startTime)));
    return html`
      ${auth.isAdmin ? html`<div class="flex justify-end mb-3"><button class="btn btn-primary btn-sm" data-action="create-schedule">${icons.plus} Xếp lịch bảo vệ</button></div>` : ''}
      ${list.length === 0 ? html`<div class="card p-6">${emptyState({ title: 'Chưa có lịch bảo vệ', description: auth.isStudent ? 'Lịch bảo vệ của nhóm bạn sẽ hiển thị khi được xếp.' : 'Chưa có lịch nào trong đợt này.' })}</div>` : html`
        <div class="table-container">
          <table>
            <thead><tr><th>Ngày</th><th>Giờ</th><th>Phòng</th><th>Đồ án</th><th>Hội đồng</th><th>Trạng thái</th>${auth.isAdmin ? html`<th></th>` : ''}</tr></thead>
            <tbody>${list.map((s) => {
              const p = projectOf(s.projectId);
              const c = committeeOf(s.committeeId);
              return html`
                <tr>
                  <td><strong>${fmtDate(s.defenseDate)}</strong></td>
                  <td class="text-mono">${fmtTime(s.startTime)} - ${fmtTime(s.endTime)}</td>
                  <td>${s.room}</td>
                  <td>${p ? html`<a href="#/projects/${p.id}">${p.code}</a><div class="text-xs text-muted">${p.title}</div>` : `Đồ án #${s.projectId}`}</td>
                  <td>${c?.name ?? `Hội đồng #${s.committeeId}`}</td>
                  <td>${statusBadge(s.status)}</td>
                  ${auth.isAdmin ? html`<td class="actions">
                    <button class="btn btn-ghost btn-xs" data-edit-schedule="${s.id}">Sửa</button>
                    <button class="btn btn-danger btn-xs" data-delete-schedule="${s.id}">Xóa</button></td>` : ''}
                </tr>`;
            })}</tbody>
          </table>
        </div>`}`;
  }

  function draw() {
    setHtml($('#content', root), state.tab === 'committees' ? committeesView() : schedulesView());
  }

  function scheduleForm(item) {
    const ready = state.committees.filter((c) => c.isReadyForDefense);
    return html`<form>
      ${item ? '' : html`
        <div class="form-group"><label class="required">Đồ án</label>
          <select name="projectId" data-type="int" required>${options(state.projects.filter((p) => !state.schedules.some((s) => s.projectId === p.id)), '', { placeholder: '-- Chọn đồ án --', text: (p) => `${p.code} - ${p.title}` })}</select></div>
        <div class="form-group"><label class="required">Hội đồng (đủ điều kiện)</label>
          <select name="committeeId" data-type="int" required>${options(ready, '', { placeholder: ready.length ? '-- Chọn hội đồng --' : 'Chưa có hội đồng đủ điều kiện', text: (c) => `${c.code} - ${c.name}` })}</select></div>`}
      <div class="grid-3">
        <div class="form-group"><label class="required">Ngày bảo vệ</label><input type="date" name="defenseDate" value="${toDateInput(item?.defenseDate)}" required /></div>
        <div class="form-group"><label class="required">Bắt đầu</label><input type="time" name="startTime" value="${item ? fmtTime(item.startTime) : '07:30'}" required /></div>
        <div class="form-group"><label class="required">Kết thúc</label><input type="time" name="endTime" value="${item ? fmtTime(item.endTime) : '08:15'}" required /></div>
      </div>
      <div class="form-group"><label class="required">Phòng</label><input name="room" value="${item?.room ?? ''}" required maxlength="50" placeholder="VD: E10-201" /></div>
    </form>`;
  }

  const actions = {
    'create-committee': () => openModal({
      title: 'Lập hội đồng bảo vệ', confirmText: 'Tạo hội đồng',
      body: html`<form>
        <div class="form-group"><label class="required">Đợt đồ án</label><select name="periodId" data-type="int" required>${options(active, state.periodId, { placeholder: '-- Chọn đợt --' })}</select></div>
        <div class="grid-2">
          <div class="form-group"><label class="required">Mã hội đồng</label><input name="code" required maxlength="20" placeholder="VD: HD01" /></div>
          <div class="form-group"><label>Địa điểm</label><input name="location" maxlength="100" placeholder="VD: Nhà E10" /></div>
        </div>
        <div class="form-group"><label class="required">Tên hội đồng</label><input name="name" required maxlength="200" placeholder="VD: Hội đồng 1 - CNTT" /></div>
      </form>`,
      async onConfirm(form) { await api.post('/defenses/committees', formData(form)); toast.success('Tạo hội đồng thành công.'); await load(); },
    }),
    'create-schedule': () => openModal({
      title: 'Xếp lịch bảo vệ', confirmText: 'Xếp lịch', body: scheduleForm(null),
      async onConfirm(form) {
        const d = formData(form);
        await api.post('/defenses/schedules', { ...d, periodId: Number(state.periodId), startTime: toTimeSpan(d.startTime), endTime: toTimeSpan(d.endTime) });
        toast.success('Tạo lịch bảo vệ thành công.');
        await load();
      },
    }),
  };

  on(root, 'click', '[data-tab]', (e, el) => { state.tab = el.dataset.tab; load(); });
  on(root, 'click', '[data-action]', (e, el) => {
    if (!state.periodId && el.dataset.action === 'create-schedule') { toast.warning('Hãy chọn một đợt đồ án trước.'); return; }
    actions[el.dataset.action]?.();
  });
  on(root, 'click', '[data-add-member]', (e, el) => {
    const c = committeeOf(Number(el.dataset.addMember));
    const available = lecturersList.filter((l) => !c.members.some((m) => m.lecturerId === l.id));
    openModal({
      title: `Thêm thành viên · ${c.name}`, confirmText: 'Thêm',
      body: html`<form>
        <div class="form-group"><label class="required">Giảng viên</label><select name="lecturerId" data-type="int" required>${options(available, '', { placeholder: '-- Chọn giảng viên --', text: (l) => `${l.fullName} (${l.degreeName})` })}</select></div>
        <div class="form-group"><label class="required">Vai trò</label><select name="role">${options(ROLES.map((r) => ({ id: r, name: label('committeeRole', r) })), c.members.some((m) => m.role === 'CHU_TICH') ? 'UY_VIEN' : 'CHU_TICH')}</select></div>
      </form>`,
      async onConfirm(form) { await api.post(`/defenses/committees/${c.id}/members`, formData(form)); toast.success('Đã thêm thành viên hội đồng.'); await load(); },
    });
  });
  on(root, 'click', '[data-remove-member]', (e, el) => {
    const [cid, mid] = el.dataset.removeMember.split(':');
    confirmDialog({
      title: 'Gỡ thành viên hội đồng', message: 'Gỡ thành viên này khỏi hội đồng?', confirmText: 'Gỡ', danger: true,
      async onConfirm() { await api.delete(`/defenses/committees/${cid}/members/${mid}`); toast.success('Đã gỡ thành viên.'); await load(); },
    });
  });
  on(root, 'click', '[data-edit-schedule]', (e, el) => {
    const s = state.schedules.find((x) => x.id === Number(el.dataset.editSchedule));
    openModal({
      title: 'Sửa lịch bảo vệ', confirmText: 'Lưu thay đổi', body: scheduleForm(s),
      async onConfirm(form) {
        const d = formData(form);
        await api.put(`/defenses/schedules/${s.id}`, { ...d, startTime: toTimeSpan(d.startTime), endTime: toTimeSpan(d.endTime) });
        toast.success('Cập nhật lịch bảo vệ thành công.');
        await load();
      },
    });
  });
  on(root, 'click', '[data-delete-schedule]', (e, el) => confirmDialog({
    title: 'Xóa lịch bảo vệ', message: 'Xóa lịch bảo vệ này?', confirmText: 'Xóa', danger: true,
    async onConfirm() { await api.delete(`/defenses/schedules/${el.dataset.deleteSchedule}`); toast.success('Đã xóa lịch bảo vệ.'); await load(); },
  }));
  $('#period', root).addEventListener('change', (e) => { state.periodId = e.target.value; load(); });

  await load();
}
