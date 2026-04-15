// ─── DOM ─────────────────────────────────────────────────────
const views = {
  login: document.getElementById('view-login'),
  main:  document.getElementById('view-main')
};

const pages = {
  home:         document.getElementById('page-home'),
  mods:         document.getElementById('page-mods'),
  resourcepacks: document.getElementById('page-resourcepacks'),
  shaderpacks:  document.getElementById('page-shaderpacks'),
  settings:     document.getElementById('page-settings'),
  logs:         document.getElementById('page-logs')
};

const els = {
  loginBtn:             document.getElementById('btn-login'),
  logoutBtn:            document.getElementById('btn-logout'),
  playerAvatar:         document.getElementById('player-avatar'),
  playerName:           document.getElementById('player-name'),
  playBtn:              document.getElementById('btn-play'),
  gameVersion:          document.getElementById('game-version'),
  // Setup progress
  setupBox:             document.getElementById('setup-box'),
  setupText:            document.getElementById('setup-text'),
  setupBar:             document.getElementById('setup-bar'),
  // Game progress
  progressSection:      document.getElementById('progress-section'),
  progressFill:         document.getElementById('progress-fill'),
  progressText:         document.getElementById('progress-text'),
  // Settings
  settingRamMin:        document.getElementById('setting-ram-min'),
  settingRamMax:        document.getElementById('setting-ram-max'),
  ramMinValue:          document.getElementById('ram-min-value'),
  ramMaxValue:          document.getElementById('ram-max-value'),
  settingWidth:         document.getElementById('setting-width'),
  settingHeight:        document.getElementById('setting-height'),
  settingJava:          document.getElementById('setting-java'),
  saveSettingsBtn:      document.getElementById('btn-save-settings'),
  // Log
  logBody:              document.getElementById('log-body'),
  clearLogBtn:          document.getElementById('btn-clear-log')
};

// 현재 선택된 프리셋 ID
let currentPresetId = null;
// 현재 게임 버전 / 모드로더 (Modrinth 필터용)
let currentGameVersion = '1.21.1';
let currentModLoader = 'vanilla';

// ─── View helpers ─────────────────────────────────────────────
function showView(name) {
  Object.values(views).forEach(v => v.classList.remove('active'));
  views[name]?.classList.add('active');
}

function showPage(name) {
  Object.values(pages).forEach(p => p.classList.remove('active'));
  pages[name]?.classList.add('active');
  document.querySelectorAll('.nav-btn').forEach(b =>
    b.classList.toggle('active', b.dataset.view === name)
  );
  if (['mods', 'resourcepacks', 'shaderpacks'].includes(name)) {
    loadFilePage(name);
  }
}

// ─── Toast ────────────────────────────────────────────────────
function showToast(msg, type = 'error') {
  const c = document.getElementById('toast-container');
  const t = document.createElement('div');
  t.className = `toast ${type}`;
  t.textContent = msg;
  c.appendChild(t);
  setTimeout(() => { t.classList.add('fade-out'); setTimeout(() => t.remove(), 300); }, 3500);
}

// ─── Setup Progress ───────────────────────────────────────────
function setSetupStatus(message, percent) {
  els.setupText.textContent = message;
  if (percent >= 0) els.setupBar.style.width = Math.min(percent, 100) + '%';
}

function hideSetupBox() { els.setupBox.style.display = 'none'; }
function showSetupBox()  { els.setupBox.style.display = 'block'; }

// ─── Auth ─────────────────────────────────────────────────────
function setProfile(name, uuid) {
  els.playerName.textContent = name;
  els.playerAvatar.src = `https://mc-heads.net/avatar/${uuid}/32`;
  showView('main');
}

els.loginBtn.addEventListener('click', async () => {
  els.loginBtn.disabled = true;
  els.loginBtn.textContent = '로그인 중...';
  const result = await window.launcher.login();
  if (result.success) {
    setProfile(result.name, result.uuid);
    showToast(`${result.name}님 환영합니다!`, 'success');
    runSetup();
  } else {
    showToast('로그인 실패: ' + (result.error || '알 수 없는 오류'));
  }
  els.loginBtn.disabled = false;
  els.loginBtn.textContent = 'Microsoft 계정으로 로그인';
});

els.logoutBtn.addEventListener('click', async () => {
  await window.launcher.logout();
  showView('login');
  showToast('로그아웃되었습니다.', 'info');
});

