import { resolve } from './core/router.js';
import { mountShell, unmountShell, setActiveNav } from './components/shell.js';
import { html, setHtml } from './core/dom.js';
import { emptyState } from './components/ui.js';

const app = document.getElementById('app');
let cleanup = null;
let renderId = 0;

async function render() {
  const resolved = resolve();
  if (!resolved) return;
  const { route, params, query, path } = resolved;
  const id = ++renderId;

  if (typeof cleanup === 'function') cleanup();
  cleanup = null;
  // Modal / panel thông báo gắn vào body nên phải gỡ khi chuyển trang
  document.querySelectorAll('.modal-backdrop, .notification-panel-backdrop').forEach((el) => el.remove());

  let target;
  if (route.public) {
    unmountShell();
    target = app;
  } else {
    target = mountShell(app);
    setActiveNav(path);
  }

  try {
    const page = await import(`./pages/${route.page}.js`);
    if (id !== renderId) return; // người dùng đã chuyển trang khác trong lúc tải
    // Mỗi lần render một container riêng: trang cũ tải chậm sẽ ghi vào container đã bị gỡ, không đè trang mới
    const pageRoot = document.createElement('div');
    target.replaceChildren(pageRoot);
    window.scrollTo(0, 0);
    cleanup = await page.render(pageRoot, { params, query, path, rerender: render });
  } catch (err) {
    if (id !== renderId) return;
    if (err?.status) return; // lỗi API đã hiện toast
    console.error(err);
    setHtml(target, html`<div class="card p-6">${emptyState({ title: 'Không tải được trang', description: err.message })}</div>`);
  }
}

window.addEventListener('hashchange', render);
if (!location.hash) location.hash = '#/dashboard';
render();
