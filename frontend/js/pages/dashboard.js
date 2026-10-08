import { html, setHtml } from '../core/dom.js';
import { api, paged, unwrap } from '../core/api.js';
import { auth } from '../core/auth.js';
import { lookup, lecturerName } from '../core/lookup.js';
import { fmtDate, fmtDateTime, statusLabel, label } from '../core/format.js';
import { statusBadge, badge, skeleton } from '../components/ui.js';

const ACTIVE_PERIOD = ['DANG_MO_DANG_KY', 'DANG_THUC_HIEN', 'DANG_BAO_VE'];

function currentSemesterText(periods) {
  const p = periods.find((x) => ACTIVE_PERIOD.includes(x.status));
  return p ? `${p.name}` : 'Chưa có đợt đồ án đang diễn ra';
}

function welcome(subtitle) {
  return html`
    <div class="welcome-header">
      <div class="welcome-title">
        <h1>Xin chào, ${auth.user?.fullName}</h1>
        <p class="welcome-subtitle">${subtitle} · ${auth.roleName}</p>
      </div>
    </div>`;
}

function summaryStrip(items) {
  return html`
    <div class="summary-strip">
      ${items.map((it, i) => html`
        ${i > 0 ? html`<div class="summary-divider"></div>` : ''}
        <div class="summary-item">
          <div class="summary-num ${it.cls || ''}">${it.value}</div>
          <div class="summary-text">${it.label}</div>
        </div>`)}
    </div>`;
}

// ---------- Sinh viên ----------
const STEPS = ['Đăng ký', 'Phân công GVHD', 'Thực hiện đồ án', 'Phản biện', 'Bảo vệ', 'Kết quả'];

function stepIndex(project, hasAdvisor) {
  if (!project) return 0;
  switch (project.status) {
    case 'HOAN_THANH': return 6;
    case 'DA_BAO_VE': return 5;
    case 'CHO_BAO_VE': return 4;
    case 'DANG_THUC_HIEN': return 2;
    default: return hasAdvisor ? 2 : 1;
  }
}

function lifecycle(current) {
  return html`
    <div class="lifecycle-timeline">
      ${STEPS.map((s, i) => {
        const state = i < current ? 'done' : (i === current ? 'current' : 'pending');
        return html`
          ${i > 0 ? html`<div class="lifecycle-line ${i <= current ? 'done' : ''}"></div>` : ''}
          <div class="lifecycle-step ${state}">
            <div class="step-indicator">${state === 'done' ? '✓' : (state === 'current' ? '●' : i + 1)}</div>
            <div class="step-label">${i + 1}. ${s}</div>
          </div>`;
      })}
    </div>`;
}

async function studentView(periods) {
  const [student, projects, lecturers] = await Promise.all([
    lookup.myStudent().catch(() => null),
    api.get('/projects', { pageSize: 20 }).then((r) => paged(r).items),
    lookup.lecturerMap(),
  ]);
  const project = projects.find((p) => !['DA_HUY'].includes(p.status)) ?? projects[0];
  const openPeriod = periods.find((p) => p.status === 'DANG_MO_DANG_KY');

  if (!project) {
    return html`
      <div class="project-hero-panel">
        <span class="eyebrow">Đồ án hiện tại</span>
        <h2 class="hero-project-name">Bạn chưa có đồ án nào</h2>
        <div class="hero-code">${student ? `${student.code} · ${student.className}` : ''}</div>
        <div class="mt-4 flex gap-2">
          ${openPeriod ? html`<a href="#/registrations/wizard" class="btn btn-primary btn-sm">Đăng ký nhóm cho ${openPeriod.name}</a>` : ''}
          <a href="#/topics" class="btn btn-sm">Khám phá đề tài</a>
        </div>
      </div>
      <div class="section-container">
        <h3 class="section-title">Quy trình thực hiện</h3>
        ${lifecycle(0)}
      </div>`;
  }

  const reports = unwrap(await api.get(`/projects/${project.id}/progress-reports`, null, { silent: true }).catch(() => null)) ?? [];
  const latest = [...reports].sort((a, b) => b.reportNumber - a.reportNumber)[0];
  const review = reports.flatMap((r) => r.reviews.map((rv) => ({ ...rv, report: r })))
    .sort((a, b) => new Date(b.reviewedDate) - new Date(a.reviewedDate))[0];
  const advisor = project.advisors.find((a) => a.isActive && a.advisorType === 'CHINH') ?? project.advisors.find((a) => a.isActive);
  const period = periods.find((p) => p.id === project.periodId);
  const progress = latest?.completionPercentage ?? 0;

  return html`
    <div class="project-hero-panel">
      <div class="hero-top">
        <div>
          <span class="eyebrow">Đồ án hiện tại</span>
          <h2 class="hero-project-name">${project.title}</h2>
          <div class="hero-code text-mono">${project.code} · ${period?.name ?? ''}</div>
        </div>
        <div class="hero-status">${statusBadge(project.status)}</div>
      </div>
      <div class="hero-details-grid">
        <div class="detail-item">
          <span class="detail-label">Giảng viên hướng dẫn</span>
          <span class="detail-val">${advisor ? lecturerName(lecturers, advisor.lecturerId) : 'Chưa phân công'}</span>
        </div>
        <div class="detail-item">
          <span class="detail-label">Hạn nộp báo cáo</span>
          <span class="detail-val text-warning">${fmtDate(period?.reportDeadline)}</span>
        </div>
        <div class="detail-item">
          <span class="detail-label">Tiến độ (báo cáo gần nhất)</span>
          <span class="detail-val font-semibold">${progress}%</span>
        </div>
      </div>
      <div class="progress-bar-container"><div class="progress-bar-fill" style="width:${progress}%"></div></div>
    </div>

    <div class="section-container">
      <h3 class="section-title">Quy trình thực hiện</h3>
      ${lifecycle(stepIndex(project, !!advisor))}
    </div>

    <div class="two-col-grid">
      <div class="panel">
        <h3 class="panel-header-title">Việc cần làm</h3>
        <div class="action-card">
          <div class="action-info">
            <div class="action-heading">Nộp báo cáo tiến độ lần ${(latest?.reportNumber ?? 0) + 1}</div>
            <div class="action-desc">Mô tả công việc đã hoàn thành và kế hoạch tiếp theo.</div>
            <div class="action-meta text-warning">Hạn báo cáo của đợt: ${fmtDate(period?.reportDeadline)}</div>
          </div>
          <a href="#/projects/${project.id}" class="btn btn-primary btn-sm">Nộp báo cáo</a>
        </div>
      </div>
      <div class="panel">
        <h3 class="panel-header-title">Nhận xét mới nhất từ GVHD</h3>
        ${review ? html`
          <div class="feedback-box">
            <div class="feedback-meta"><strong>${lecturerName(lecturers, review.lecturerId)}</strong> · <span>${fmtDateTime(review.reviewedDate)}</span></div>
            <p class="feedback-text">"${review.reviewContent}"</p>
            <div class="feedback-tag">${badge('Đánh giá: ' + label('evaluation', review.evaluation), review.evaluation === 'KHONG_DAT' ? 'danger' : 'success')}</div>
          </div>` : html`<p class="text-sm text-muted">Chưa có nhận xét nào.</p>`}
      </div>
    </div>`;
}

