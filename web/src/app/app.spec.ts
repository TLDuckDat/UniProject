import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';

import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), provideNzIconsTesting()],
    }).compileComponents();
  });

  it('hiển thị tên hệ thống ở header', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const title = (fixture.nativeElement as HTMLElement).querySelector('h1')?.textContent;
    expect(title).toContain('UPM – Quản lý Đồ án tốt nghiệp');
  });
});
