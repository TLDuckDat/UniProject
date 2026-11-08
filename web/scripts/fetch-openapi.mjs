// Tải tài liệu OpenAPI từ API đang chạy về openapi/swagger.json để ng-openapi-gen sinh code.
// Nếu API chưa chạy: giữ nguyên openapi/swagger.json đã commit và chỉ cảnh báo.
// Đổi địa chỉ API bằng biến môi trường UPM_API_URL (mặc định http://localhost:5080).
import { mkdir, writeFile } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const apiUrl = process.env.UPM_API_URL ?? 'http://localhost:5080';
const source = `${apiUrl}/swagger/v1/swagger.json`;
const target = resolve(dirname(fileURLToPath(import.meta.url)), '..', 'openapi', 'swagger.json');

try {
  const response = await fetch(source, { signal: AbortSignal.timeout(10_000) });
  if (!response.ok) {
    throw new Error(`HTTP ${response.status}`);
  }
  const spec = await response.json();
  await mkdir(dirname(target), { recursive: true });
  await writeFile(target, JSON.stringify(spec, null, 2) + '\n', 'utf8');
  console.log(`Đã tải OpenAPI từ ${source}`);
} catch (error) {
  console.warn(
    `[cảnh báo] Không tải được ${source} (${error.message}). Dùng openapi/swagger.json hiện có.`,
  );
}
