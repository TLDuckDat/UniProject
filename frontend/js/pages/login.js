import { html, setHtml, on, $ } from '../core/dom.js';
import { api, unwrap } from '../core/api.js';
import { auth } from '../core/auth.js';
import { toast } from '../core/toast.js';
import { navigate } from '../core/router.js';
import { APP_NAME, APP_LOGO } from '../config.js';
import { openModal } from '../components/ui.js';

const CAMPUS_IMAGE = 'assets/khu-giang-duong-17513589597-17865039932.webp';
const SCHOOL_LOGO = 'assets/logo-truong-192.png';

export function render(root, { query }) {
  setHtml(root, html`
    <div class="login-split">
      <aside class="login-visual" style="background-image:url('${CAMPUS_IMAGE}')">
        <div class="login-visual-overlay">
          <div class="login-visual-school">
            <img class="login-school-logo" src="${SCHOOL_LOGO}" alt="Logo Trường Đại học Giao thông Vận tải" />
            <div class="login-school-name">
              <span>Trường Đại học Giao thông Vận tải</span>
              <span>Phân hiệu tại TP. Hồ Chí Minh</span>
            </div>
          </div>
          <div class="login-visual-copy">
            <h2>Hệ thống quản lý đồ án</h2>
            <p>Đồ án cơ sở, chuyên ngành, môn học và tốt nghiệp của Bộ môn Công nghệ Thông tin</p>
          </div>
        </div>
      </aside>

      <main class="login-panel">
        <div class="login-container">
          <div class="brand-header">
            <img class="brand-logo" src="${APP_LOGO}" alt="${APP_NAME}" />
            <div class="brand-name">${APP_NAME}</div>
          </div>

          <div class="login-header">
            <h1 class="login-title">Đăng nhập</h1>
            <p class="login-subtitle">Đăng nhập để tiếp tục vào hệ thống quản lý đồ án</p>
          </div>

          <form class="login-form" novalidate>
            <div class="form-group">
              <label for="username">Tên đăng nhập</label>
              <input id="username" name="username" type="text" class="form-input" placeholder="Mã số sinh viên hoặc tài khoản giảng viên" autocomplete="username" />
              <div class="form-error hidden" data-error="username">Vui lòng nhập tên đăng nhập.</div>
            </div>
            <div class="form-group">
              <div class="login-label-row">
                <label for="password">Mật khẩu</label>
                <button type="button" class="login-link" data-action="forgot-password">Quên mật khẩu?</button>
              </div>
              <input id="password" name="password" type="password" class="form-input" placeholder="Nhập mật khẩu" autocomplete="current-password" />
              <div class="form-error hidden" data-error="password">Vui lòng nhập mật khẩu.</div>
            </div>
            <button type="submit" class="btn btn-primary btn-lg w-full" id="login-submit">Đăng nhập</button>
          </form>

          <div class="login-footer">
            <p>Bộ môn Công nghệ Thông tin - Phân hiệu Trường ĐH GTVT tại TP.HCM</p>
            <p class="text-xs text-muted mt-1">&copy; ${new Date().getFullYear()} ${APP_NAME}</p>
          </div>
        </div>
      </main>
    </div>`);

  const form = $('form', root);
  const submitBtn = $('#login-submit', root);

  const validate = () => {
    let ok = true;
    for (const name of ['username', 'password']) {
      const empty = !form.elements[name].value.trim();
      form.elements[name].classList.toggle('is-invalid', empty);
      $(`[data-error="${name}"]`, form).classList.toggle('hidden', !empty);
      ok &&= !empty;
    }
    return ok;
  };

  // Chưa có chức năng đặt lại mật khẩu: hướng dẫn liên hệ GVHD
  on(root, 'click', '[data-action="forgot-password"]', () => {
    openModal({
      title: 'Quên mật khẩu',
      maxWidth: '420px',
      hasFooter: false,
      body: html`
        <p class="forgot-text">Chức năng đặt lại mật khẩu hiện chưa hỗ trợ.</p>
        <p class="forgot-text">Vui lòng liên hệ <strong>giảng viên hướng dẫn (GVHD)</strong> để được hỗ trợ cấp lại mật khẩu.</p>
        <div class="forgot-actions"><button type="button" class="btn btn-primary btn-sm" data-close>Đã hiểu</button></div>`,
    });
  });

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    if (!validate()) return;
    submitBtn.disabled = true;
    submitBtn.textContent = 'Đang xác thực...';
    try {
      const res = await api.post('/auth/login', {
        username: form.elements.username.value.trim(),
        password: form.elements.password.value,
      });
      const data = unwrap(res);
      auth.setAuth(data);
      toast.success(`Chào mừng ${data.fullName} quay trở lại!`);
      navigate(query.returnUrl || '/dashboard');
    } catch {
      submitBtn.disabled = false;
      submitBtn.textContent = 'Đăng nhập';
    }
  });

  form.elements.username.focus();
}
