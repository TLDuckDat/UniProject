import { html, setHtml, $ } from '../core/dom.js';
import { api } from '../core/api.js';
import { toast } from '../core/toast.js';
import { pageHeader } from '../components/ui.js';
import { logout } from '../components/shell.js';

export function render(root) {
  setHtml(root, html`
    ${pageHeader({ title: 'Đổi mật khẩu', subtitle: 'Sau khi đổi mật khẩu, bạn cần đăng nhập lại' })}
    <div class="card" style="max-width:480px">
      <div class="card-body">
        <form id="form">
          <div class="form-group"><label class="required">Mật khẩu hiện tại</label><input name="oldPassword" type="password" required autocomplete="current-password" /></div>
          <div class="form-group"><label class="required">Mật khẩu mới</label><input name="newPassword" type="password" required minlength="6" autocomplete="new-password" />
            <span class="form-hint">Tối thiểu 6 ký tự, khác mật khẩu cũ.</span></div>
          <div class="form-group"><label class="required">Nhập lại mật khẩu mới</label><input name="confirm" type="password" required autocomplete="new-password" />
            <span class="form-error hidden" id="mismatch">Mật khẩu nhập lại không khớp.</span></div>
          <button class="btn btn-primary" type="submit">Đổi mật khẩu</button>
        </form>
      </div>
    </div>`);

  $('#form', root).addEventListener('submit', async (e) => {
    e.preventDefault();
    const f = e.target.elements;
    const mismatch = f.newPassword.value !== f.confirm.value;
    $('#mismatch', root).classList.toggle('hidden', !mismatch);
    if (mismatch) return;
    const btn = $('button[type=submit]', e.target);
    btn.disabled = true;
    try {
      await api.post('/auth/change-password', { oldPassword: f.oldPassword.value, newPassword: f.newPassword.value });
      toast.success('Đổi mật khẩu thành công. Vui lòng đăng nhập lại.');
      logout();
    } catch {
      btn.disabled = false;
    }
  });
}
