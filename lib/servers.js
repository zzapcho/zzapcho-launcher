/**
 * servers.dat 파일 읽기/쓰기 (GZip 압축 NBT 형식)
 * Minecraft 1.7+ 표준 서버 목록 형식
 */

const fs   = require('fs');
const path = require('path');
const zlib = require('zlib');

// ─── NBT primitive helpers ───────────────────────────────────

/** TAG_String (id=8): [08][name_len:u16][name][value_len:u16][value] */
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

/** TAG_Byte (id=1): [01][name_len:u16][name][value:i8] */
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

/** TAG_End: [00] */
const TAG_END = Buffer.from([0x00]);

// ─── Encode servers list to NBT ──────────────────────────────

function encodeServersDat(servers) {
  const entries = servers.map(s => {
    const ip = (s.port && s.port !== 25565) ? `${s.ip}:${s.port}` : (s.ip || '');
    return Buffer.concat([
      tagString('name', s.name  || 'Server'),
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

// ─── Decode servers.dat NBT ──────────────────────────────────

function parseServersDat(buf) {
  const servers = [];
  let o = 0;

  function readString() {
    const len = buf.readUInt16BE(o); o += 2;
    const s = buf.slice(o, o + len).toString('utf8'); o += len;
    return s;
  }

  try {
    // Root TAG_Compound
    if (buf[o++] !== 10) return [];
    readString(); // root name

    while (o < buf.length) {
      const type = buf[o++];
      if (type === 0) break; // TAG_End
      const tagName = readString();

      if (type === 9 && tagName === 'servers') { // TAG_List of servers
        const elemType = buf[o++]; // should be 10 (TAG_Compound)
        const count = buf.readInt32BE(o); o += 4;

        for (let i = 0; i < count; i++) {
          const sv = {};
          while (o < buf.length) {
            const ft = buf[o++];
            if (ft === 0) break; // TAG_End closes compound
            const fn = readString();
            if (ft === 8) { sv[fn] = readString(); }      // TAG_String
            else if (ft === 1) { o++; }                    // TAG_Byte
            else if (ft === 2) { o += 2; }                 // TAG_Short
            else if (ft === 3) { o += 4; }                 // TAG_Int
            else if (ft === 4) { o += 8; }                 // TAG_Long
            else if (ft === 5) { o += 4; }                 // TAG_Float
            else if (ft === 6) { o += 8; }                 // TAG_Double
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
      // 필요없는 다른 태그는 skip (간단히 무시 — servers 태그만 필요)
    }
  } catch { /* 파싱 실패 시 빈 배열 반환 */ }

  return servers;
}

// ─── Public API ──────────────────────────────────────────────

/**
 * servers.dat 읽기 → [{name, ip, port}] 반환
 */
function readServersDat(gamePath) {
  const dest = path.join(gamePath, 'servers.dat');
  if (!fs.existsSync(dest)) return [];
  try {
    const buf = zlib.gunzipSync(fs.readFileSync(dest));
    return parseServersDat(buf);
  } catch { return []; }
}

/**
 * servers.dat 쓰기 (manifest 서버 + 사용자 추가 서버 병합)
 * @param {string} gamePath       - Minecraft 게임 디렉토리
 * @param {Array}  manifestServers - manifest에 정의된 서버 목록
 * @param {Array}  prevManifestServers - 이전 manifest 서버 목록 (교체 시 제거용)
 */
function writeServersDat(gamePath, manifestServers, prevManifestServers = []) {
  if (!Array.isArray(manifestServers)) manifestServers = [];

  fs.mkdirSync(gamePath, { recursive: true });

  // 현재 servers.dat에서 사용자 추가 서버 추출 (manifest/이전manifest 서버 제외)
  const existing = readServersDat(gamePath);
  const toExclude = new Set([
    ...(manifestServers || []),
    ...(prevManifestServers || [])
  ].map(s => normalizeIp(s.ip)));

  const userServers = existing.filter(s => !toExclude.has(normalizeIp(s.ip)));

  // manifest 서버 먼저, 그 다음 사용자 서버
  const merged = [...manifestServers, ...userServers];

  if (merged.length === 0) return;

  const nbt        = encodeServersDat(merged);
  const compressed = zlib.gzipSync(nbt);
  fs.writeFileSync(path.join(gamePath, 'servers.dat'), compressed);
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

module.exports = { writeServersDat, readServersDat };