// ─── Setup Flow ───────────────────────────────────────────────
window.launcher.onSetupProgress(({ message, percent }) => {
  setSetupStatus(message, percent);
});

async function runSetup() {
  showSetupBox();
  setSetupStatus('설정 확인 중...', 2);
  els.playBtn.disabled = true;

  let manifest = null;

  try {
    const check = await window.launcher.checkUpdate(currentPresetId);
    if (check.manifest) {
      manifest = check.manifest;
      currentGameVersion = manifest.gameVersion || '1.21.1';
      currentModLoader = (manifest.modLoader?.type || 'vanilla').toLowerCase();
      els.gameVersion.textContent = currentGameVersion;
      updateModloaderBadge(manifest.modLoader);
    }
  } catch {}

  const result = await window.launcher.runSetup(manifest);

  if (result.success) {
    hideSetupBox();
    els.playBtn.disabled = false;
    if (manifest?.gameVersion) {
      els.gameVersion.textContent = manifest.gameVersion;
      updateModloaderBadge(manifest.modLoader);
    }
  } else {
    setSetupStatus('오류: ' + result.error, 100);
    els.setupBar.style.background = 'var(--red)';
    showToast('설정 실패: ' + result.error);
    els.playBtn.disabled = false;
  }
}

// ─── Game Launch ──────────────────────────────────────────────
let isLaunching = false;

function resetPlayButton() {
  isLaunching = false;
  els.playBtn.disabled = false;
  els.playBtn.innerHTML = '<span class="play-icon">&#9654;</span><span>플레이</span>';
  els.progressSection.classList.remove('visible');
}

els.playBtn.addEventListener('click', async () => {
  if (isLaunching) return;
  isLaunching = true;
  els.playBtn.disabled = true;
  els.playBtn.innerHTML = '<span>파일 확인 중...</span>';

  showSetupBox();
  setSetupStatus('게임 파일 확인 중...', 0);

  let manifest = null;
  try {
    const check = await window.launcher.checkUpdate(currentPresetId);
    if (check.manifest) {
      manifest = check.manifest;
      currentGameVersion = manifest.gameVersion || '1.21.1';
      currentModLoader = (manifest.modLoader?.type || 'vanilla').toLowerCase();
      els.gameVersion.textContent = currentGameVersion;
      updateModloaderBadge(manifest.modLoader);
    }
  } catch {}

  const setupResult = await window.launcher.runSetup(manifest);
  hideSetupBox();

  if (!setupResult.success) {
    showToast('파일 업데이트 실패: ' + setupResult.error, 'error');
    resetPlayButton();
    return;
  }

  els.playBtn.innerHTML = '<span>게임 준비 중...</span>';
  els.progressSection.classList.add('visible');
  els.progressFill.style.width = '0%';
  els.progressText.textContent = '게임 에셋 확인 중...';

  const result = await window.launcher.launch();
  if (result.success) {
    els.playBtn.innerHTML = '<span>게임 실행 중</span>';
    showToast('마인크래프트가 실행되었습니다.', 'success');
  } else {
    showToast('실행 실패: ' + (result.error || '오류'), 'error');
    resetPlayButton();
  }
});

window.launcher.onProgress(e => {
  if (e.total > 0) {
    const pct = Math.round((e.task / e.total) * 100);
    els.progressFill.style.width = pct + '%';
    els.progressText.textContent = `${e.type} (${e.task}/${e.total})`;
  }
});

window.launcher.onDownloadStatus(e => {
  if (e.total > 0) {
    const pct = Math.round((e.current / e.total) * 100);
    els.progressFill.style.width = pct + '%';
    els.progressText.textContent = `다운로드: ${e.name} (${pct}%)`;
  }
});

window.launcher.onGameClosed(code => {
  resetPlayButton();
  if (code === 0) showToast('게임이 종료되었습니다.', 'success');
  else if (code !== null) showToast(`게임 비정상 종료 (코드: ${code})`, 'error');
});

// ─── Game Log ─────────────────────────────────────────────────
window.launcher.onGameLog(e => {
  const text = typeof e === 'string' ? e : (e?.data || JSON.stringify(e));
  appendLog(text);
});

let _logAtBottom = true;
const _logScrollBtn = document.getElementById('log-scroll-btn');

els.logBody.addEventListener('scroll', () => {
  _logAtBottom = els.logBody.scrollHeight - els.logBody.scrollTop - els.logBody.clientHeight < 60;
  _logScrollBtn?.classList.toggle('visible', !_logAtBottom);
});

