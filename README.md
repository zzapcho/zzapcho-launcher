# zzapcho Launcher 관리자 가이드

> 이 런처를 처음 보는 분도 이해할 수 있도록 작성했습니다.

---

## 📦 전체 구조 이해하기

이 런처는 **GitHub 저장소 2개**로 운영됩니다.

| 저장소 | 하는 일 |
|--------|---------|
| `zzapcho/zzapcho-launcher` | 런처 프로그램 자체 (지금 이 곳) |
| `zzapcho/mcserver1` | 모드, 리소스팩, 셰이더 파일 관리 |

- 플레이어가 런처를 켜면 → `mcserver1`에서 최신 파일 자동 다운로드
- 런처 코드를 수정하면 → 플레이어 런처가 자동으로 업데이트

---

## 🧩 모드 / 리소스팩 / 셰이더 추가하는 법

### 1단계 — `mcserver1` 저장소 열기

👉 https://github.com/zzapcho/mcserver1 접속

---

### 2단계 — 파일을 올바른 폴더에 업로드

아래 폴더에 맞는 파일을 넣으면 됩니다.

```
mcserver1/
├── mods/           ← 모드 파일 (.jar)
├── resourcepacks/  ← 리소스팩 파일 (.zip)
└── shaderpacks/    ← 셰이더 파일 (.zip)
```

**방법:**
1. 위 폴더 중 하나 클릭 (예: `mods`)
2. 오른쪽 위 **Add file** → **Upload files** 클릭
3. 파일 끌어다 놓기
4. 아래 **Commit changes** 클릭

---

### 3단계 — 기다리기 (약 1~2분)

파일을 올리면 GitHub가 자동으로 `manifest.json`을 업데이트합니다.
→ 플레이어가 **플레이 버튼**을 누를 때 자동으로 새 파일이 받아집니다. ✅

---

### ❌ 모드 삭제하는 법

1. `mods/` 폴더에서 삭제할 파일 클릭
2. 오른쪽 위 🗑️ **Delete file** 클릭
3. **Commit changes** 클릭

→ 플레이어 런처가 다음 실행 시 자동으로 해당 파일 삭제 ✅

---

### 📝 서버 주소 변경하는 법

1. `mcserver1` 저장소에서 `manifest.json` 파일 클릭
2. 오른쪽 위 ✏️ (연필 아이콘) 클릭
3. 아래 부분 수정:

```json
"servers": [
  {
    "name": "서버 이름",
    "ip": "서버.주소.com",
    "port": 25565
  }
]
```

4. **Commit changes** 클릭

---

### 🎮 마인크래프트 버전 변경하는 법

1. `manifest.json`에서 아래 부분 수정:

```json
"gameVersion": "1.21.1",
"modLoader": {
  "type": "fabric",
  "version": "latest"
}
```

- `type`은 `"fabric"`, `"forge"`, `"vanilla"` 중 하나
- `version`은 `"latest"` 또는 `"0.16.9"` 처럼 직접 지정

2. **Commit changes** 클릭

---

## 🚀 런처 자체를 업데이트하는 법

런처 디자인이나 기능을 바꾸고 싶을 때 사용합니다.

---

### 1단계 — 코드 수정

`C:\Users\kdy20\Desktop\Claude\mrs-launcher` 폴더에서 원하는 파일을 수정합니다.

---

### 2단계 — 버전 숫자 올리기

`package.json` 파일을 메모장이나 VSCode로 열어서 맨 위쪽에 있는 버전 숫자를 올립니다.

```json
"version": "1.0.1"   ← 이 숫자를 1.0.2, 1.0.3 이런식으로 올리면 됨
```

> ⚠️ 버전 숫자를 안 올리면 플레이어 런처가 업데이트가 생긴 줄 모릅니다!

---

### 3단계 — 터미널 열기

`mrs-launcher` 폴더 안에서 터미널을 엽니다.

> **방법:** `mrs-launcher` 폴더를 탐색기에서 열고 → 주소창 클릭 → `cmd` 입력 → 엔터

---

### 4단계 — 명령어 4줄 입력

아래 명령어를 **위에서부터 순서대로** 하나씩 입력하고 엔터를 누릅니다.

버전이 `1.0.2`라면:

```
git add .
git commit -m "v1.0.2"
git tag v1.0.2
git push && git push --tags
```

> `v1.0.2` 부분은 package.json에 적은 버전 숫자와 똑같이 써야 합니다!

각 명령어가 하는 일:
- `git add .` → 수정한 파일 전부 선택
- `git commit -m "..."` → 변경사항 저장
- `git tag v1.0.2` → "이게 버전 1.0.2야" 라고 표시
- `git push && git push --tags` → GitHub에 올리기

---

### 5단계 — GitHub가 알아서 빌드 (5~10분 기다리기)

명령어를 입력하면 GitHub가 자동으로 exe 파일을 만들어줍니다.
진행 상황 보려면 👉 https://github.com/zzapcho/zzapcho-launcher/actions

- 🟡 노란 원 = 지금 빌드 중
- ✅ 초록 체크 = 완료! exe 파일이 Releases에 올라갔음
- ❌ 빨간 X = 실패 (클릭 → **Re-run all jobs** 눌러서 다시 시도)

---

### 6단계 — 설치 파일 받기 (첫 배포 때만)

> 처음 한 번만 플레이어가 직접 설치해야 합니다.
> 그 다음부터는 런처가 켜질 때 혼자 알아서 업데이트됩니다.

👉 https://github.com/zzapcho/zzapcho-launcher/releases

여기서 `zzapcho Launcher Setup X.X.X.exe` 파일을 받아서 플레이어에게 공유하면 됩니다.

---

### 7단계 — 플레이어 자동 업데이트

플레이어가 런처를 켜면:

1. 새 버전이 있으면 자동으로 다운로드 시작
2. 화면에 다운로드 진행률 표시
3. 완료되면 자동으로 재시작 → 새 버전으로 업데이트 완료 ✅

---

## 🔄 업데이트 타이밍 정리

| 무엇 | 언제 체크 | 어디서 가져옴 |
|------|----------|--------------|
| 모드 / 리소스팩 / 셰이더 | 플레이 버튼 누를 때마다 | `mcserver1` GitHub |
| 런처 프로그램 자체 | 런처 켤 때 (2초 후 자동) | `zzapcho-launcher` GitHub Releases |

---

## 📋 자주 쓰는 명령어 모음

```bash
# 런처 개발 중 실행 (테스트)
npm start

# 최종 배포용 .exe 빌드 (로컬, 개발자 모드 필요)
npm run build
```

---

## ❓ 문제가 생겼을 때

| 증상 | 해결 방법 |
|------|----------|
| GitHub Actions 빌드 실패 | Actions 탭에서 빨간 X 클릭 → 오류 확인 → **Re-run all jobs** |
| Actions에서 "Permission denied" | Settings → Actions → Workflow permissions → Read and write permissions |
| 플레이어가 업데이트 안 됨 | package.json version이 올라갔는지 확인 |
| 모드가 적용 안 됨 | mcserver1의 manifest.json이 업데이트됐는지 확인 |
| 서버가 멀티플레이에 없음 | manifest.json의 servers 항목 IP 확인 |
| 로컬 빌드 실패 (심볼릭 링크 오류) | Windows 설정 → 개발자용 → 개발자 모드 켜기 |
