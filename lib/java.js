/**
 * Java 감지 및 자동 설치
 * Adoptium (Eclipse Temurin) JRE 21 사용
 */

const { execSync } = require('child_process');
const { net } = require('electron');
const path = require('path');
const fs = require('fs');

function fetchJson(url) {
  return new Promise((resolve, reject) => {
    const req = net.request({ url, method: 'GET' });
    let chunks = [];
    req.on('response', res => {
      if (res.statusCode !== 200) return reject(new Error(`HTTP ${res.statusCode}`));
      res.on('data', c => chunks.push(c));
      res.on('end', () => {
        try { resolve(JSON.parse(Buffer.concat(chunks).toString('utf-8'))); }
        catch (e) { reject(e); }
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

function isJavaValid(javaPath) {
  try {
    execSync(`"${javaPath}" -version`, { stdio: 'pipe', timeout: 5000 });
    return true;
  } catch { return false; }
}

function findBundledJava(dataPath) {
  const jreDir = path.join(dataPath, 'jre');
  if (!fs.existsSync(jreDir)) return null;
  for (const entry of fs.readdirSync(jreDir)) {
    const javaw = path.join(jreDir, entry, 'bin', 'javaw.exe');
    const java  = path.join(jreDir, entry, 'bin', 'java');
    if (fs.existsSync(javaw) && isJavaValid(javaw)) return javaw;
    if (fs.existsSync(java)  && isJavaValid(java))  return java;
  }
  return null;
}

/** 사용 가능한 Java 경로 반환. 없으면 null */
function detectJava(dataPath, customPath = '') {
  if (customPath && isJavaValid(customPath)) return customPath;
  const bundled = findBundledJava(dataPath);
  if (bundled) return bundled;
  if (isJavaValid('javaw')) return 'javaw';
  if (isJavaValid('java'))  return 'java';
  return null;
}

/**
 * Adoptium JRE 21 다운로드 및 설치
 * @param {string} dataPath  - 런처 데이터 폴더 (ex: %AppData%/MRSLauncher)
 * @param {function} onProgress - (message, current, total) 콜백
 * @returns {string} 설치된 java 실행 파일 경로
 */
async function downloadJava(dataPath, onProgress) {
  onProgress('Java 다운로드 정보 가져오는 중...', 0, 100);

  const apiUrl = 'https://api.adoptium.net/v3/assets/latest/21/hotspot?os=windows&arch=x64&image_type=jre&vendor=eclipse';
  const info = await fetchJson(apiUrl);
  if (!info || !info[0]) throw new Error('Adoptium API에서 Java 정보를 가져오지 못했습니다.');

  const pkg = info[0].binary.package;
  const jreDir = path.join(dataPath, 'jre');
  const zipPath = path.join(jreDir, 'jre.zip');

  fs.mkdirSync(jreDir, { recursive: true });

  onProgress('Java 다운로드 중...', 0, pkg.size || 100);
  await downloadFile(pkg.link, zipPath, (curr, total) => {
    onProgress('Java 다운로드 중...', curr, total);
  });

  onProgress('Java 압축 해제 중...', 0, 100);
  // Windows 내장 PowerShell로 압축 해제
  execSync(
    `powershell -NoProfile -Command "Expand-Archive -LiteralPath '${zipPath}' -DestinationPath '${jreDir}' -Force"`,
    { stdio: 'pipe', timeout: 180000 }
  );
  fs.unlinkSync(zipPath);

  const javaPath = findBundledJava(dataPath);
  if (!javaPath) throw new Error('Java 설치 후 실행 파일을 찾을 수 없습니다.');
  return javaPath;
}

module.exports = { detectJava, downloadJava };
