import { html, setHtml, on, formData, icons, $ } from '../core/dom.js';
import { api, paged } from '../core/api.js';
import { auth } from '../core/auth.js';
import { toast } from '../core/toast.js';
import { lookup } from '../core/lookup.js';
import { fmtDateTime, label } from '../core/format.js';
import { pageHeader, emptyState, skeletonRows, tabs, pagination, badge, options, openModal } from '../components/ui.js';

export async function render(root) {
  const state = { tab: 'all', page: 1, data: null };
  const canSend = auth.isAdmin || auth.isLecturer;

  setHtml(root, html`
    ${pageHeader({
      title: 'Thông báo',
      subtitle: 'Thông báo từ khoa, giảng viên và hệ thống',
      actions: html`
        <button class="btn btn-sm" data-action="mark-all">Đánh dấu tất cả đã đọc</button>
        ${canSend ? html`<button class="btn btn-primary btn-sm" data-action="send">${icons.plus} Gửi thông báo</button>` : ''}`,
    })}
    <div id="tabs"></div>
    <div id="list"></div>`);

  async function load() {
    setHtml($('#tabs', root), tabs([{ id: 'all', label: 'Tất cả' }, { id: 'unread', label: 'Chưa đọc' }, { id: 'read', label: 'Đã đọc' }], state.tab));
    setHtml($('#list', root), skeletonRows(4));
    const isRead = state.tab === 'all' ? '' : state.tab === 'read';
    state.data = paged(await api.get('/notifications/my', { pageNumber: state.page, pageSize: 20, isRead }));
    const d = state.data;
    setHtml($('#list', root), d.items.length === 0
      ? html`<div class="card p-6">${emptyState({ title: 'Không có thông báo', description: 'Bạn chưa nhận được thông báo nào trong mục này.' })}</div>`
      : html`
        <div class="card">
          <div class="notification-list">${d.items.map((n) => html`
            <div class="notification-item ${n.isRead ? '' : 'unread'}" data-id="${n.id}">
              <div class="item-status">${n.isRead ? '' : html`<span class="dot"></span>`}</div>
              <div class="item-content">
                <div class="item-title">${n.title}</div>
                <div class="item-text">${n.content}</div>
                <div class="item-meta">
                  <span class="item-time">${fmtDateTime(n.createdDate)}</span>
                  ${n.notificationType ? badge(n.notificationType, 'default') : ''}
                  ${n.priority !== 'BINH_THUONG' ? html`<span class="item-priority ${n.priority === 'KHAN_CAP' ? 'priority-high' : ''}">${label('priority', n.priority)}</span>` : ''}
                </div>
              </div>
            </div>`)}
          </div>
          ${pagination(d)}
        </div>`);
  }

  async function send() {
    const [classes, lecturers] = await Promise.all([lookup.classes(), lookup.lecturers()]);
    const sorted = [...classes].sort((a, b) => b.admissionYear - a.admissionYear || a.code.localeCompare(b.code));
    openModal({
      title: 'Gửi thông báo', maxWidth: '620px', confirmText: 'Gửi',
      body: html`<form>
        <div class="form-group"><label class="required">Tiêu đề</label><input name="title" required maxlength="200" /></div>
        <div class="form-group"><label class="required">Nội dung</label><textarea name="content" required rows="4"></textarea></div>
        <div class="grid-2">
          <div class="form-group"><label>Phân loại</label><input name="notificationType" maxlength="50" placeholder="VD: HOC_VU, DO_AN" /></div>
          <div class="form-group"><label>Mức độ</label><select name="priority">${options(Object.entries({ BINH_THUONG: 0, QUAN_TRONG: 0, KHAN_CAP: 0 }).map(([k]) => ({ id: k, name: label('priority', k) })), 'BINH_THUONG')}</select></div>
        </div>
        <div class="form-group"><label class="required">Gửi tới</label>
          <select name="target" required>
            <option value="">-- Chọn người nhận --</option>
            <option value="lecturers">Tất cả giảng viên (${lecturers.length})</option>
            ${sorted.map((c) => html`<option value="class:${c.id}">Lớp ${c.code} - ${c.name}</option>`)}
          </select>
        </div>
      </form>`,
      async onConfirm(form) {
        const d = formData(form);
        let recipientAccountIds;
        if (d.target === 'lecturers') recipientAccountIds = lecturers.map((l) => l.accountId);
        else {
          const classId = d.target.split(':')[1];
          const first = paged(await api.get('/students', { classId, pageSize: 100 }));
          const rest = await Promise.all(Array.from({ length: Math.max(0, first.totalPages - 1) }, (_, i) =>
            api.get('/students', { classId, pageSize: 100, pageNumber: i + 2 }).then((r) => paged(r).items)));
          recipientAccountIds = [...first.items, ...rest.flat()].map((s) => s.accountId);
        }
        if (!recipientAccountIds.length) { toast.warning('Nhóm người nhận đang trống.'); return false; }
        await api.post('/notifications', {
          title: d.title, content: d.content, notificationType: d.notificationType, priority: d.priority, recipientAccountIds,
        });
        toast.success(`Đã gửi thông báo tới ${recipientAccountIds.length} người.`);
      },
    });
  }

  on(root, 'click', '[data-tab]', (e, el) => { state.tab = el.dataset.tab; state.page = 1; load(); });
  on(root, 'click', '[data-page]', (e, el) => { state.page = Number(el.dataset.page); load(); });
  on(root, 'click', '.notification-item', async (e, el) => {
    const n = state.data.items.find((x) => x.id === Number(el.dataset.id));
    if (n && !n.isRead) {
      await api.patch(`/notifications/${n.id}/read`);
      await load();
    }
  });
  on(root, 'click', '[data-action]', async (e, el) => {
    if (el.dataset.action === 'send') send();
    else if (el.dataset.action === 'mark-all') {
      await api.patch('/notifications/mark-all-read');
      toast.success('Đã đánh dấu tất cả thông báo là đã đọc');
      await load();
    }
  });

  await load();
}