_logScrollBtn?.addEventListener('click', () => {
  els.logBody.scrollTop = els.logBody.scrollHeight;
});

function appendLog(text) {
  const line = document.createElement('div');
  line.className = 'log-line';
  const lower = text.toLowerCase();
  if (lower.includes('[warn]') || lower.includes('warning')) line.classList.add('warn');
  else if (lower.includes('[error]') || lower.includes('exception') || lower.includes('caused by')) line.classList.add('error');
  else if (lower.includes('[info]')) line.classList.add('info');
  line.textContent = text;
  els.logBody.appendChild(line);
  if (_logAtBottom) els.logBody.scrollTop = els.logBody.scrollHeight;
}

function updateModloaderBadge(ml) {
  const badge = document.getElementById('modloader-badge');
  if (!badge) return;
  const type = (ml?.type || 'vanilla').toLowerCase();
  const labels = { fabric: 'Fabric', forge: 'Forge', vanilla: 'Vanilla' };
  badge.textContent = labels[type] || (type.charAt(0).toUpperCase() + type.slice(1));
  badge.className = `modloader-badge ml-${type}`;
  badge.style.display = '';
}

els.clearLogBtn.addEventListener('click', () => { els.logBody.innerHTML = ''; });

// ─── Auto Updater ─────────────────────────────────────────────
const updateOverlay   = document.getElementById('update-overlay');
const updateTitle     = document.getElementById('update-title');
const updateVersion   = document.getElementById('update-version');
const updateFill      = document.getElementById('update-progress-fill');
const updateStatus    = document.getElementById('update-status');

window.launcher.onUpdaterAvailable(version => {
  updateVersion.textContent = `v${version}`;
  updateTitle.textContent   = '새 버전 다운로드 중...';
  updateStatus.textContent  = '준비 중...';
  updateFill.style.width    = '0%';
  updateOverlay.style.display = 'flex';
});

window.launcher.onUpdaterProgress(({ percent, speed }) => {
  updateFill.style.width   = percent + '%';
  updateStatus.textContent = `${percent}%  ·  ${speed} KB/s`;
});

window.launcher.onUpdaterDownloaded(version => {
  updateFill.style.width   = '100%';
  updateTitle.textContent  = '업데이트 설치 중...';
  updateStatus.textContent = '잠시 후 자동으로 재시작됩니다.';
});

// ─── File Pages ───────────────────────────────────────────────
async function loadFilePage(category) {
  const data = await window.launcher.listFiles(category);
  renderFileList(`server-${category}`, data.server, true);
  renderFileList(`user-${category}`, data.user, false, category);
}

function renderFileList(elId, files, isServer, category) {
  const el = document.getElementById(elId);
  if (!el) return;
  el.innerHTML = '';
  if (files.length === 0) {
    el.innerHTML = `<div class="file-list-empty">없음</div>`;
    return;
  }
  for (const name of files) {
    const item = document.createElement('div');
    item.className = 'file-item';
    item.innerHTML = `<span class="file-item-name" title="${name}">${name}</span>`;
    if (isServer) {
      item.innerHTML += `<span class="server-badge">서버</span>`;
    } else {
      const btn = document.createElement('button');
      btn.className = 'file-item-del';
      btn.title = '삭제';
      btn.textContent = '✕';
      btn.addEventListener('click', async () => {
        const r = await window.launcher.removeFile(category, name);
        if (r.success) loadFilePage(category);
        else showToast('삭제 실패: ' + r.error);
      });
      item.appendChild(btn);
    }
    el.appendChild(item);
  }
}

// 파일 추가 버튼
document.querySelectorAll('.btn-add-file').forEach(btn => {
  btn.addEventListener('click', async () => {
    const category = btn.dataset.category;
    const paths = await window.launcher.openFileDialog(category);
    if (paths.length === 0) return;
    const r = await window.launcher.addFiles(category, paths);
    if (r.success) { loadFilePage(category); showToast('파일이 추가되었습니다.', 'success'); }
    else showToast('추가 실패: ' + r.error);
  });
});

