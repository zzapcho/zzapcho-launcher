const { app, BrowserWindow, ipcMain, net, dialog, shell } = require('electron');
const path = require('path');
const fs = require('fs');
const crypto = require('crypto');

let mainWindow;

const DATA_PATH = path.join(app.getPath('appData'), 'zzapchoLauncher');
const GAME_PATH = path.join(DATA_PATH, 'minecraft');
const SETTINGS_FILE = path.join(DATA_PATH, 'settings.json');
const AUTH_FILE = path.join(DATA_PATH, 'auth.json');
const USER_FILES_PATH = path.join(DATA_PATH, 'user_files'); // 프리셋별 사용자 파일 저장소

function getManifestCacheFile(presetId) {
  return path.join(DATA_PATH, 'manifest_' + (presetId || 'default') + '.json');
}

// 앱 루트의 launcher.config.json에서 설정 로드
const CONFIG = (() => {
  try { return JSON.parse(fs.readFileSync(path.join(__dirname, 'launcher.config.json'), 'utf-8')); }
  catch { return {}; }
})();

const DEFAULT_SETTINGS = {
  ram: { min: 2, max: 4 },
  resolution: { width: 1280, height: 720 },
  javaPath: '',
  selectedPreset: CONFIG.presets?.[0]?.id || 'mcserver1'
};

const VALID_FILE_CATS = ['mods', 'resourcepacks', 'shaderpacks'];

// ─── Utilities ───────────────────────────────────────────────

function ensureDirs() {
  [DATA_PATH, GAME_PATH, USER_FILES_PATH].forEach(d => { if (!fs.existsSync(d)) fs.mkdirSync(d, { recursive: true }); });
}

// 사용자 파일 스태시 경로
function getUserStashDir(presetId, category) {
  return path.join(USER_FILES_PATH, presetId, category);
}

// 현재 게임 디렉토리의 사용자 파일(서버 파일 제외)을 스태시에 저장
function stashUserFiles(presetId, manifest) {
  for (const cat of VALID_FILE_CATS) {
    const serverNames = new Set(
      (manifest?.files || [])
        .filter(f => f.path.startsWith(cat + '/'))
        .map(f => path.basename(f.path))
    );
    const dirPath = path.join(GAME_PATH, cat);
    if (!fs.existsSync(dirPath)) continue;
    const stashDir = getUserStashDir(presetId, cat);
    fs.mkdirSync(stashDir, { recursive: true });
    // 기존 스태시 초기화 후 현재 사용자 파일 저장
    try { fs.readdirSync(stashDir).forEach(f => fs.unlinkSync(path.join(stashDir, f))); } catch {}
    for (const f of fs.readdirSync(dirPath)) {
      if (!serverNames.has(f) && !f.endsWith('.tmp')) {
        try { fs.copyFileSync(path.join(dirPath, f), path.join(stashDir, f)); } catch {}
      }
    }
  }
}

// 게임 디렉토리의 mods/resourcepacks/shaderpacks 전체 초기화
function wipeGameFileDirs() {
  for (const cat of VALID_FILE_CATS) {
    const dirPath = path.join(GAME_PATH, cat);
    if (!fs.existsSync(dirPath)) continue;
    for (const f of fs.readdirSync(dirPath)) {
      try { fs.unlinkSync(path.join(dirPath, f)); } catch {}
    }
  }
}

// 스태시에서 게임 디렉토리로 사용자 파일 복원
function restoreUserFiles(presetId) {
  for (const cat of VALID_FILE_CATS) {
    const stashDir = getUserStashDir(presetId, cat);
    if (!fs.existsSync(stashDir)) continue;
    const destDir = path.join(GAME_PATH, cat);
    fs.mkdirSync(destDir, { recursive: true });
    for (const f of fs.readdirSync(stashDir)) {
      try { fs.copyFileSync(path.join(stashDir, f), path.join(destDir, f)); } catch {}
    }
  }
}

function readJson(filePath, fallback) {
  try { return JSON.parse(fs.readFileSync(filePath, 'utf-8')); }
  catch { return fallback; }
}

function writeJson(filePath, data) {
  fs.writeFileSync(filePath, JSON.stringify(data, null, 2));
}