// ---------- Giảng viên ----------
async function lecturerView() {
  const [me, projectsRes, regsRes, topicsRes, committeesRes] = await Promise.all([
    lookup.myLecturer().catch(() => null),
    api.get('/projects', { pageSize: 100 }, { silent: true }).catch(() => null),
    api.get('/registrations', { status: 'CHO_DUYET', pageSize: 1 }, { silent: true }).catch(() => null),
    api.get('/topics', { status: 'CHO_DUYET', pageSize: 100 }, { silent: true }).catch(() => null),
    api.get('/defenses/committees', { pageSize: 100 }, { silent: true }).catch(() => null),
  ]);
  const projects = paged(projectsRes).items;
  const advising = me ? projects.filter((p) => p.advisors.some((a) => a.isActive && a.lecturerId === me.id)) : projects;
  const myCommittees = me ? paged(committeesRes).items.filter((c) => c.members.some((m) => m.lecturerId === me.id)) : [];
  const pendingTopics = paged(topicsRes).items.filter((t) => !me || t.proposedByLecturerId !== me.id);

  // Báo cáo tiến độ chưa có nhận xét trong các đồ án đang hướng dẫn
  const reportLists = await Promise.all(advising.slice(0, 20).map((p) =>
    api.get(`/projects/${p.id}/progress-reports`, null, { silent: true }).then((r) => (unwrap(r) ?? []).map((x) => ({ ...x, project: p }))).catch(() => [])));
  const unreviewed = reportLists.flat().filter((r) => r.reviews.length === 0)
    .sort((a, b) => new Date(b.submittedDate) - new Date(a.submittedDate));

  const tasks = [
    ...unreviewed.slice(0, 5).map((r) => ({
      title: `${r.project.code} vừa nộp báo cáo tiến độ lần ${r.reportNumber}: ${r.title}`,
      sub: `${r.project.title} · ${fmtDateTime(r.submittedDate)}`,
      href: `#/projects/${r.project.id}`, btn: 'Xem & Nhận xét', cls: 'btn-primary',
    })),
    ...pendingTopics.slice(0, 3).map((t) => ({
      title: `Đề tài "${t.title}" đang chờ duyệt`,
      sub: `${t.code} · ${label('projectKind', t.projectKind)}`,
      href: '#/topics/approval', btn: 'Xem chi tiết', cls: '',
    })),
  ];

  return html`
    ${summaryStrip([
      { value: advising.length, label: 'Đồ án hướng dẫn' },
      { value: unreviewed.length, label: 'Báo cáo cần nhận xét', cls: 'text-warning' },
      { value: paged(regsRes).totalItems, label: 'Đăng ký đề tài chờ duyệt', cls: 'text-info' },
      { value: myCommittees.length, label: 'Hội đồng tham gia' },
    ])}
    <div class="section-container">
      <h3 class="section-title">Cần xử lý</h3>
      <div class="task-list">
        ${tasks.length === 0 ? html`<div class="task-item"><div class="task-content"><div class="task-sub">Không có việc nào cần xử lý.</div></div></div>`
          : tasks.map((t) => html`
            <div class="task-item">
              <div class="task-dot"></div>
              <div class="task-content">
                <div class="task-title">${t.title}</div>
                <div class="task-sub">${t.sub}</div>
              </div>
              <a href="${t.href}" class="btn btn-sm ${t.cls}">${t.btn}</a>
            </div>`)}
      </div>
    </div>`;
}

