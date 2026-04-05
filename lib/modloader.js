/**
 * Fabric / Forge 자동 설치
 */

const { net } = require('electron');
const { spawn } = require('child_process');
const path = require('path');
const fs = require('fs');

// ─── 공통 네트워크 유틸 ──────────────────────────────────────

function fetchJson(url) {
  return new Promise((resolve, reject) => {
    const req = net.request({ url, method: 'GET' });
    let chunks = [];
    req.on('response', res => {
      if (res.statusCode !== 200) return reject(new Error(`HTTP ${res.statusCode}: ${url}`));
      res.on('data', c => chunks.push(c));
      res.on('end', () => {
        try { resolve(JSON.parse(Buffer.concat(chunks).toString('utf-8'))); }
        catch (e) { reject(new Error('JSON 파싱 실패: ' + url)); }
      });
    });
    req.on('error', reject);
    req.end();
  });
}

function downloadFile(url, dest, onProgress) {
  return new Promise((resolve, reject) => {
    fs.mkdirSync(path.dirname(dest), { recursive: true });
    const tmp = dest + '.tmp';
    const file = fs.createWriteStream(tmp);
    const req = net.request({ url, method: 'GET' });
    req.on('response', res => {
      if (res.statusCode !== 200) { file.destroy(); return reject(new Error(`HTTP ${res.statusCode}`)); }
      const total = parseInt(res.headers['content-length'] || '0', 10);
      let current = 0;
      res.on('data', chunk => { current += chunk.length; file.write(chunk); onProgress && total && onProgress(current, total); });
      res.on('end', () => file.end(() => {
        try { if (fs.existsSync(dest)) fs.unlinkSync(dest); fs.renameSync(tmp, dest); resolve(); }
        catch (e) { reject(e); }
      }));
      res.on('error', e => { file.destroy(); reject(e); });
    });
    req.on('error', e => { file.destroy(); reject(e); });
    req.end();
  });
}

// ─── Fabric ──────────────────────────────────────────────────

function getInstalledFabricId(gamePath, mcVersion) {
  const versions = path.join(gamePath, 'versions');
  if (!fs.existsSync(versions)) return null;
  const found = fs.readdirSync(versions)
    .find(v => v.startsWith('fabric-loader-') && v.endsWith(`-${mcVersion}`));
  return found || null;
}

function isFabricInstalled(gamePath, mcVersion) {
  return !!getInstalledFabricId(gamePath, mcVersion);
}

async function installFabric(gamePath, mcVersion, onProgress) {
  onProgress('Fabric 최신 버전 확인 중...');

  const loaders = await fetchJson(
    `https://meta.fabricmc.net/v2/versions/loader/${mcVersion}?limit=1`
  );
  if (!loaders || loaders.length === 0)
    throw new Error(`Fabric이 Minecraft ${mcVersion}을 지원하지 않습니다.`);

  const loaderVer = loaders[0].loader.version;
  const versionId = `fabric-loader-${loaderVer}-${mcVersion}`;

  onProgress(`Fabric ${loaderVer} 프로파일 다운로드 중...`);

  const profile = await fetchJson(
    `https://meta.fabricmc.net/v2/versions/loader/${mcVersion}/${loaderVer}/profile/json`
  );

  const versionDir = path.join(gamePath, 'versions', versionId);
  fs.mkdirSync(versionDir, { recursive: true });
  fs.writeFileSync(
    path.join(versionDir, `${versionId}.json`),
    JSON.stringify(profile, null, 2)
  );

  onProgress(`Fabric ${loaderVer} 설치 완료`);
  return versionId;
}

// ─── Forge ───────────────────────────────────────────────────

function getInstalledForgeId(gamePath, mcVersion) {
  const versions = path.join(gamePath, 'versions');
  if (!fs.existsSync(versions)) return null;
  const found = fs.readdirSync(versions)
    .find(v => v.toLowerCase().includes('forge') && v.includes(mcVersion));
  return found || null;
}

function isForgeInstalled(gamePath, mcVersion) {
  return !!getInstalledForgeId(gamePath, mcVersion);
}

async function installForge(gamePath, mcVersion, javaPath, tmpDir, onProgress) {
  onProgress('Forge 최신 버전 확인 중...');

  const promotions = await fetchJson(
    'https://files.minecraftforge.net/net/minecraftforge/forge/promotions_slim.json'
  );

  const forgeVer =
    promotions.promos[`${mcVersion}-recommended`] ||
    promotions.promos[`${mcVersion}-latest`];

  if (!forgeVer)
    throw new Error(`Forge가 Minecraft ${mcVersion}을 지원하지 않습니다.`);

  const fullVer = `${mcVersion}-${forgeVer}`;
  const installerUrl =
    `https://maven.minecraftforge.net/net/minecraftforge/forge/${fullVer}/forge-${fullVer}-installer.jar`;

  onProgress(`Forge ${forgeVer} 다운로드 중...`);

  fs.mkdirSync(tmpDir, { recursive: true });
  const installerJar = path.join(tmpDir, `forge-installer.jar`);
  await downloadFile(installerUrl, installerJar, (curr, total) => {
    onProgress(`Forge 다운로드 중... ${Math.round(curr / total * 100)}%`);
  });

  onProgress('Forge 설치 중... (1~3분 소요)');

  await new Promise((resolve, reject) => {
    const proc = spawn(javaPath, ['-jar', installerJar, '--installClient', gamePath], {
      stdio: ['ignore', 'pipe', 'pipe']
    });
    const timeout = setTimeout(() => { proc.kill(); reject(new Error('Forge 설치 타임아웃 (5분).')); }, 300000);
    proc.on('close', code => {
      clearTimeout(timeout);
      if (code === 0) resolve();
      else reject(new Error(`Forge 설치 실패 (종료코드: ${code})`));
    });
    proc.on('error', e => { clearTimeout(timeout); reject(e); });
  });

  try { fs.unlinkSync(installerJar); } catch {}

  const versionId = getInstalledForgeId(gamePath, mcVersion);
  onProgress('Forge 설치 완료');
  return versionId;
}

// ─── exports ─────────────────────────────────────────────────

module.exports = {
  isFabricInstalled, getInstalledFabricId, installFabric,
  isForgeInstalled,  getInstalledForgeId,  installForge
};