// 드래그 앤 드롭
document.querySelectorAll('.drop-zone').forEach(zone => {
  const category = zone.dataset.category;

  zone.addEventListener('dragover', e => {
    e.preventDefault();
    zone.classList.add('drag-over');
  });
  zone.addEventListener('dragleave', () => zone.classList.remove('drag-over'));
  zone.addEventListener('drop', async e => {
    e.preventDefault();
    zone.classList.remove('drag-over');
    const files = Array.from(e.dataTransfer.files);
    if (files.length === 0) return;
    const paths = files.map(f => window.launcher.getPathForFile(f)).filter(Boolean);
    if (paths.length === 0) return;
    const r = await window.launcher.addFiles(category, paths);
    if (r.success) { loadFilePage(category); showToast(`${paths.length}개 파일 추가됨`, 'success'); }
    else showToast('추가 실패: ' + r.error);
  });
});

// ─── Server Status ────────────────────────────────────────────

const _tooltip = document.createElement('div');
_tooltip.className = 'player-tooltip';
_tooltip.style.display = 'none';
document.body.appendChild(_tooltip);

async function refreshServerStatus() {
  const wrap = document.getElementById('server-status-wrap');
  const list = document.getElementById('server-list-status');
  if (!wrap || !list) return;
  let result;
  try { result = await window.launcher.getServerStatus(); } catch { return; }
  const servers = result.servers || [];
  if (servers.length === 0) { wrap.style.display = 'none'; return; }
  wrap.style.display = '';
  list.innerHTML = servers.map(s => {
    const isOnline = s.online;
    const playerStr = isOnline ? `${s.online_count}/${s.max_count}명` : '오프라인';
    const names = (s.sample || []).join('\n');
    return `<div class="server-card ${isOnline ? 'online' : 'offline'}">
      <span class="server-dot ${isOnline ? 'online' : 'offline'}"></span>
      <span class="server-card-name">${s.name || s.ip}</span>
      <span class="server-players ${isOnline && s.online_count > 0 ? 'has-players' : ''}" data-players="${names}">${playerStr}</span>
    </div>`;
  }).join('');
  list.querySelectorAll('.server-players[data-players]').forEach(el => {
    el.addEventListener('mouseenter', e => {
      const names = e.currentTarget.dataset.players?.trim();
      if (!names) return;
      _tooltip.innerHTML = names.split('\n').filter(n => n).map(n => `<div class="tooltip-player">${n}</div>`).join('') || '<div class="tooltip-player" style="color:#666">없음</div>';
      _tooltip.style.display = 'block';
      _positionTooltip(e);
    });
    el.addEventListener('mousemove', _positionTooltip);
    el.addEventListener('mouseleave', () => { _tooltip.style.display = 'none'; });
  });
}

function _positionTooltip(e) {
  const tw = _tooltip.offsetWidth, th = _tooltip.offsetHeight;
  let x = e.clientX + 12, y = e.clientY - th - 8;
  if (x + tw > window.innerWidth) x = e.clientX - tw - 8;
  if (y < 0) y = e.clientY + 16;
  _tooltip.style.left = x + 'px';
  _tooltip.style.top  = y + 'px';
}

// ─── Preset Selector ──────────────────────────────────────────

function initPresetDropdown() {
  const btn = document.getElementById('preset-dropdown-btn');
  const list = document.getElementById('preset-dropdown-list');
  if (!btn || !list) return;

  btn.onclick = (e) => {
    e.stopPropagation();
    list.classList.toggle('open');
  };

  document.addEventListener('click', () => list.classList.remove('open'));
}

async function loadPresets() {
  const presets = await window.launcher.listPresets();
  const settings = await window.launcher.getSettings();
  currentPresetId = settings.selectedPreset || presets[0]?.id;

  const selectedName = document.getElementById('preset-selected-name');
  const list = document.getElementById('preset-dropdown-list');
  if (!selectedName || !list) return;

  const current = presets.find(p => p.id === currentPresetId) || presets[0];
  selectedName.textContent = current?.name || '선택';

  list.innerHTML = '';
  for (const preset of presets) {
    const opt = document.createElement('div');
    opt.className = 'preset-option' + (preset.id === currentPresetId ? ' selected' : '');
    opt.textContent = preset.name;
    opt.addEventListener('click', async (e) => {
      e.stopPropagation();
      if (preset.id === currentPresetId) {
        list.classList.remove('open');
        return;
      }
      currentPresetId = preset.id;
      selectedName.textContent = preset.name;
      list.classList.remove('open');
      list.querySelectorAll('.preset-option').forEach(o =>
        o.classList.toggle('selected', o.textContent === preset.name)
      );
      // setSettings 호출 제거 — setup:run 내부에서 selectedPreset 저장
      await runSetup();
      refreshServerStatus();
    });
    list.appendChild(opt);
  }
}