function fetchJson(url) {
  return new Promise((resolve, reject) => {
    const req = net.request({ url, method: 'GET' });
    let chunks = [];
    req.on('response', res => {
      if (res.statusCode !== 200) return reject(new Error(`HTTP ${res.statusCode}: ${url}`));
      res.on('data', c => chunks.push(c));
      res.on('end', () => {
        try { resolve(JSON.parse(Buffer.concat(chunks).toString('utf-8'))); }
        catch { reject(new Error('JSON 파싱 실패')); }
      });
    });
    req.on('error', reject);
    req.end();
  });
}

function downloadFile(url, destPath, onProgress) {
  return new Promise((resolve, reject) => {
    fs.mkdirSync(path.dirname(destPath), { recursive: true });
    const tmp = destPath + '.tmp';
    const fileStream = fs.createWriteStream(tmp);
    const req = net.request({ url, method: 'GET' });
    req.on('response', res => {
      if (res.statusCode !== 200) { fileStream.destroy(); return reject(new Error(`HTTP ${res.statusCode}`)); }
      const total = parseInt(res.headers['content-length'] || '0', 10);
      let current = 0;
      res.on('data', chunk => { current += chunk.length; fileStream.write(chunk); if (onProgress && total) onProgress(current, total); });
      res.on('end', () => fileStream.end(() => {
        try { if (fs.existsSync(destPath)) fs.unlinkSync(destPath); fs.renameSync(tmp, destPath); resolve(); }
        catch (e) { reject(e); }
      }));
      res.on('error', e => { fileStream.destroy(); try { fs.unlinkSync(tmp); } catch {} reject(e); });
    });
    req.on('error', e => { fileStream.destroy(); try { fs.unlinkSync(tmp); } catch {} reject(e); });
    req.end();
  });
}

function fileMd5(filePath) {
  return new Promise((resolve, reject) => {
    if (!fs.existsSync(filePath)) return resolve(null);
    const hash = crypto.createHash('md5');
    const stream = fs.createReadStream(filePath);
    stream.on('data', d => hash.update(d));
    stream.on('end', () => resolve(hash.digest('hex')));
    stream.on('error', reject);
  });
}

function sendSetupProgress(message, percent = -1) {
  mainWindow?.webContents.send('setup:progress', { message, percent });
}

// ─── Window ──────────────────────────────────────────────────

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 900, height: 560, resizable: false, frame: false,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false
    }
  });
  mainWindow.loadFile(path.join(__dirname, 'src', 'index.html'));
}

// ─── Auth ────────────────────────────────────────────────────

let mcToken = null;

ipcMain.handle('auth:login', async () => {
  try {
    const { Auth } = require('msmc');
    const auth = new Auth('select_account');
    const xbox = await auth.launch('electron');
    const mc = await xbox.getMinecraft();
    const token = mc.mclc();
    mcToken = token;
    writeJson(AUTH_FILE, { token, refreshToken: xbox.save() });
    return { success: true, name: token.name, uuid: token.uuid };
  } catch (e) { return { success: false, error: e.message }; }
});

ipcMain.handle('auth:check', async () => {
  try {
    const data = readJson(AUTH_FILE, null);
    if (!data?.token) return { loggedIn: false };
    if (data.refreshToken) {
      try {
        const { Auth } = require('msmc');
        const xbox = await new Auth('select_account').refresh(data.refreshToken);
        const token = (await xbox.getMinecraft()).mclc();
        mcToken = token;
        writeJson(AUTH_FILE, { token, refreshToken: xbox.save() });
        return { loggedIn: true, name: token.name, uuid: token.uuid };
      } catch {}
    }
    mcToken = data.token;
    return { loggedIn: true, name: data.token.name, uuid: data.token.uuid };
  } catch { return { loggedIn: false }; }
});

ipcMain.handle('auth:logout', () => {
  mcToken = null;
  if (fs.existsSync(AUTH_FILE)) fs.unlinkSync(AUTH_FILE);
  return { success: true };
});

// ─── Presets ─────────────────────────────────────────────────

ipcMain.handle('presets:list', () => CONFIG.presets || []);

// ─── Setup (Java + ModLoader + Files + Servers) ──────────────

