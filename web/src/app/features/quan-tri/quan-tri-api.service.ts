import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}
export interface CatalogItem {
  id: number;
  ma: string;
  ten: string;
  parentId?: number | null;
  parentTen?: string | null;
  coSoId?: number | null;
  email?: string | null;
  loai?: string | null;
  soLuong?: number | null;
  soTinChi?: number | null;
  nganh?: string | null;
  conHoatDong?: boolean | null;
  tinhDiem?: boolean | null;
  hocKyKeHoach?: number | null;
  laHPThayTheDATN?: boolean | null;
  he?: string | null;
  chuongTrinh?: string | null;
  tongTCYeuCau?: number | null;
  tcdatn?: number | null;
  tcttn?: number | null;
  coHocPhanThayThe?: boolean | null;
  hocPhanId?: number | null;
}
export type CatalogRequest = Omit<CatalogItem, 'id' | 'parentTen'>;
export interface RuleSetItem {
  id: number;
  maRuleSet: string;
  tenRuleSet: string;
  canCuPhapLy?: string | null;
  maxTCChuaTichLuy: number;
  gpaBaoVeMin: number;
  gpaTotNghiepMin: number;
  diemDatDATN: number;
  trongSoQT: number;
  heSoThoiGianToiDa: number;
  minTVHoiDong: number;
  maxTVHoiDong: number;
  nguongHaHangHocLaiPct: number;
  haHangKhiKyLuat: boolean;
  hieuLucTu: string;
  hieuLucDen?: string | null;
  daSuDung: boolean;
}
export type RuleSetRequest = Omit<RuleSetItem, 'id' | 'daSuDung'>;
export interface RuleSetCohort {
  ruleSetId: number;
  khoaHocTu: number;
  khoaHocDen: number | null;
  canXacNhan: boolean;
}
export interface ThamSoBoMon {
  id: number;
  boMonId: number;
  ruleSetId: number;
  trongSoQT: number;
  trongSoHD: number;
  tachDiemKhac: boolean;
  soNgaySuaGapToiDa: number;
  dungHanKhiNghiTamThoi: boolean;
}
export type ThamSoBoMonRequest = Omit<ThamSoBoMon, 'id'>;
export interface ThamSoDaoVan {
  id: number;
  khoaId: number;
  ruleSetId: number | null;
  chuongTrinh: string;
  bat: boolean;
  nguongPhanTram: number;
}
export type ThamSoDaoVanRequest = Omit<ThamSoDaoVan, 'id'>;
export interface AccountRole {
  maVaiTro: string;
  coSoId?: number | null;
  khoaId?: number | null;
  boMonId?: number | null;
}
export interface AccountItem {
  id: number;
  tenDangNhap: string;
  email?: string | null;
  loaiTK: string;
  conHoatDong: boolean;
  giangVienId?: number | null;
  sinhVienId?: number | null;
  vaiTro: AccountRole[];
}
export interface AccountRequest extends Omit<AccountItem, 'id'> {
  matKhau?: string | null;
}
export interface ImportError {
  row: number;
  field: string;
  message: string;
}
export interface ImportPreview {
  previewId: string;
  loai: string;
  fileName: string;
  totalRows: number;
  validRows: number;
  errors: ImportError[];
  expiresAtUtc: string;
}

