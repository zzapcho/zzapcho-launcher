/**
 * servers.dat 파일 생성 (GZip 압축 NBT 형식)
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
  // ── 각 서버를 TAG_Compound payload로 인코딩 ──────────────────
  const entries = servers.map(s => {
    // port가 25565이거나 없으면 ip만, 아니면 ip:port
    const ip = (s.port && s.port !== 25565) ? `${s.ip}:${s.port}` : (s.ip || '');
    return Buffer.concat([
      tagString('name', s.name  || 'Server'),
      tagString('ip',   ip),
      tagByte('acceptTextures', 1),
      TAG_END   // TAG_End closes each compound entry
    ]);
  });

  // ── TAG_List "servers" 헤더 ──────────────────────────────────
  // [09][name_len:u16][name][elem_type:u8][count:i32]
  const listName  = Buffer.from('servers', 'utf8');
  const listHead  = Buffer.allocUnsafe(1 + 2 + listName.length + 1 + 4);
  let o = 0;
  listHead[o++] = 9;                            // TAG_List id
  listHead.writeUInt16BE(listName.length, o); o += 2;
  listName.copy(listHead, o); o += listName.length;
  listHead[o++] = 10;                           // element type = TAG_Compound
  listHead.writeInt32BE(servers.length, o);

  // ── Root TAG_Compound (name_len=0) ───────────────────────────
  // [0A][00 00][...content...][00]
  return Buffer.concat([
    Buffer.from([0x0A, 0x00, 0x00]),  // TAG_Compound, empty name
    listHead,
    ...entries,
    TAG_END                           // closes root compound
  ]);
}

// ─── Public API ──────────────────────────────────────────────

/**
 * gamePath 디렉토리에 servers.dat 작성
 * @param {string} gamePath  - Minecraft 게임 디렉토리 (GAME_PATH)
 * @param {Array}  servers   - [{ name, ip, port }]
 */
function writeServersDat(gamePath, servers) {
  if (!Array.isArray(servers) || servers.length === 0) return;

  fs.mkdirSync(gamePath, { recursive: true });

  const nbt        = encodeServersDat(servers);
  const compressed = zlib.gzipSync(nbt);
  const dest       = path.join(gamePath, 'servers.dat');

  fs.writeFileSync(dest, compressed);
  console.log('[servers.dat] written to:', dest, `(${servers.length} server(s))`);
}

module.exports = { writeServersDat };