ipcMain.handle('setup:run', async (_, manifest) => {
  const { detectJava, downloadJava } = require('./lib/java');
  const modloader = require('./lib/modloader');
  const { writeServersDat } = require('./lib/servers');

  const presetId = manifest?._presetId;
  const MANIFEST_CACHE = getManifestCacheFile(presetId || 'default');

  const settings = readJson(SETTINGS_FILE, DEFAULT_SETTINGS);
  const previousPresetId = settings.selectedPreset;

  // 프리셋 전환 처리
  if (presetId && previousPresetId && presetId !== previousPresetId) {
    // 1. 현재 사용자 파일을 이전 프리셋 스태시에 저장
    const oldManifest = readJson(getManifestCacheFile(previousPresetId), null);
    stashUserFiles(previousPresetId, oldManifest);

    // 2. mods/resourcepacks/shaderpacks 전체 초기화
    wipeGameFileDirs();

    // 3. 새 프리셋의 사용자 파일 복원
    restoreUserFiles(presetId);

    // 4. 새 프리셋의 서버 파일 강제 재다운로드를 위해 캐시 삭제
    const newCache = getManifestCacheFile(presetId);
    try { if (fs.existsSync(newCache)) fs.unlinkSync(newCache); } catch {}
  }

  // 현재 선택된 프리셋 저장
  if (presetId) {
    writeJson(SETTINGS_FILE, { ...settings, selectedPreset: presetId });
  }

  try {
    // ── 1. Java ──────────────────────────────────────────────
    sendSetupProgress('Java 확인 중...', 5);
    let javaPath = detectJava(DATA_PATH, settings.javaPath);

    if (!javaPath) {
      sendSetupProgress('Java가 없습니다. 자동 설치 중...', 10);
      javaPath = await downloadJava(DATA_PATH, (msg, curr, total) => {
        const pct = total > 0 ? Math.round(10 + (curr / total) * 25) : 15;
        sendSetupProgress(msg, pct);
      });
    }

    writeJson(SETTINGS_FILE, { ...readJson(SETTINGS_FILE, DEFAULT_SETTINGS), javaPath });

    // ── 2. 모드 로더 ─────────────────────────────────────────
    let versionId = null;
    const ml = manifest?.modLoader;
    const gameVersion = manifest?.gameVersion || '1.21.1';

    if (ml && ml.type !== 'vanilla') {
      if (ml.type === 'fabric') {
        sendSetupProgress('Fabric 확인 중...', 40);
        if (!modloader.isFabricInstalled(GAME_PATH, gameVersion)) {
          versionId = await modloader.installFabric(GAME_PATH, gameVersion, msg => sendSetupProgress(msg, 45));
        } else {
          versionId = modloader.getInstalledFabricId(GAME_PATH, gameVersion);
        }
      } else if (ml.type === 'forge') {
        sendSetupProgress('Forge 확인 중...', 40);
        if (!modloader.isForgeInstalled(GAME_PATH, gameVersion)) {
          versionId = await modloader.installForge(GAME_PATH, gameVersion, javaPath, DATA_PATH, msg => sendSetupProgress(msg, 45));
        } else {
          versionId = modloader.getInstalledForgeId(GAME_PATH, gameVersion);
        }
      }
    }

    // ── 3. 파일 업데이트 ──────────────────────────────────────
    sendSetupProgress('파일 업데이트 확인 중...', 55);
    const localManifest = readJson(MANIFEST_CACHE, null);

    if (manifest && (localManifest?.version !== manifest.version)) {
      const filesToDownload = [];
      for (const file of manifest.files || []) {
        const safePath = path.normalize(file.path).replace(/^(\.\.[\\/])+/, '');
        const localPath = path.join(GAME_PATH, safePath);
        const localMd5 = await fileMd5(localPath);
        if (localMd5 !== file.md5) filesToDownload.push({ ...file, path: safePath });
      }

      const filesToDelete = [];
      if (localManifest) {
        const newPaths = new Set((manifest.files || []).map(f => f.path));
        for (const f of localManifest.files || []) {
          if (!newPaths.has(f.path)) filesToDelete.push(f.path);
        }
      }

      const total = filesToDownload.length;
      for (let i = 0; i < filesToDownload.length; i++) {
        const file = filesToDownload[i];
        const pct = Math.round(55 + (i / total) * 35);
        sendSetupProgress(`다운로드 중: ${path.basename(file.path)} (${i + 1}/${total})`, pct);
        await downloadFile(file.url, path.join(GAME_PATH, file.path), (curr, tot) => {
          const p = Math.round(55 + ((i + curr / tot) / total) * 35);
          sendSetupProgress(`다운로드 중: ${path.basename(file.path)}`, p);
        });
      }

      for (const fp of filesToDelete) {
        const lp = path.join(GAME_PATH, fp);
        if (fs.existsSync(lp)) fs.unlinkSync(lp);
      }

      writeJson(MANIFEST_CACHE, manifest);
    }

    // ── 4. 서버 목록 ─────────────────────────────────────────
    // manifest 서버는 항상 최신으로, 사용자 추가 서버는 유지
    const serverSource = manifest || localManifest;
    if (serverSource?.servers !== undefined) {
      sendSetupProgress('서버 목록 설정 중...', 92);
      // 이전 manifest 서버도 넘겨서 교체 시 깔끔하게 제거
      writeServersDat(GAME_PATH, serverSource.servers || [], localManifest?.servers || []);
    }

    sendSetupProgress('준비 완료!', 100);
    return { success: true, javaPath, versionId, gameVersion };
  } catch (e) {
    return { success: false, error: e.message };
  }
});