// ---------- Admin ----------
async function adminView(periods) {
  const current = periods.find((p) => ACTIVE_PERIOD.includes(p.status)) ?? periods[0];
  const [students, lecturers, regs, projectsRes, topicsRes, committeesRes] = await Promise.all([
    api.get('/students', { pageSize: 1 }, { silent: true }).catch(() => null),
    api.get('/lecturers', { pageSize: 1 }, { silent: true }).catch(() => null),
    api.get('/registrations', { pageSize: 1, periodId: current?.id }, { silent: true }).catch(() => null),
    api.get('/projects', { pageSize: 100, periodId: current?.id }, { silent: true }).catch(() => null),
    api.get('/topics', { status: 'CHO_DUYET', pageSize: 1 }, { silent: true }).catch(() => null),
    api.get('/defenses/committees', { pageSize: 100, periodId: current?.id }, { silent: true }).catch(() => null),
  ]);
  const projects = paged(projectsRes).items;
  const noAdvisor = projects.filter((p) => !p.advisors.some((a) => a.isActive)).length;
  const notReady = paged(committeesRes).items.filter((c) => !c.isReadyForDefense).length;
  const pendingTopics = paged(topicsRes).totalItems;

  const alerts = [];
  if (noAdvisor) alerts.push({ cls: 'warning', tag: 'Cảnh báo', text: html`<strong>${noAdvisor} đồ án</strong> chưa được phân công giảng viên hướng dẫn.`, href: '#/projects', btn: 'Xử lý ngay →' });
  if (notReady) alerts.push({ cls: 'danger', tag: 'Khẩn cấp', text: html`<strong>${notReady} hội đồng bảo vệ</strong> chưa đủ thành phần theo quy chế (3 - 5 thành viên, 1 chủ tịch, 1 thư ký).`, href: '#/defense', btn: 'Bổ sung thành viên →' });
  if (pendingTopics) alerts.push({ cls: 'info', tag: 'Thông báo', text: html`<strong>${pendingTopics} đề tài</strong> đang chờ phê duyệt.`, href: '#/topics/approval', btn: 'Duyệt đề tài →' });
  if (!alerts.length) alerts.push({ cls: 'success', tag: 'Ổn định', text: 'Không có vấn đề nào cần xử lý.', href: '#/periods', btn: 'Xem đợt đồ án →' });

  return html`
    <div class="period-banner">
      <div class="period-info">
        <span class="eyebrow">Đợt đồ án hiện hành</span>
        ${current ? html`
          <h2 class="period-name">${current.name}</h2>
          <div class="period-dates">${fmtDate(current.startDate)} &rarr; ${fmtDate(current.endDate)} · ${statusLabel(current.status)}</div>`
          : html`<h2 class="period-name">Chưa có đợt đồ án</h2><div class="period-dates">Tạo đợt mới để bắt đầu quy trình.</div>`}
      </div>
      <div class="period-action"><a href="#/periods" class="btn btn-sm">Quản lý đợt</a></div>
    </div>
    ${summaryStrip([
      { value: paged(students).totalItems, label: 'Sinh viên trong hệ thống' },
      { value: paged(regs).totalItems, label: 'Nhóm đăng ký (đợt hiện hành)' },
      { value: projects.filter((p) => p.status === 'DANG_THUC_HIEN').length, label: 'Đồ án đang thực hiện' },
      { value: paged(lecturers).totalItems, label: 'Giảng viên' },
    ])}
    <div class="section-container">
      <h3 class="section-title">Cảnh báo hệ thống</h3>
      <div class="attention-list">
        ${alerts.map((a) => html`
          <div class="attention-item">
            <div class="attention-badge ${a.cls}">${a.tag}</div>
            <div class="attention-text">${a.text}</div>
            <a href="${a.href}" class="btn btn-sm btn-ghost">${a.btn}</a>
          </div>`)}
      </div>
    </div>`;
}

export async function render(root) {
  setHtml(root, html`
    <div class="dashboard-page">
      ${welcome('Đang tải...')}
      <div class="card p-5 space-y-3">${skeleton('40%', '20px')}${skeleton('100%', '60px')}${skeleton('100%', '60px')}</div>
    </div>`);

  const periods = await lookup.periods().catch(() => []);
  let body;
  if (auth.isStudent) body = await studentView(periods);
  else if (auth.isLecturer) body = await lecturerView();
  else body = await adminView(periods);

  setHtml(root, html`
    <div class="dashboard-page">
      ${welcome(currentSemesterText(periods))}
      ${body}
    </div>`);
}
