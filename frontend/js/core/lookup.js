import { api, paged, unwrap } from './api.js';

// Cache tra cứu tên theo ID (DTO của backend chỉ trả ID của module khác)
const cache = new Map();

function once(key, loader) {
  if (!cache.has(key)) cache.set(key, loader().catch((e) => { cache.delete(key); throw e; }));
  return cache.get(key);
}

export function clearLookups() {
  cache.clear();
}

async function allPages(path, query = {}) {
  const first = paged(await api.get(path, { ...query, pageSize: 100, pageNumber: 1 }, { silent: true }));
  const items = [...first.items];
  for (let p = 2; p <= first.totalPages; p++) {
    items.push(...paged(await api.get(path, { ...query, pageSize: 100, pageNumber: p }, { silent: true })).items);
  }
  return items;
}

export const lookup = {
  lecturers: () => once('lecturers', () => allPages('/lecturers')),
  majors: () => once('majors', () => allPages('/majors')),
  classes: () => once('classes', () => allPages('/classes')),
  faculties: () => once('faculties', () => allPages('/faculties')),
  departments: () => once('departments', () => allPages('/departments')),
  periods: () => once('periods', () => allPages('/project-periods')),
  semesters: () => once('semesters', async () => unwrap(await api.get('/semesters', null, { silent: true })) ?? []),
  degrees: () => once('degrees', async () => unwrap(await api.get('/degrees', null, { silent: true })) ?? []),
  ranks: () => once('ranks', async () => unwrap(await api.get('/degrees/ranks', null, { silent: true })) ?? []),
  projectTypes: () => once('projectTypes', async () => unwrap(await api.get('/project-types', null, { silent: true })) ?? []),
  periodTopics: (periodId) => once(`periodTopics:${periodId}`, async () => unwrap(await api.get(`/topics/periods/${periodId}`, null, { silent: true })) ?? []),

  student: (id) => once(`student:${id}`, async () => unwrap(await api.get(`/students/${id}`, null, { silent: true }))),
  topic: (id) => once(`topic:${id}`, async () => unwrap(await api.get(`/topics/${id}`, null, { silent: true }))),

  async lecturerMap() {
    return new Map((await this.lecturers()).map((l) => [l.id, l]));
  },

  async studentMap(ids) {
    const unique = [...new Set(ids.filter(Boolean))];
    const list = await Promise.all(unique.map((id) => this.student(id).catch(() => null)));
    return new Map(unique.map((id, i) => [id, list[i]]));
  },

  myStudent: () => once('me:student', async () => unwrap(await api.get('/students/me', null, { silent: true }))),
  myLecturer: () => once('me:lecturer', async () => unwrap(await api.get('/lecturers/me', null, { silent: true }))),
};

export const lecturerName = (map, id) => {
  const l = map.get(id);
  return l ? l.fullName : (id ? `GV #${id}` : '—');
};
export const studentName = (map, id) => {
  const s = map.get(id);
  return s ? `${s.fullName} (${s.code})` : (id ? `SV #${id}` : '—');
};