// ─── Easy (Modrinth) Browser ──────────────────────────────────

let _easyCategory = 'mods';
let _easySearchTimer = null;
let _easyExpanded = null; // currently expanded result element

const _easyOverlay = document.getElementById('easy-overlay');
const _easyBody    = document.getElementById('easy-body');
const _easySearch  = document.getElementById('easy-search');
const _easyTitle   = document.getElementById('easy-modal-title');

const CATEGORY_LABELS = { mods: '모드', resourcepacks: '리소스팩', shaderpacks: '셰이더' };

document.querySelectorAll('.btn-browse-easy').forEach(btn => {
  btn.addEventListener('click', () => openEasyBrowser(btn.dataset.category));
});

document.getElementById('easy-close')?.addEventListener('click', closeEasyBrowser);

_easyOverlay?.addEventListener('click', e => {
  if (e.target === _easyOverlay) closeEasyBrowser();
});

_easySearch?.addEventListener('input', () => {
  clearTimeout(_easySearchTimer);
  _easySearchTimer = setTimeout(doEasySearch, 400);
});

function openEasyBrowser(category) {
  _easyCategory = category;
  _easyTitle.textContent = `${CATEGORY_LABELS[category] || category} 브라우저`;
  _easySearch.value = '';
  _easyBody.innerHTML = '<div class="easy-placeholder">검색어를 입력하세요</div>';
  _easyExpanded = null;
  _easyOverlay.style.display = 'flex';
  setTimeout(() => _easySearch.focus(), 50);
}

function closeEasyBrowser() {
  _easyOverlay.style.display = 'none';
}

async function doEasySearch() {
  const query = _easySearch.value.trim();
  if (!query) {
    _easyBody.innerHTML = '<div class="easy-placeholder">검색어를 입력하세요</div>';
    return;
  }
  _easyBody.innerHTML = '<div class="easy-loading"><span class="easy-spinner"></span>검색 중...</div>';
  _easyExpanded = null;

  const r = await window.launcher.modrinthSearch({
    query,
    category: _easyCategory,
    gameVersion: currentGameVersion,
    loader: currentModLoader
  });

  if (!r.success) {
    _easyBody.innerHTML = `<div class="easy-placeholder">오류: ${r.error}</div>`;
    return;
  }
  if (r.hits.length === 0) {
    _easyBody.innerHTML = '<div class="easy-placeholder">결과 없음</div>';
    return;
  }

  _easyBody.innerHTML = '';
  for (const hit of r.hits) {
    _easyBody.appendChild(createEasyResult(hit));
  }
}

function createEasyResult(hit) {
  const dlCount = hit.downloads >= 1000000
    ? (hit.downloads / 1000000).toFixed(1) + 'M'
    : hit.downloads >= 1000
      ? (hit.downloads / 1000).toFixed(0) + 'K'
      : hit.downloads;

  const item = document.createElement('div');
  item.className = 'easy-result';
  item.innerHTML = `
    <div class="easy-result-row">
      <img class="easy-result-icon" src="${hit.icon_url || ''}" alt="" onerror="this.style.visibility='hidden'">
      <div class="easy-result-info">
        <div class="easy-result-name">${hit.title}</div>
        <div class="easy-result-meta">${hit.author} · ${dlCount} ↓</div>
        <div class="easy-result-desc">${hit.description}</div>
      </div>
      <span class="easy-result-arrow">▾</span>
    </div>
    <div class="easy-versions"></div>
  `;

  item.addEventListener('click', async () => {
    if (_easyExpanded && _easyExpanded !== item) {
      _easyExpanded.classList.remove('expanded');
    }
    item.classList.toggle('expanded');
    _easyExpanded = item.classList.contains('expanded') ? item : null;

    if (item.classList.contains('expanded')) {
      await loadEasyVersions(item, hit.project_id);
    }
  });

  return item;
}

