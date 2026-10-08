import { html, setHtml, on, formData, $ } from '../core/dom.js';
import { api, paged } from '../core/api.js';
import { auth } from '../core/auth.js';
import { toast } from '../core/toast.js';
import { lookup, lecturerName } from '../core/lookup.js';
import { fmtDate, label } from '../core/format.js';
import { pageHeader, statusBadge, emptyState, options, openModal } from '../components/ui.js';
import { topicDetailBody } from './topics.js';

/** Form đề tài dùng chung cho tạo mới / cập nhật. Khi cập nhật không đổi được mã và loại đồ án. */
export async function topicFormBody(item, { forStudent = false, forAdmin = false } = {}) {
  const [majors, types, lecturers] = await Promise.all([lookup.majors(), lookup.projectTypes(), lookup.lecturers()]);
  return html`
    <form>
      <div class="grid-2">
        <div class="form-group"><label class="required">Mã đề tài</label><input name="code" value="${item?.code ?? ''}" ${item ? 'disabled' : 'required'} maxlength="20" placeholder="VD: DT-CNTT-001" /></div>
        <div class="form-group"><label class="required">Loại đồ án</label>
          <select name="projectKind" ${item ? 'disabled' : 'required'}>${options(types, item?.projectKind, { value: 'kind', text: 'name', placeholder: '-- Chọn loại --' })}</select></div>
      </div>
      <div class="form-group"><label class="required">Tên đề tài</label><input name="title" value="${item?.title ?? ''}" required maxlength="300" /></div>
      <div class="form-group"><label class="required">Mô tả</label><textarea name="description" required rows="4">${item?.description ?? ''}</textarea></div>
      <div class="form-group"><label>Mục tiêu / sản phẩm</label><textarea name="objectives" rows="3">${item?.objectives ?? ''}</textarea></div>
      <div class="form-group"><label>Yêu cầu kiến thức & kỹ năng</label><textarea name="requirements" rows="3">${item?.requirements ?? ''}</textarea></div>
      <div class="form-group"><label>Công nghệ dự kiến</label><input name="expectedTechnologies" value="${item?.expectedTechnologies ?? ''}" placeholder="Phân tách bằng dấu phẩy, VD: ASP.NET Core, SQL Server" /></div>
      <div class="grid-2">
        <div class="form-group"><label class="required">Ngành</label>
          <select name="majorId" data-type="int" required>${options(majors, item?.majorId, { placeholder: '-- Chọn ngành --' })}</select></div>
        <div class="form-group"><label class="required">Số SV tối đa</label><input name="maxStudents" type="number" min="1" max="10" value="${item?.maxStudents ?? 2}" required /></div>
      </div>
      ${forStudent ? html`
        <div class="form-group"><label class="required">Giảng viên dự kiến hướng dẫn</label>
          <select name="expectedAdvisorLecturerId" data-type="int" required>${options(lecturers, '', { placeholder: '-- Chọn giảng viên --', text: (l) => `${l.fullName} (${l.degreeName})` })}</select>
          <span class="form-hint">Đề tài sẽ do chính giảng viên này (hoặc Admin) duyệt.</span></div>` : ''}
      ${forAdmin ? html`
        <div class="form-group"><label>Giảng viên đứng tên</label>
          <select name="proposedByLecturerId" data-type="int">${options(lecturers, '', { placeholder: '-- Chọn giảng viên --', text: (l) => `${l.fullName} (${l.degreeName})` })}</select></div>` : ''}
    </form>`;
}

export async function render(root) {
  const [lecturers, majors, me] = await Promise.all([
    lookup.lecturerMap(), lookup.majors(),
    auth.isLecturer ? lookup.myLecturer().catch(() => null) : auth.isStudent ? lookup.myStudent().catch(() => null) : null,
  ]);
  const state = { items: [] };

  setHtml(root, html`
    ${pageHeader({
      title: 'Đề xuất đề tài',
      subtitle: auth.isStudent
        ? 'Sinh viên tự đề xuất đề tài kèm giảng viên dự kiến hướng dẫn; đề tài cần được duyệt trước khi mở trong đợt'
        : 'Đề xuất đề tài mới và theo dõi trạng thái duyệt của các đề tài bạn đứng tên',
      actions: html`<button class="btn btn-primary btn-sm" data-action="create">+ Đề xuất đề tài</button>`,
    })}
    <div id="list"></div>`);

  async function load() {
    let items = [];
    if (auth.isLecturer && me) {
      items = paged(await api.get('/topics', { lecturerId: me.id, pageSize: 100 })).items;
    } else if (auth.isStudent && me) {
      // Không có API lọc theo SV đề xuất: lấy danh sách và lọc phía client
      const all = await Promise.all(['CHO_DUYET', 'DA_DUYET', 'TU_CHOI'].map((s) => api.get('/topics', { status: s, pageSize: 100 }).then((r) => paged(r).items)));
      items = all.flat().filter((t) => t.proposedByStudentId === me.id);
    } else if (auth.isAdmin) {
      items = paged(await api.get('/topics', { status: 'CHO_DUYET', pageSize: 100 })).items;
    }
    state.items = items;
    setHtml($('#list', root), items.length === 0
      ? html`<div class="card p-6">${emptyState({ title: 'Chưa có đề tài nào', description: 'Bấm "Đề xuất đề tài" để gửi đề tài đầu tiên.', actionText: 'Đề xuất đề tài', action: 'create' })}</div>`
      : html`
        <div class="table-container">
          <table>
            <thead><tr><th>Mã</th><th>Tên đề tài</th><th>Loại</th><th>${auth.isStudent ? 'GV dự kiến' : 'Giảng viên'}</th><th>Ngày tạo</th><th>Trạng thái</th><th></th></tr></thead>
            <tbody>${items.map((t) => html`
              <tr>
                <td class="text-mono">${t.code}</td>
                <td><strong>${t.title}</strong>${t.rejectReason ? html`<div class="text-xs text-danger mt-1">Lý do từ chối: ${t.rejectReason}</div>` : ''}</td>
                <td>${label('projectKind', t.projectKind)}</td>
                <td>${lecturerName(lecturers, t.proposedByLecturerId)}</td>
                <td>${fmtDate(t.createdAt)}</td>
                <td>${statusBadge(t.status)}</td>
                <td class="actions"><button class="btn btn-ghost btn-xs" data-detail="${t.id}">Chi tiết</button></td>
              </tr>`)}
            </tbody>
          </table>
        </div>`);
  }

  async function openCreate() {
    openModal({
      title: auth.isStudent ? 'Sinh viên đề xuất đề tài' : 'Đề xuất đề tài mới',
      maxWidth: '680px',
      confirmText: 'Gửi đề xuất',
      body: await topicFormBody(null, { forStudent: auth.isStudent, forAdmin: auth.isAdmin }),
      async onConfirm(form) {
        const d = formData(form);
        if (auth.isStudent) await api.post('/topics/student-proposals', d);
        else await api.post('/topics', d);
        toast.success('Đề xuất đề tài thành công. Đề tài đang chờ duyệt.');
        await load();
      },
    });
  }

  on(root, 'click', '[data-action="create"]', openCreate);
  on(root, 'click', '[data-detail]', (e, el) => {
    const t = state.items.find((x) => x.id === Number(el.dataset.detail));
    openModal({ title: t.title, maxWidth: '680px', hasFooter: false, body: topicDetailBody(t, lecturers, majors) });
  });

  await load();
}
