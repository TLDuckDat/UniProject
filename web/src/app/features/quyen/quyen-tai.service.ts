import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

/**
 * Tải file quyển dạng blob để xem trong trình duyệt (iframe) – cần Bearer nên không dùng URL trực tiếp.
 * OpenAPI sinh `void` cho thao tác trả file, nên gọi qua service của feature.
 */
@Injectable({ providedIn: 'root' })
export class QuyenTaiService {
  private readonly http = inject(HttpClient);

  taiPhienBan(phienBanId: number): Observable<Blob> {
    return this.http.get(`/api/v1/quyen/${phienBanId}/tai`, { responseType: 'blob' });
  }
}