// ─── Update Check ─────────────────────────────────────────────

ipcMain.handle('update:check', async (_, presetId) => {
  try {
    const settings = readJson(SETTINGS_FILE, DEFAULT_SETTINGS);
    const pid = presetId || settings.selectedPreset || CONFIG.presets?.[0]?.id;
    const preset = (CONFIG.presets || []).find(p => p.id === pid) || CONFIG.presets?.[0];
    if (!preset?.manifestUrl) return { skipped: true };

    const manifest = await fetchJson(preset.manifestUrl);
    const local = readJson(getManifestCacheFile(pid), null);

    return {
      hasUpdate: !local || local.version !== manifest.version,
      currentVersion: local?.version || null,
      newVersion: manifest.version,
      manifest: { ...manifest, _presetId: pid }
    };
  } catch (e) {
    const settings = readJson(SETTINGS_FILE, DEFAULT_SETTINGS);
    const pid = presetId || settings.selectedPreset || CONFIG.presets?.[0]?.id;
    return { hasUpdate: false, error: e.message, manifest: readJson(getManifestCacheFile(pid), null) };
  }
});

// ─── Game Launch ─────────────────────────────────────────────

ipcMain.handle('game:launch', async () => {
  if (!mcToken) return { success: false, error: '로그인이 필요합니다.' };

  try {
    const { Client } = require('minecraft-launcher-core');
    const settings = readJson(SETTINGS_FILE, DEFAULT_SETTINGS);
    const presetId = settings.selectedPreset || CONFIG.presets?.[0]?.id;
    const localManifest = readJson(getManifestCacheFile(presetId), null);
    const launcher = new Client();

    launcher.on('progress', e => mainWindow?.webContents.send('game:progress', e));
    launcher.on('download-status', e => mainWindow?.webContents.send('game:download-status', e));
    launcher.on('close', code => mainWindow?.webContents.send('game:closed', code));
    launcher.on('data', e => mainWindow?.webContents.send('game:log', e));

    const { writeServersDat: wsd } = require('./lib/servers');
    const mf = localManifest;
    // 게임 실행 직전 — manifest 서버 유지하되 사용자 추가 서버 보존
    if (mf?.servers !== undefined) wsd(GAME_PATH, mf.servers || [], mf.servers || []);

    const gameVersion = localManifest?.gameVersion || '1.21.1';
    const modloader = require('./lib/modloader');
    const ml = localManifest?.modLoader;
    let customVersion = null;
    if (ml?.type === 'fabric') customVersion = modloader.getInstalledFabricId(GAME_PATH, gameVersion);
    if (ml?.type === 'forge')  customVersion = modloader.getInstalledForgeId(GAME_PATH, gameVersion);

    const opts = {
      authorization: mcToken,
      root: GAME_PATH,
      version: customVersion
        ? { number: gameVersion, type: 'release', custom: customVersion }
        : { number: gameVersion, type: 'release' },
      memory: { max: (settings.ram?.max || 4) + 'G', min: (settings.ram?.min || 2) + 'G' },
      window: {
        width:  settings.resolution?.width  || 1280,
        height: settings.resolution?.height || 720,
        fullscreen: false
      },
      overrides: {
        gameDirectory: GAME_PATH
      }
    };

    if (settings.javaPath) opts.javaPath = settings.javaPath;

    await launcher.launch(opts);
    return { success: true };
  } catch (e) { return { success: false, error: e.message }; }
});

// ─── File Management ─────────────────────────────────────────