@Injectable({ providedIn: 'root' })
export class QuanTriApiService {
  private readonly http = inject(HttpClient);
  danhMuc(
    loai: string,
    q = '',
    page = 1,
    pageSize = 20,
    sort?: string,
    desc = false,
  ): Observable<Paged<CatalogItem>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize).set('desc', desc);
    if (q) params = params.set('q', q);
    if (sort) params = params.set('sort', sort);
    return this.http.get<Paged<CatalogItem>>(`/api/v1/danh-muc/${loai}`, { params });
  }
  layDanhMuc(loai: string, id: number): Observable<CatalogItem> {
    return this.http.get<CatalogItem>(`/api/v1/danh-muc/${loai}/${id}`);
  }
  taoDanhMuc(loai: string, body: CatalogRequest): Observable<CatalogItem> {
    return this.http.post<CatalogItem>(`/api/v1/danh-muc/${loai}`, body);
  }
  suaDanhMuc(loai: string, id: number, body: CatalogRequest): Observable<CatalogItem> {
    return this.http.put<CatalogItem>(`/api/v1/danh-muc/${loai}/${id}`, body);
  }
  xoaDanhMuc(loai: string, id: number): Observable<void> {
    return this.http.delete<void>(`/api/v1/danh-muc/${loai}/${id}`);
  }
  taiKhoans(q = '', page = 1, pageSize = 20): Observable<Paged<AccountItem>> {
    return this.http.get<Paged<AccountItem>>('/api/v1/tai-khoan/quan-ly', {
      params: { q, page, pageSize },
    });
  }
  taoTaiKhoan(body: AccountRequest): Observable<AccountItem> {
    return this.http.post<AccountItem>('/api/v1/tai-khoan/quan-ly', body);
  }
  suaTaiKhoan(id: number, body: AccountRequest): Observable<AccountItem> {
    return this.http.put<AccountItem>(`/api/v1/tai-khoan/quan-ly/${id}`, body);
  }
  xoaTaiKhoan(id: number): Observable<void> {
    return this.http.delete<void>(`/api/v1/tai-khoan/quan-ly/${id}`);
  }
  ruleSets(q = '', page = 1): Observable<Paged<RuleSetItem>> {
    return this.http.get<Paged<RuleSetItem>>('/api/v1/rule-set', {
      params: { q, page, pageSize: 200 },
    });
  }
  cloneRuleSet(id: number, maRuleSet: string, tenRuleSet: string): Observable<RuleSetItem> {
    return this.http.post<RuleSetItem>(`/api/v1/rule-set/${id}/nhan-ban`, {
      maRuleSet,
      tenRuleSet,
    });
  }
  saveRuleSet(id: number | null, body: RuleSetRequest): Observable<RuleSetItem> {
    return id === null
      ? this.http.post<RuleSetItem>('/api/v1/rule-set', body)
      : this.http.put<RuleSetItem>(`/api/v1/rule-set/${id}`, body);
  }
  ruleSetCohorts(id: number): Observable<RuleSetCohort[]> {
    return this.http.get<RuleSetCohort[]>(`/api/v1/rule-set/${id}/khoa-hoc`);
  }
  saveRuleSetCohorts(id: number, rows: RuleSetCohort[]): Observable<void> {
    return this.http.put<void>(`/api/v1/rule-set/${id}/khoa-hoc`, rows);
  }
  saveThamSoBoMon(body: ThamSoBoMonRequest): Observable<ThamSoBoMon> {
    return this.http.put<ThamSoBoMon>('/api/v1/rule-set/tham-so-bo-mon', body);
  }
  thamSoBoMon(ruleSetId: number): Observable<ThamSoBoMon[]> {
    return this.http.get<ThamSoBoMon[]>(`/api/v1/rule-set/${ruleSetId}/tham-so-bo-mon`);
  }
  saveThamSoDaoVan(body: ThamSoDaoVanRequest): Observable<ThamSoDaoVan> {
    return this.http.put<ThamSoDaoVan>('/api/v1/rule-set/tham-so-dao-van', body);
  }
  thamSoDaoVan(khoaId: number): Observable<ThamSoDaoVan[]> {
    return this.http.get<ThamSoDaoVan[]>('/api/v1/rule-set/tham-so-dao-van', {
      params: { khoaId },
    });
  }
  preview(loai: string, file: File): Observable<ImportPreview> {
    const data = new FormData();
    data.append('file', file);
    return this.http.post<ImportPreview>(`/api/v1/dong-bo/${loai}/xem-truoc`, data);
  }
  commit(previewId: string): Observable<{ inserted: number; updated: number; skipped: number }> {
    return this.http.post<{ inserted: number; updated: number; skipped: number }>(
      '/api/v1/dong-bo/xac-nhan',
      { previewId },
    );
  }
  downloadTemplate(loai: string): Observable<Blob> {
    return this.http.get(`/api/v1/dong-bo/${loai}/mau`, { responseType: 'blob' });
  }
}
