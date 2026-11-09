import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';

@Component({
  selector: 'app-quan-tri-layout',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet, NzButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nav aria-label="Quản trị">
      <a
        nz-button
        nzType="link"
        routerLink="/quan-tri"
        routerLinkActive="active"
        [routerLinkActiveOptions]="{ exact: true }"
        >Danh mục</a
      >
      <a nz-button nzType="link" routerLink="/quan-tri/rule-set" routerLinkActive="active"
        >RuleSet & tham số</a
      >
      <a nz-button nzType="link" routerLink="/quan-tri/import" routerLinkActive="active"
        >Import đồng bộ</a
      >
      <a nz-button nzType="link" routerLink="/quan-tri/tai-khoan" routerLinkActive="active"
        >Tài khoản</a
      >
    </nav>
    <router-outlet />
  `,
  styles: `
    nav {
      display: flex;
      gap: 8px;
      border-bottom: 1px solid #eee;
      padding: 8px 20px;
    }
    .active {
      font-weight: 600;
      color: #1677ff;
    }
  `,
})
export class QuanTriLayout {}
