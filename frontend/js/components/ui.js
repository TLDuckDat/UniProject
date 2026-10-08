import { html, raw, setHtml, icons, $ } from '../core/dom.js';
import { statusLabel, statusVariant } from '../core/format.js';

export function pageHeader({ title, subtitle, breadcrumbs, backUrl, actions = '' }) {
  return html`
    <div class="page-header">
      <div class="page-header-main">
        ${backUrl ? html`<div class="back-link-wrap"><a href="${backUrl}" class="back-link">${icons.back} Quay lại danh sách</a></div>` : ''}
        ${breadcrumbs ? html`<div class="page-header-breadcrumbs"><span>${breadcrumbs}</span></div>` : ''}
        <h1 class="page-title">${title}</h1>
        ${subtitle ? html`<p class="page-subtitle">${subtitle}</p>` : ''}
      </div>
      <div class="page-header-actions">${actions}</div>
    </div>`;
}

export const badge = (text, variant = 'default') => html`<span class="badge badge-${variant === 'neutral' ? 'default' : variant}">${text}</span>`;

export const statusBadge = (status) => badge(statusLabel(status), statusVariant(status));

export function emptyState({ title = 'Không có dữ liệu', description, actionText, action = '' }) {
  return html`
    <div class="empty-state">
      <div class="empty-icon">${icons.info}</div>
      <h4 class="empty-title">${title}</h4>
      ${description ? html`<p class="empty-description">${description}</p>` : ''}
      ${actionText ? html`<div class="empty-actions"><button class="btn btn-primary btn-sm" data-action="${action}">${actionText}</button></div>` : ''}
    </div>`;
}

export const skeleton = (width = '100%', height = '16px') =>
  html`<div class="skeleton" style="width:${width};height:${height}"></div>`;

export const skeletonRows = (n = 3) =>
  html`<div class="card p-4"><div class="space-y-3">${Array.from({ length: n }, () => skeleton('100%', '45px'))}</div></div>`;

export const skeletonCards = (n = 4) => html`
  <div class="topic-grid">
    ${Array.from({ length: n }, () => html`
      <div class="card p-5 space-y-3">
        ${skeleton('30%', '20px')}${skeleton('80%', '24px')}${skeleton('100%', '48px')}${skeleton('100%', '32px')}
      </div>`)}
  </div>`;

function pageList(current, total) {
  if (total <= 5) return Array.from({ length: total }, (_, i) => i + 1);
  const pages = [1];
  if (current > 3) pages.push(-1);
  for (let i = Math.max(2, current - 1); i <= Math.min(total - 1, current + 1); i++) pages.push(i);
  if (current < total - 2) pages.push(-1);
  pages.push(total);
  return pages;
}

// Thanh phân trang; click các nút có data-page để chuyển trang
export function pagination({ pageNumber, totalPages, totalItems }) {
  if (!totalPages || totalPages <= 1) return '';
  return html`
    <div class="pagination-wrapper">
      <div class="pagination-info">
        Hiển thị trang <strong>${pageNumber}</strong> / <strong>${totalPages}</strong> (Tổng số ${totalItems} mục)
      </div>
      <div class="pagination-controls">
        <button class="btn btn-sm" data-page="${pageNumber - 1}" ${raw(pageNumber <= 1 ? 'disabled' : '')}>&larr; Trước</button>
        <div class="page-numbers">
          ${pageList(pageNumber, totalPages).map((p) => p === -1
            ? html`<span class="ellipsis">...</span>`
            : html`<button class="btn btn-sm btn-page ${p === pageNumber ? 'active' : ''}" data-page="${p}">${p}</button>`)}
        </div>
        <button class="btn btn-sm" data-page="${pageNumber + 1}" ${raw(pageNumber >= totalPages ? 'disabled' : '')}>Sau &rarr;</button>
      </div>
    </div>`;
}

export function tabs(items, active) {
  return html`
    <div class="tabs-nav">
      ${items.map((t) => html`
        <button class="tab-btn ${t.id === active ? 'active' : ''}" data-tab="${t.id}">
          ${t.label}${t.count !== undefined ? html`<span class="tab-count">${t.count}</span>` : ''}
        </button>`)}
    </div>`;
}

