import { html, setHtml } from './dom.js';

let container;

function ensureContainer() {
  if (!container) {
    container = document.createElement('div');
    container.className = 'toast-container';
    document.body.appendChild(container);
  }
  return container;
}

function show(type, message, title, duration = 4000) {
  const item = document.createElement('div');
  item.className = `toast-item toast-${type}`;
  setHtml(item, html`
    <div class="toast-content">
      ${title ? html`<div class="toast-title">${title}</div>` : ''}
      <div class="toast-message">${message}</div>
    </div>
    <button class="toast-close" aria-label="Đóng">&times;</button>
  `);
  item.querySelector('.toast-close').addEventListener('click', () => item.remove());
  ensureContainer().appendChild(item);
  if (duration > 0) setTimeout(() => item.remove(), duration);
}

export const toast = {
  success: (message, title = 'Thành công') => show('success', message, title),
  error: (message, title = 'Lỗi') => show('danger', message, title, 5000),
  warning: (message, title = 'Cảnh báo') => show('warning', message, title),
  info: (message, title = 'Thông tin') => show('info', message, title),
};