async function loadEasyVersions(resultEl, projectId) {
  const versionsEl = resultEl.querySelector('.easy-versions');
  if (versionsEl.dataset.loaded) return;
  versionsEl.dataset.loaded = '1';
  versionsEl.innerHTML = '<div class="easy-ver-loading"><span class="easy-spinner"></span>버전 로딩 중...</div>';

  const r = await window.launcher.modrinthVersions({
    projectId,
    gameVersion: currentGameVersion,
    loader: currentModLoader,
    category: _easyCategory
  });

  if (!r.success || r.versions.length === 0) {
    versionsEl.innerHTML = '<div class="easy-ver-empty">호환 버전 없음</div>';
    return;
  }

  versionsEl.innerHTML = '';
  for (const ver of r.versions) {
    const primaryFile = ver.files?.find(f => f.primary) || ver.files?.[0];
    if (!primaryFile) continue;

    const row = document.createElement('div');
    row.className = 'easy-ver-row';

    const nameSpan = document.createElement('span');
    nameSpan.className = 'easy-ver-name';
    const gv = ver.game_versions?.slice(-1)[0] || '';
    nameSpan.textContent = (ver.name || ver.version_number) + (gv ? `  [${gv}]` : '');

    const installBtn = document.createElement('button');
    installBtn.className = 'easy-ver-install';
    installBtn.textContent = '설치';

    installBtn.addEventListener('click', async (e) => {
      e.stopPropagation();
      installBtn.disabled = true;
      installBtn.textContent = '설치 중...';
      const res = await window.launcher.modrinthDownload({
        url: primaryFile.url,
        filename: primaryFile.filename,
        category: _easyCategory
      });
      if (res.success) {
        installBtn.textContent = '완료 ✓';
        showToast(`${primaryFile.filename} 설치됨`, 'success');
        loadFilePage(_easyCategory);
      } else {
        installBtn.disabled = false;
        installBtn.textContent = '설치';
        showToast('설치 실패: ' + res.error, 'error');
      }
    });

    row.appendChild(nameSpan);
    row.appendChild(installBtn);
    versionsEl.appendChild(row);
  }
}

// ─── Navigation ───────────────────────────────────────────────
document.querySelectorAll('.nav-btn').forEach(btn => {
  btn.addEventListener('click', () => {
    showPage(btn.dataset.view);
    if (btn.dataset.view === 'home') refreshServerStatus();
  });
});

// ─── Settings ─────────────────────────────────────────────────
async function loadSettings() {
  const s = await window.launcher.getSettings();
  els.settingRamMin.value     = s.ram?.min || 2;
  els.settingRamMax.value     = s.ram?.max || 4;
  els.ramMinValue.textContent = s.ram?.min || 2;
  els.ramMaxValue.textContent = s.ram?.max || 4;
  els.settingWidth.value      = s.resolution?.width || 1280;
  els.settingHeight.value     = s.resolution?.height || 720;
  els.settingJava.value       = s.javaPath || '';

  const verEl = document.getElementById('launcher-version');
  if (verEl) {
    const v = await window.launcher.getVersion();
    verEl.textContent = `런처 버전 v${v}`;
  }
}

els.settingRamMin.addEventListener('input', () => {
  els.ramMinValue.textContent = els.settingRamMin.value;
  if (+els.settingRamMin.value > +els.settingRamMax.value) {
    els.settingRamMax.value = els.settingRamMin.value;
    els.ramMaxValue.textContent = els.settingRamMax.value;
  }
});
els.settingRamMax.addEventListener('input', () => {
  els.ramMaxValue.textContent = els.settingRamMax.value;
  if (+els.settingRamMax.value < +els.settingRamMin.value) {
    els.settingRamMin.value = els.settingRamMax.value;
    els.ramMinValue.textContent = els.settingRamMin.value;
  }
});

document.getElementById('btn-open-game-folder')?.addEventListener('click', async () => {
  await window.launcher.openGameFolder();
});

els.saveSettingsBtn.addEventListener('click', async () => {
  const s = {
    ram:        { min: +els.settingRamMin.value, max: +els.settingRamMax.value },
    resolution: { width: +els.settingWidth.value || 1280, height: +els.settingHeight.value || 720 },
    javaPath:   els.settingJava.value.trim()
  };
  await window.launcher.setSettings(s);
  showToast('저장되었습니다.', 'success');
});

// ─── Window controls ──────────────────────────────────────────
document.getElementById('btn-minimize').addEventListener('click', () => window.launcher.minimize());
document.getElementById('btn-close').addEventListener('click',    () => window.launcher.close());

// ─── Init ─────────────────────────────────────────────────────
async function init() {
  await loadSettings();
  initPresetDropdown();
  await loadPresets();
  const auth = await window.launcher.checkAuth();
  if (auth.loggedIn) {
    setProfile(auth.name, auth.uuid);
    await runSetup();
    refreshServerStatus();
    setInterval(refreshServerStatus, 30000);
  }
}

init();