export function segmented(items, active) {
  return html`
    <div class="status-tabs">
      ${items.map((t) => html`<button class="seg-btn ${t.key === active ? 'active' : ''}" data-filter="${t.key}">${t.label}</button>`)}
    </div>`;
}

export const options = (items, selected, { value = 'id', text = 'name', placeholder } = {}) => html`
  ${placeholder !== undefined ? html`<option value="">${placeholder}</option>` : ''}
  ${items.map((it) => {
    const v = typeof value === 'function' ? value(it) : it[value];
    const t = typeof text === 'function' ? text(it) : it[text];
    return html`<option value="${v}" ${raw(String(v) === String(selected ?? '') ? 'selected' : '')}>${t}</option>`;
  })}`;

/**
 * Mở modal. body là html``; nếu có form bên trong thì onConfirm nhận form element.
 * onConfirm trả về false để giữ modal mở (vd validate lỗi).
 */
export function openModal({
  title, body, maxWidth = '560px', hasFooter = true,
  confirmText = 'Xác nhận', cancelText = 'Hủy', confirmVariant = 'primary',
  onConfirm, onOpen,
}) {
  const backdrop = document.createElement('div');
  backdrop.className = 'modal-backdrop';
  setHtml(backdrop, html`
    <div class="modal-dialog" style="max-width:${maxWidth}">
      <div class="modal-header">
        <h3 class="modal-title">${title}</h3>
        <button class="modal-close" data-close aria-label="Đóng">&times;</button>
      </div>
      <div class="modal-body">${body}</div>
      ${hasFooter ? html`
        <div class="modal-footer">
          <button class="btn btn-sm" data-close>${cancelText}</button>
          <button class="btn btn-sm ${confirmVariant === 'danger' ? 'btn-danger' : 'btn-primary'}" data-confirm>${confirmText}</button>
        </div>` : ''}
    </div>`);
  document.body.appendChild(backdrop);

  const dialog = $('.modal-dialog', backdrop);
  const close = () => {
    backdrop.remove();
    document.removeEventListener('keydown', onKey);
  };
  const onKey = (e) => { if (e.key === 'Escape') close(); };
  document.addEventListener('keydown', onKey);

  backdrop.addEventListener('click', (e) => {
    if (e.target === backdrop || e.target.closest('[data-close]')) close();
  });

  const confirmBtn = $('[data-confirm]', backdrop);
  const submit = async () => {
    if (!onConfirm) return close();
    const form = $('form', dialog);
    if (form && !form.reportValidity()) return;
    confirmBtn.disabled = true;
    const original = confirmBtn.textContent;
    confirmBtn.textContent = 'Đang xử lý...';
    try {
      const keep = await onConfirm(form, dialog);
      if (keep !== false) close();
    } catch {
      // lỗi đã được toast trong api.js
    } finally {
      if (confirmBtn.isConnected) {
        confirmBtn.disabled = false;
        confirmBtn.textContent = original;
      }
    }
  };
  confirmBtn?.addEventListener('click', submit);
  $('form', dialog)?.addEventListener('submit', (e) => { e.preventDefault(); submit(); });

  onOpen?.(dialog);
  $('input:not([type=hidden]), select, textarea', dialog)?.focus();
  return { close, el: dialog };
}

export function confirmDialog({ title = 'Xác nhận', message, confirmText = 'Xác nhận', danger = false, onConfirm }) {
  return openModal({
    title,
    maxWidth: '440px',
    body: html`<p class="text-sm text-secondary">${message}</p>`,
    confirmText,
    confirmVariant: danger ? 'danger' : 'primary',
    onConfirm,
  });
}

export function promptDialog({ title, label, placeholder = '', confirmText = 'Xác nhận', danger = false, required = true, onConfirm }) {
  return openModal({
    title,
    maxWidth: '480px',
    confirmText,
    confirmVariant: danger ? 'danger' : 'primary',
    body: html`
      <form>
        <div class="form-group">
          <label class="${required ? 'required' : ''}">${label}</label>
          <textarea name="value" placeholder="${placeholder}" ${raw(required ? 'required' : '')}></textarea>
        </div>
      </form>`,
    onConfirm: (form) => onConfirm(form.elements.value.value.trim()),
  });
}
