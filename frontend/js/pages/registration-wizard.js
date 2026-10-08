import { html, setHtml, on, formData, icons, $ } from '../core/dom.js';
import { api, paged, unwrap } from '../core/api.js';
import { toast } from '../core/toast.js';
import { lookup } from '../core/lookup.js';
import { fmtDate, fmtDateTime, label, statusLabel } from '../core/format.js';
import { pageHeader, statusBadge, badge, emptyState, skeletonRows, options, openModal, confirmDialog } from '../components/ui.js';

const STEPS = ['Tạo nhóm', 'Thành viên', 'Nguyện vọng đề tài', 'Gửi duyệt'];

export async function render(root, { query }) {
  setHtml(root, skeletonRows(4));
  const [me, periods] = await Promise.all([lookup.myStudent(), lookup.periods()]);
  const openPeriods = periods.filter((p) => ['DANG_MO_DANG_KY', 'DANG_THUC_HIEN', 'DANG_BAO_VE'].includes(p.status));
  const state = {
    periodId: Number(query.periodId) || openPeriods.find((p) => p.status === 'DANG_MO_DANG_KY')?.id || openPeriods[0]?.id,
    reg: null, students: new Map(), topics: [], changeRequests: [],
  };

  async function load() {
    if (!state.periodId) return draw();
    state.reg = unwrap(await api.get('/registrations/me', { periodId: state.periodId }));
    state.topics = await lookup.periodTopics(state.periodId).catch(() => []);
    state.students = state.reg ? await lookup.studentMap(state.reg.members.map((m) => m.studentId)) : new Map();
    state.changeRequests = (unwrap(await api.get('/registration-change-requests/me', null, { silent: true }).catch(() => null)) ?? [])
      .filter((c) => c.registrationId === state.reg?.id);
    draw();
  }

  const period = () => periods.find((p) => p.id === state.periodId);
  const isLeader = () => state.reg?.members.some((m) => m.studentId === me.id && m.role === 'TRUONG_NHOM');
  const editable = () => state.reg?.status === 'NHAP' && isLeader();
  const topicOf = (ptId) => state.topics.find((t) => t.id === ptId);

  function currentStep() {
    if (!state.reg) return 0;
    if (state.reg.status !== 'NHAP') return 4;
    if (state.reg.preferences.length > 0) return 3;
    return state.reg.members.length > 1 ? 2 : 1;
  }

  function stepsBar() {
    const cur = currentStep();
    return html`<div class="wizard-steps">${STEPS.map((s, i) => html`
      <div class="wizard-step ${i < cur ? 'done' : i === cur ? 'current' : ''}"><span class="num">${i < cur ? '✓' : i + 1}</span>${s}</div>`)}</div>`;
  }

  function createGroup() {
    return html`
      <div class="card">
        <div class="card-header"><h3 class="card-title">Bước 1 · Tạo nhóm đăng ký</h3></div>
        <div class="card-body">
          <p class="text-sm text-secondary mb-4">Người tạo đơn sẽ là trưởng nhóm. Sau khi tạo, bạn thêm thành viên và chọn nguyện vọng đề tài.</p>
          <form id="create-form" class="flex gap-3 items-center flex-wrap">
            <input name="groupName" placeholder="Tên nhóm (tùy chọn), VD: Nhóm STO" style="max-width:360px" maxlength="100" />
            <button class="btn btn-primary btn-sm" type="submit">Tạo nhóm</button>
          </form>
        </div>
      </div>`;
  }

  function membersCard() {
    const r = state.reg;
    return html`
      <div class="card">
        <div class="card-header">
          <h3 class="card-title">Thành viên nhóm (${r.members.length})</h3>
          ${editable() ? html`<button class="btn btn-sm" data-action="add-member">${icons.plus} Thêm thành viên</button>` : ''}
        </div>
        <div class="card-body member-list">
          ${r.members.map((m) => {
            const s = state.students.get(m.studentId);
            return html`
              <div class="member-row">
                <div class="user-avatar">${(s?.fullName || '?').split(' ').pop().charAt(0)}</div>
                <div class="flex-col" style="flex:1">
                  <span class="member-name">${s?.fullName ?? `SV #${m.studentId}`} ${m.studentId === me.id ? '(bạn)' : ''}</span>
                  <span class="member-sub">${s?.code ?? ''} · ${s?.className ?? ''} · tham gia ${fmtDate(m.joinedDate)}</span>
                </div>
                ${badge(label('memberRole', m.role), m.role === 'TRUONG_NHOM' ? 'accent' : 'default')}
                ${editable() && m.role !== 'TRUONG_NHOM' ? html`<button class="btn btn-ghost btn-xs" data-remove-member="${m.studentId}">Gỡ</button>` : ''}
              </div>`;
          })}
        </div>
      </div>`;
  }

  function preferencesCard() {
    const r = state.reg;
    const prefs = [...r.preferences].sort((a, b) => a.priorityOrder - b.priorityOrder);
    return html`
      <div class="card">
        <div class="card-header">
          <h3 class="card-title">Nguyện vọng đề tài (${prefs.length})</h3>
          ${editable() ? html`<button class="btn btn-sm" data-action="add-pref">${icons.plus} Thêm nguyện vọng</button>` : ''}
        </div>
        <div class="card-body">
          ${prefs.length === 0 ? html`<p class="text-sm text-muted">Chưa chọn nguyện vọng nào. Nguyện vọng 1 là ưu tiên cao nhất.</p>` : prefs.map((p, i) => {
            const t = topicOf(p.periodTopicId);
            return html`
              <div class="pref-item">
                <div class="pref-order">${p.priorityOrder}</div>
                <div style="flex:1">
                  <div class="member-name">${t?.topicTitle ?? `Đề tài #${p.periodTopicId}`}</div>
                  <div class="member-sub">${t?.topicCode ?? ''} · tối đa ${t?.maxStudents ?? '?'} SV
                    ${p.rejectReason ? html` · <span class="text-danger">Lý do từ chối: ${p.rejectReason}</span>` : ''}</div>
                </div>
                ${statusBadge(p.status)}
                ${editable() ? html`
                  <button class="btn btn-ghost btn-xs" data-move="${p.id}" data-to="${p.priorityOrder - 1}" ${i === 0 ? 'disabled' : ''}>↑</button>
                  <button class="btn btn-ghost btn-xs" data-move="${p.id}" data-to="${p.priorityOrder + 1}" ${i === prefs.length - 1 ? 'disabled' : ''}>↓</button>` : ''}
              </div>`;
          })}
        </div>
      </div>`;
  }

  function changeRequestsCard() {
    const r = state.reg;
    if (r.status !== 'DA_DUYET') return '';
    return html`
      <div class="card">
        <div class="card-header">
          <h3 class="card-title">Yêu cầu đổi đề tài / đổi nhóm</h3>
          <button class="btn btn-sm" data-action="change-request">Gửi yêu cầu</button>
        </div>
        <div class="card-body">
          ${state.changeRequests.length === 0 ? html`<p class="text-sm text-muted">Chưa có yêu cầu nào. Trưởng nhóm có thể xin đổi đề tài; thành viên có thể xin chuyển sang nhóm khác. Admin sẽ duyệt.</p>`
            : html`<div class="info-list">${state.changeRequests.map((c) => html`
              <div class="info-row">
                <span><strong>${label('changeType', c.type)}</strong> · ${fmtDateTime(c.requestedDate)}<div class="text-xs text-muted">${c.reason}${c.processNote ? ` · Phản hồi: ${c.processNote}` : ''}</div></span>
                <span class="info-value">${statusBadge(c.status)}</span>
              </div>`)}</div>`}
        </div>
      </div>`;
  }

  function draw() {
    const p = period();
    const header = pageHeader({
      title: 'Đăng ký đề tài nhóm',
      subtitle: 'Tạo nhóm, mời thành viên, chọn nguyện vọng đề tài và gửi giảng viên duyệt',
      actions: openPeriods.length > 1 ? html`<select id="period" style="width:280px">${options(openPeriods, state.periodId)}</select>` : '',
    });
    if (!p) {
      setHtml(root, html`${header}<div class="card p-6">${emptyState({ title: 'Chưa có đợt đồ án nào đang mở', description: 'Khi Admin mở đăng ký cho đợt có lớp của bạn tham gia, bạn có thể đăng ký tại đây.' })}</div>`);
      return;
    }
    const r = state.reg;
    setHtml(root, html`
      ${header}
      <div class="period-banner mb-4">
        <div>
          <span class="eyebrow">${label('projectKind', p.projectKind)}</span>
          <h2 class="period-name">${p.name}</h2>
          <div class="period-dates">Hạn đăng ký: ${fmtDate(p.registrationDeadline)} · ${statusLabel(p.status)}</div>
        </div>
        ${r ? html`<div class="flex gap-2 items-center">${statusBadge(r.status)}
          ${['NHAP', 'CHO_DUYET'].includes(r.status) && isLeader() ? html`<button class="btn btn-danger btn-sm" data-action="cancel">Hủy đơn</button>` : ''}
          ${editable() ? html`<button class="btn btn-primary btn-sm" data-action="submit" ${r.preferences.length ? '' : 'disabled'}>Gửi duyệt</button>` : ''}</div>` : ''}
      </div>
      ${stepsBar()}
      ${!r ? createGroup() : html`
        ${r.status === 'NHAP' && !isLeader() ? html`<div class="card p-4 mb-4 text-sm text-secondary">Chỉ trưởng nhóm được sửa đơn đăng ký.</div>` : ''}
        <div class="detail-grid">
          <div class="stack">${preferencesCard()}${changeRequestsCard()}</div>
          <div class="stack">${membersCard()}</div>
        </div>`}`);
    $('#period', root)?.addEventListener('change', (e) => { state.periodId = Number(e.target.value); load(); });
    $('#create-form', root)?.addEventListener('submit', async (e) => {
      e.preventDefault();
      await api.post('/registrations', { periodId: state.periodId, groupName: e.target.elements.groupName.value.trim() || null });
      toast.success('Tạo đơn đăng ký nhóm thành công.');
      await load();
    });
  }

  function addMember() {
    const modal = openModal({
      title: 'Thêm thành viên', confirmText: 'Thêm vào nhóm',
      body: html`<form>
        <div class="form-group"><label class="required">Tìm sinh viên theo MSSV hoặc họ tên</label>
          <div class="flex gap-2"><input name="q" placeholder="Nhập MSSV" /><button type="button" class="btn btn-sm" data-search>Tìm</button></div>
        </div>
        <div class="form-group"><label class="required">Chọn sinh viên</label><select name="studentId" required><option value="">-- Nhập từ khóa rồi bấm Tìm --</option></select></div>
      </form>`,
      async onConfirm(form) {
        await api.post(`/registrations/${state.reg.id}/members`, { studentId: Number(form.elements.studentId.value) });
        toast.success('Đã thêm thành viên.');
        await load();
      },
    });
    const form = $('form', modal.el);
    const search = async () => {
      const q = form.elements.q.value.trim();
      if (!q) return;
      const found = paged(await api.get('/students', { search: q, pageSize: 20 })).items.filter((s) => s.id !== me.id);
      setHtml(form.elements.studentId, options(found, '', { placeholder: found.length ? '-- Chọn sinh viên --' : 'Không tìm thấy', text: (s) => `${s.code} - ${s.fullName} (${s.className})` }));
    };
    $('[data-search]', form).addEventListener('click', search);
    form.elements.q.addEventListener('keydown', (e) => { if (e.key === 'Enter') { e.preventDefault(); search(); } });
  }

  function addPreference() {
    const chosen = state.reg.preferences.map((p) => p.periodTopicId);
    const available = state.topics.filter((t) => t.status === 'DANG_MO' && !chosen.includes(t.id));
    openModal({
      title: 'Thêm nguyện vọng đề tài', confirmText: 'Thêm nguyện vọng',
      body: available.length ? html`<form>
        <div class="form-group"><label class="required">Đề tài đang mở trong đợt</label>
          <select name="periodTopicId" data-type="int" required>${options(available, '', { placeholder: '-- Chọn đề tài --', text: (t) => `${t.topicCode} - ${t.topicTitle} (tối đa ${t.maxStudents} SV)` })}</select></div>
        <div class="form-group"><label class="required">Thứ tự ưu tiên</label><input name="priorityOrder" type="number" min="1" value="${state.reg.preferences.length + 1}" required /></div>
      </form>` : emptyState({ title: 'Không còn đề tài để chọn', description: 'Tất cả đề tài đang mở đã có trong nguyện vọng, hoặc đợt chưa mở đề tài nào.' }),
      async onConfirm(form) {
        if (!form) return true;
        await api.post(`/registrations/${state.reg.id}/preferences`, formData(form));
        toast.success('Đã thêm nguyện vọng.');
        await load();
      },
    });
  }

  function changeRequest() {
    const leader = isLeader();
    const available = state.topics.filter((t) => t.status === 'DANG_MO');
    openModal({
      title: 'Gửi yêu cầu thay đổi', confirmText: 'Gửi yêu cầu',
      body: html`<form>
        <div class="form-group"><label class="required">Loại yêu cầu</label>
          <select name="type" required>
            ${leader ? html`<option value="DOI_DE_TAI">Đổi đề tài (trưởng nhóm)</option>` : ''}
            <option value="DOI_NHOM">Đổi nhóm (chuyển sang nhóm khác)</option>
          </select></div>
        <div class="form-group" data-for="DOI_DE_TAI"><label>Đề tài mới</label>
          <select name="newPeriodTopicId" data-type="int">${options(available, '', { placeholder: '-- Chọn đề tài --', text: (t) => `${t.topicCode} - ${t.topicTitle}` })}</select></div>
        <div class="form-group" data-for="DOI_NHOM"><label>Mã đơn đăng ký của nhóm muốn chuyển sang</label>
          <input name="targetRegistrationId" type="number" min="1" placeholder="Hỏi trưởng nhóm bên kia mã đơn (số)" /></div>
        <div class="form-group"><label class="required">Lý do</label><textarea name="reason" required></textarea></div>
      </form>`,
      onOpen(dialog) {
        const sync = () => {
          const type = $('[name=type]', dialog).value;
          dialog.querySelectorAll('[data-for]').forEach((el) => el.classList.toggle('hidden', el.dataset.for !== type));
        };
        $('[name=type]', dialog).addEventListener('change', sync);
        sync();
      },
      async onConfirm(form) {
        const d = formData(form);
        if (d.type === 'DOI_DE_TAI') d.targetRegistrationId = null; else d.newPeriodTopicId = null;
        await api.post('/registration-change-requests', { ...d, registrationId: state.reg.id });
        toast.success('Đã gửi yêu cầu. Chờ Admin duyệt.');
        await load();
      },
    });
  }

  on(root, 'click', '[data-action]', (e, el) => {
    const a = el.dataset.action;
    if (a === 'add-member') addMember();
    else if (a === 'add-pref') addPreference();
    else if (a === 'change-request') changeRequest();
    else if (a === 'submit') {
      confirmDialog({
        title: 'Gửi đơn đăng ký', message: 'Sau khi gửi, nhóm không sửa được thành viên và nguyện vọng nữa. Tiếp tục?', confirmText: 'Gửi duyệt',
        async onConfirm() { await api.post(`/registrations/${state.reg.id}/submit`); toast.success('Đã gửi đơn đăng ký. Chờ giảng viên duyệt.'); await load(); },
      });
    } else if (a === 'cancel') {
      confirmDialog({
        title: 'Hủy đơn đăng ký', message: 'Đơn đăng ký của cả nhóm sẽ bị hủy.', confirmText: 'Hủy đơn', danger: true,
        async onConfirm() { await api.post(`/registrations/${state.reg.id}/cancel`); toast.success('Đã hủy đơn đăng ký.'); await load(); },
      });
    }
  });
  on(root, 'click', '[data-remove-member]', async (e, el) => {
    await api.delete(`/registrations/${state.reg.id}/members/${el.dataset.removeMember}`);
    toast.success('Đã gỡ thành viên.');
    await load();
  });
  on(root, 'click', '[data-move]', async (e, el) => {
    await api.put(`/registrations/${state.reg.id}/preferences/order`, { preferenceId: Number(el.dataset.move), newPriorityOrder: Number(el.dataset.to) });
    await load();
  });

  await load();
}

