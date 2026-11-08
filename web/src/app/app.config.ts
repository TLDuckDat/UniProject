import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import localeVi from '@angular/common/locales/vi';
import { ApplicationConfig, LOCALE_ID, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideNzI18n, vi_VN } from 'ng-zorro-antd/i18n';
import { provideNzIcons } from 'ng-zorro-antd/icon';

import { provideApiConfiguration } from './api/api-configuration';
import { routes } from './app.routes';
import { problemDetailsInterceptor } from './core/http/problem-details.interceptor';
import { APP_ICONS } from './core/icons';

// Định dạng số/ngày theo locale "vi" (dấu phẩy thập phân, dd/MM/yyyy).
registerLocaleData(localeVi);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withFetch(), withInterceptors([problemDetailsInterceptor])),
    // rootUrl rỗng: gọi tương đối /api/... – dev đi qua proxy.conf.json, prod cùng origin với API.
    provideApiConfiguration(''),
    { provide: LOCALE_ID, useValue: 'vi' },
    provideNzI18n(vi_VN),
    provideNzIcons(APP_ICONS),
  ],
};
