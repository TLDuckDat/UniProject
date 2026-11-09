import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

/** Tải file nhị phân (phiếu giao nhiệm vụ PDF). OpenAPI sinh `void` cho thao tác trả file, nên gọi qua service của feature. */
@Injectable({ providedIn: 'root' })
export class TienDoTaiService {
  private readonly http = inject(HttpClient);

  taiPhieuPdf(svTrongDotId: number): Observable<Blob> {
    return this.http.get(`/api/v1/svtrongdot/${svTrongDotId}/phieu-giao-nhiem-vu/pdf`, {
      responseType: 'blob',
    });
  }
}