ipcMain.handle('files:list', async (_, category) => {
  if (!VALID_FILE_CATS.includes(category)) return { server: [], user: [] };

  const settings = readJson(SETTINGS_FILE, DEFAULT_SETTINGS);
  const presetId = settings.selectedPreset || CONFIG.presets?.[0]?.id;
  const localManifest = readJson(getManifestCacheFile(presetId), null);
  const prefix = category + '/';
  const serverNames = new Set(
    (localManifest?.files || [])
      .filter(f => f.path.startsWith(prefix))
      .map(f => path.basename(f.path))
  );

  const dirPath = path.join(GAME_PATH, category);
  let allFiles = [];
  if (fs.existsSync(dirPath)) {
    allFiles = fs.readdirSync(dirPath).filter(f => {
      try { return fs.statSync(path.join(dirPath, f)).isFile() && !f.endsWith('.tmp'); }
      catch { return false; }
    });
  }

  return {
    server: allFiles.filter(f => serverNames.has(f)),
    user: allFiles.filter(f => !serverNames.has(f))
  };
});

ipcMain.handle('files:add', async (_, category, filePaths) => {
  if (!VALID_FILE_CATS.includes(category)) return { success: false, error: '잘못된 카테고리' };

  try {
    const settings = readJson(SETTINGS_FILE, DEFAULT_SETTINGS);
    const presetId = settings.selectedPreset || CONFIG.presets?.[0]?.id;
    const destDir = path.join(GAME_PATH, category);
    const stashDir = getUserStashDir(presetId, category);
    fs.mkdirSync(destDir, { recursive: true });
    fs.mkdirSync(stashDir, { recursive: true });
    for (const src of filePaths) {
      if (!src || !fs.existsSync(src)) continue;
      const name = path.basename(src);
      if (!name) continue;
      fs.copyFileSync(src, path.join(destDir, name));
      fs.copyFileSync(src, path.join(stashDir, name)); // 스태시에도 저장
    }
    return { success: true };
  } catch (e) {
    return { success: false, error: e.message };
  }
});

ipcMain.handle('files:remove', async (_, category, fileName) => {
  if (!VALID_FILE_CATS.includes(category)) return { success: false, error: '잘못된 카테고리' };
  if (!fileName || fileName.includes('/') || fileName.includes('\\') || fileName.includes('..')) {
    return { success: false, error: '잘못된 파일명' };
  }

  const settings = readJson(SETTINGS_FILE, DEFAULT_SETTINGS);
  const presetId = settings.selectedPreset || CONFIG.presets?.[0]?.id;
  const localManifest = readJson(getManifestCacheFile(presetId), null);
  const serverNames = new Set(
    (localManifest?.files || [])
      .filter(f => f.path.startsWith(category + '/'))
      .map(f => path.basename(f.path))
  );
  if (serverNames.has(fileName)) return { success: false, error: '서버 파일은 삭제할 수 없습니다.' };

  try {
    const filePath = path.join(GAME_PATH, category, fileName);
    if (fs.existsSync(filePath)) fs.unlinkSync(filePath);
    // 스태시에서도 삭제
    const stashPath = path.join(getUserStashDir(presetId, category), fileName);
    try { if (fs.existsSync(stashPath)) fs.unlinkSync(stashPath); } catch {}
    return { success: true };
  } catch (e) {
    return { success: false, error: e.message };
  }
});

ipcMain.handle('files:open-dialog', async (_, category) => {
  const filters = {
    mods: [{ name: 'Mod Files', extensions: ['jar'] }],
    resourcepacks: [{ name: 'ResourcePack Files', extensions: ['zip'] }],
    shaderpacks: [{ name: 'Shader Files', extensions: ['zip', 'jar'] }]
  };
  const result = await dialog.showOpenDialog(mainWindow, {
    properties: ['openFile', 'multiSelections'],
    filters: [
      filters[category] || { name: 'All Files', extensions: ['*'] },
      { name: 'All Files', extensions: ['*'] }
    ]
  });
  return result.canceled ? [] : result.filePaths;
});

// ─── Server Status Ping (Minecraft SLP) ──────────────────────

