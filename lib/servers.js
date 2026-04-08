/**
 * servers.dat 파일 읽기/쓰기 (GZip 압축 NBT 형식)
 * Minecraft 1.7+ 표준 서버 목록 형식
 */

const fs   = require('fs');
const path = require('path');
const zlib = require('zlib');

// ─── NBT primitive helpers ───────────────────────────────────

function tagString(name, value) {
  const n = Buffer.from(name,  'utf8');
  const v = Buffer.from(value, 'utf8');
  const buf = Buffer.allocUnsafe(1 + 2 + n.length + 2 + v.length);
  let o = 0;
  buf[o++] = 8;
  buf.writeUInt16BE(n.length, o); o += 2;
  n.copy(buf, o); o += n.length;
  buf.writeUInt16BE(v.length, o); o += 2;
  v.copy(buf, o);
  return buf;
}

function tagByte(name, value) {
  const n = Buffer.from(name, 'utf8');
  const buf = Buffer.allocUnsafe(1 + 2 + n.length + 1);
  let o = 0;
  buf[o++] = 1;
  buf.writeUInt16BE(n.length, o); o += 2;
  n.copy(buf, o); o += n.length;
  buf.writeInt8(value, o);
  return buf;
}

const TAG_END = Buffer.from([0x00]);

// ─── Encode ──────────────────────────────────────────────────

function encodeServersDat(servers) {
  const entries = servers.map(s => {
    const ip = (s.port && s.port !== 25565) ? `${s.ip}:${s.port}` : (s.ip || '');
    return Buffer.concat([
      tagString('name', s.name || 'Server'),
      tagString('ip',   ip),
      tagByte('acceptTextures', 1),
      TAG_END
    ]);
  });

  const listName = Buffer.from('servers', 'utf8');
  const listHead = Buffer.allocUnsafe(1 + 2 + listName.length + 1 + 4);
  let o = 0;
  listHead[o++] = 9;
  listHead.writeUInt16BE(listName.length, o); o += 2;
  listName.copy(listHead, o); o += listName.length;
  listHead[o++] = 10;
  listHead.writeInt32BE(servers.length, o);

  return Buffer.concat([
    Buffer.from([0x0A, 0x00, 0x00]),
    listHead,
    ...entries,
    TAG_END
  ]);
}

// ─── Decode ──────────────────────────────────────────────────

function parseServersDat(buf) {
  const servers = [];
  let o = 0;

  function readString() {
    const len = buf.readUInt16BE(o); o += 2;
    const s = buf.slice(o, o + len).toString('utf8'); o += len;
    return s;
  }

  try {
    if (buf[o++] !== 10) return [];
    readString(); // root name

    while (o < buf.length) {
      const type = buf[o++];
      if (type === 0) break;
      const tagName = readString();

      if (type === 9 && tagName === 'servers') {
        o++; // element type
        const count = buf.readInt32BE(o); o += 4;

        for (let i = 0; i < count; i++) {
          const sv = {};
          while (o < buf.length) {
            const ft = buf[o++];
            if (ft === 0) break;
            const fn = readString();
            if      (ft === 8) { sv[fn] = readString(); }
            else if (ft === 1) { o++; }
            else if (ft === 2) { o += 2; }
            else if (ft === 3) { o += 4; }
            else if (ft === 4) { o += 8; }
            else if (ft === 5) { o += 4; }
            else if (ft === 6) { o += 8; }
          }
          if (sv.ip !== undefined) {
            const lastColon = sv.ip.lastIndexOf(':');
            let host = sv.ip, port = 25565;
            if (lastColon > 0) {
              const p = parseInt(sv.ip.slice(lastColon + 1));
              if (!isNaN(p)) { host = sv.ip.slice(0, lastColon); port = p; }
            }
            servers.push({ name: sv.name || 'Server', ip: host, port });
          }
        }
      }
    }
  } catch {}

  return servers;
}

function normalizeIp(ip) {
  if (!ip) return '';
  const s = String(ip).toLowerCase();
  const lastColon = s.lastIndexOf(':');
  if (lastColon > 0 && !isNaN(parseInt(s.slice(lastColon + 1)))) {
    return s.slice(0, lastColon);
  }
  return s;
}

// ─── Public API ──────────────────────────────────────────────

function readServersDat(gamePath) {
  const dest = path.join(gamePath, 'servers.dat');
  if (!fs.existsSync(dest)) return [];
  try {
    const buf = zlib.gunzipSync(fs.readFileSync(dest));
    return parseServersDat(buf);
  } catch { return []; }
}

/**
 * servers.dat 쓰기
 * - manifest 서버는 항상 맨 위
 * - 사용자 추가 서버(manifest/이전manifest 제외)는 보존
 * - 실패해도 setup:run을 중단시키지 않음 (try/catch 포함)
 */
function writeServersDat(gamePath, manifestServers, prevManifestServers = []) {
  try {
    if (!Array.isArray(manifestServers)) manifestServers = [];

    fs.mkdirSync(gamePath, { recursive: true });

    // 사용자 추가 서버 추출 (manifest & 이전 preset 서버 제외)
    let userServers = [];
    try {
      const existing = readServersDat(gamePath);
      const toExclude = new Set(
        [...(manifestServers), ...(prevManifestServers || [])].map(s => normalizeIp(s.ip))
      );
      userServers = existing.filter(s => !toExclude.has(normalizeIp(s.ip)));
    } catch {}

    // manifest 서버 상단 고정 + 사용자 서버
    const merged = [...manifestServers, ...userServers];
    if (merged.length === 0) return;

    const nbt        = encodeServersDat(merged);
    const compressed = zlib.gzipSync(nbt);
    fs.writeFileSync(path.join(gamePath, 'servers.dat'), compressed);
    console.log('[servers.dat] written:', merged.map(s => s.ip).join(', '));
  } catch (e) {
    console.error('[servers.dat] write failed:', e.message);
  }
}

module.exports = { writeServersDat, readServersDat };
