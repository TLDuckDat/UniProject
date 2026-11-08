import { Directive, effect, inject, input, TemplateRef, ViewContainerRef } from '@angular/core';

import { AuthService } from '../../core/auth/auth.service';

/**
 * Ẩn/hiện phần tử theo quyền (có MỘT trong các quyền là đủ):
 * `<button *coQuyen="'Dot.QuanLy'">…</button>` hoặc `*coQuyen="['DeTai.Duyet', 'PhanCong.QuanLy']"`.
 * Chỉ là lớp giao diện – server luôn kiểm quyền lại.
 */
@Directive({ selector: '[coQuyen]' })
export class CoQuyenDirective {
  readonly coQuyen = input.required<string | readonly string[]>();

  private readonly auth = inject(AuthService);
  private readonly template = inject(TemplateRef<unknown>);
  private readonly container = inject(ViewContainerRef);
  private dangHien = false;

  constructor() {
    effect(() => {
      const yeuCau = this.coQuyen();
      const hien = this.auth.coQuyen(...(typeof yeuCau === 'string' ? [yeuCau] : yeuCau));
      if (hien && !this.dangHien) {
        this.container.createEmbeddedView(this.template);
      } else if (!hien && this.dangHien) {
        this.container.clear();
      }
      this.dangHien = hien;
    });
  }
}