function pingMinecraftServer(host, port) {
  port = port || 25565;
  return new Promise((resolve) => {
    const socket = require('net').createConnection({ host, port });
    let buf = Buffer.alloc(0);
    let settled = false;

    const finish = (r) => {
      if (settled) return;
      settled = true;
      socket.destroy();
      resolve(r);
    };

    socket.setTimeout(5000);
    socket.on('timeout', () => finish({ online: false }));
    socket.on('error',   () => finish({ online: false }));

    socket.on('connect', () => {
      const vi = (n) => {
        const b = [];
        do { let x = n & 0x7f; n >>>= 7; if (n) x |= 0x80; b.push(x); } while (n);
        return Buffer.from(b);
      };
      const str = (s) => { const b = Buffer.from(s, 'utf8'); return Buffer.concat([vi(b.length), b]); };
      const pkt = (id, ...d) => { const body = Buffer.concat([vi(id), ...d]); return Buffer.concat([vi(body.length), body]); };
      const portBuf = Buffer.allocUnsafe(2); portBuf.writeUInt16BE(port);

      socket.write(Buffer.concat([
        pkt(0x00, vi(0), str(host), portBuf, vi(1)),
        pkt(0x00)
      ]));
    });

    socket.on('data', chunk => {
      buf = Buffer.concat([buf, chunk]);
      try {
        let o = 0;
        const rv = () => { let v = 0, s = 0, b; do { b = buf[o++]; v |= (b & 0x7f) << s; s += 7; } while (b & 0x80); return v; };
        const pktLen = rv();
        if (buf.length < o + pktLen) return;
        rv();
        const sLen = rv();
        const json = JSON.parse(buf.slice(o, o + sLen).toString('utf8'));
        finish({
          online:        true,
          online_count:  json.players?.online  ?? 0,
          max_count:     json.players?.max     ?? 0,
          sample:       (json.players?.sample || []).map(p => p.name)
        });
      } catch {}
    });
  });
}

ipcMain.handle('server:status', async () => {
  const settings = readJson(SETTINGS_FILE, DEFAULT_SETTINGS);
  const presetId = settings.selectedPreset || CONFIG.presets?.[0]?.id;
  const manifest = readJson(getManifestCacheFile(presetId), null);
  const servers = manifest?.servers;
  if (!servers?.length) return { servers: [] };

  const results = await Promise.all(servers.map(async s => {
    const addr = s.ip || '';
    const colonIdx = addr.lastIndexOf(':');
    let host = addr, port = s.port || 25565;
    if (colonIdx > 0) {
      const p = parseInt(addr.slice(colonIdx + 1));
      if (!isNaN(p)) { host = addr.slice(0, colonIdx); port = p; }
    }
    const status = await pingMinecraftServer(host, port);
    return { name: s.name, ip: addr, ...status };
  }));

  return { servers: results };
});

// ─── Game Folder ─────────────────────────────────────────────

ipcMain.handle('folder:open-game', async () => {
  if (!fs.existsSync(GAME_PATH)) fs.mkdirSync(GAME_PATH, { recursive: true });
  await shell.openPath(GAME_PATH);
  return { success: true };
});

// ─── Settings ────────────────────────────────────────────────

ipcMain.handle('settings:get', () => ({ ...DEFAULT_SETTINGS, ...readJson(SETTINGS_FILE, {}) }));
ipcMain.handle('settings:set', (_, s) => {
  const existing = readJson(SETTINGS_FILE, {});
  writeJson(SETTINGS_FILE, { ...existing, ...s });
  return { success: true };
});

// ─── Window ──────────────────────────────────────────────────

ipcMain.on('window:minimize', () => mainWindow?.minimize());
ipcMain.on('window:close',   () => mainWindow?.close());

// ─── Auto Updater ────────────────────────────────────────────

function initAutoUpdater() {
  try {
    const { autoUpdater } = require('electron-updater');
    autoUpdater.autoDownload = true;
    autoUpdater.autoInstallOnAppQuit = false;
    autoUpdater.logger = null;

    autoUpdater.on('update-available', info => {
      mainWindow?.webContents.send('updater:available', info.version);
    });
    autoUpdater.on('download-progress', p => {
      mainWindow?.webContents.send('updater:progress', {
        percent: Math.round(p.percent || 0),
        speed: Math.round((p.bytesPerSecond || 0) / 1024)
      });
    });
    autoUpdater.on('update-downloaded', info => {
      mainWindow?.webContents.send('updater:downloaded', info.version);
      setTimeout(() => autoUpdater.quitAndInstall(true, true), 1500);
    });
    autoUpdater.on('error', e => console.log('[updater]', e?.message));

    autoUpdater.checkForUpdates().catch(e => console.log('[updater] check failed:', e?.message));
  } catch (e) {
    console.log('[updater] init failed:', e.message);
  }
}

// ─── App Lifecycle ───────────────────────────────────────────

app.whenReady().then(() => {
  ensureDirs();
  createWindow();
  if (app.isPackaged) {
    setTimeout(initAutoUpdater, 2000);
  }
});
app.on('window-all-closed', () => app.quit());
