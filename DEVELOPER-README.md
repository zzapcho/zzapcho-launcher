# 개발자 README

이 문서는 `잡초 약탈서버 런처`를 개발하거나 운영할 때 알아야 하는 실행 방법, 폴더 구조, 기능 구조를 정리한 문서입니다.

## 현재 구현 단계

현재는 1-7번 단계의 베타 골격까지 진행 중입니다.

- 1번: WPF 앱 뼈대, 설정, 로그, 서버 상태
- 2번: manifest 로드, manifest 검증, SHA-256 검사, 격리 구조, 업데이트 화면
- 3번: 로그인/Java/Minecraft 실행 서비스 인터페이스와 개발용 placeholder
- 4번: permit/Paper 게이트 문서와 스켈레톤
- 5번: 업데이트 서비스 인터페이스와 릴리즈 문서
- 6번: 지원 ZIP 생성
- 7번: QA/보안 리뷰 문서와 참조 런처 스타일 UI 개편

아직 구현 전:

- 실제 Microsoft OAuth 로그인
- 실제 CMLLib Minecraft 실행
- 실제 Paper 플러그인 Java 구현
- 실제 업데이트 다운로드/재시작 UI
- 실제 크래시 리포트 업로드

## 제일 쉬운 실행 방법

처음 개발하는 PC에서는 아래 명령 하나만 실행하면 됩니다.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\dev.ps1
```

이 명령이 하는 일:

1. 저장소 안의 `.dotnet-sdk` 폴더에 .NET 8 SDK가 있는지 확인합니다.
2. 없으면 Microsoft 공식 설치 스크립트로 .NET 8 SDK를 자동 설치합니다.
3. 프로젝트를 restore 합니다.
4. 프로젝트를 build 합니다.
5. 테스트를 실행합니다.
6. 테스트가 끝나면 런처 앱을 실행합니다.

테스트는 건너뛰고 앱만 빨리 켜고 싶으면:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\dev.ps1 -SkipTests
```

## 명령별 사용법

### 1. SDK 준비만 하기

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\setup.ps1
```

사용하는 경우:

- 이 PC에 .NET SDK가 있는지 모르겠을 때
- 처음 개발 환경을 준비할 때
- restore만 먼저 확인하고 싶을 때

### 2. 빌드만 하기

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

사용하는 경우:

- 코드가 컴파일되는지만 확인하고 싶을 때
- PR 올리기 전에 빠르게 확인할 때

### 3. 테스트까지 하기

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\test.ps1
```

사용하는 경우:

- 기능을 고친 뒤 깨진 부분이 없는지 확인할 때
- manifest, 파일 검증, 설정 저장 같은 로직을 검증할 때

### 4. 앱 실행하기

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run.ps1
```

사용하는 경우:

- WPF 런처 화면을 직접 확인하고 싶을 때
- 서버 상태 카드, 설정 화면, 업데이트 화면을 눈으로 보고 싶을 때

### 5. 배포용 publish 만들기

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish.ps1
```

이 명령은 Release 빌드, 테스트, self-contained publish를 실행합니다. 일반 유저에게 SDK를 설치시키지 않는 배포 폴더가 만들어집니다.

결과:

```text
artifacts/publish/win-x64
```

테스트를 이미 끝낸 상태에서 빠르게 publish만 만들고 싶으면:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish.ps1 -SkipTests
```

### 6. 설치 파일까지 만들기

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package.ps1
```

이 명령은 `publish.ps1` 흐름 뒤에 Velopack CLI를 저장소 안의 `.tools/vpk`에 설치하고 설치/업데이트 패키지를 만듭니다.

결과:

```text
artifacts/releases/win-x64
```

버전을 직접 지정하려면:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package.ps1 -Version 1.0.0-beta.2
```

## 자동 설치되는 것

개발 스크립트는 .NET 8 SDK가 없으면 저장소 내부에 자동 설치합니다.

```text
.dotnet-sdk/
```

이 폴더는 개발용 도구 폴더라 Git에 올리지 않습니다.

dotnet 실행용 홈 폴더도 저장소 내부에 만듭니다.

```text
.dotnet-home/
```

이렇게 하는 이유:

- 사용자 PC 전체 설정을 최대한 건드리지 않기 위해서
- SDK가 없는 PC에서도 개발 명령이 바로 동작하게 하기 위해서
- 권한 문제를 줄이기 위해서

## 유저 배포 때 자동 설치는 어떻게 할 것인가

개발 중에는 위 스크립트가 SDK를 자동 설치합니다.

실제 유저 배포에서는 유저가 SDK를 설치하면 안 됩니다.
유저는 개발자가 아니라 런처만 실행하면 되는 사람이기 때문입니다.

정식 배포는 `publish.ps1`와 `package.ps1`에서 처리합니다.

- Velopack 설치파일 생성
- self-contained 배포 폴더 생성
- 런처 설치와 업데이트 자동화
- Windows SmartScreen 경고를 줄이기 위한 코드서명 준비

즉:

- 개발자 PC: `scripts/*.ps1`이 SDK 자동 설치
- 일반 유저 PC: Velopack 설치 파일이 런처 설치와 업데이트 담당

## 배포 버전 관리

런처 자체 버전은 `Directory.Build.props`에서 수정합니다.

```xml
<Version>1.0.0-beta.1</Version>
<AssemblyVersion>1.0.0.0</AssemblyVersion>
<FileVersion>1.0.0.0</FileVersion>
```

GitHub Actions 릴리즈에서 다른 버전을 지정하면 그 값이 패키지 버전으로 사용됩니다.

클라이언트 파일/Minecraft 버전은 `manifest/launcher-manifest.json`에서 수정합니다.

배포 절차와 체크리스트는 [docs/distribution-readiness.md](docs/distribution-readiness.md)에 따로 정리했습니다.

## GitHub 연결

현재 저장소 원격 주소:

```text
https://github.com/zzapcho/zzapcho-launcher.git
```

처음 올릴 때는 기존 GitHub `main`을 바로 덮지 말고 새 브랜치로 올리는 편이 안전합니다.

```powershell
git status
git add .
git commit -m "Prepare WPF launcher beta"
git branch -M launcher-wpf-beta
git push -u origin launcher-wpf-beta
```

그 다음 GitHub에서 `launcher-wpf-beta` 브랜치로 Pull Request를 만들고 확인한 뒤 `main`에 합치면 됩니다.

GitHub 로그인이 안 되어 있으면 push 중 브라우저 로그인이 뜹니다. GitHub CLI를 쓰는 경우에는 아래 명령으로 먼저 로그인해도 됩니다.

```powershell
gh auth login
```

원격 주소를 다시 확인하려면:

```powershell
git remote -v
```

## 수동 실행 방법

.NET 8 SDK가 필요합니다.

```powershell
dotnet restore ZzapchoRaidLauncher.sln
dotnet build ZzapchoRaidLauncher.sln
dotnet run --project src/Zzapcho.Launcher.App
```

일반적으로는 위 자동화 스크립트를 쓰면 됩니다.
아래는 이 작업 환경처럼 SDK가 기본 설치되어 있지 않거나 MSBuild 병렬 평가 문제가 있는 경우의 참고 명령입니다.

```powershell
New-Item -ItemType Directory -Force -Path '.dotnet-home\.dotnet\tools'
$env:DOTNET_CLI_HOME=(Resolve-Path '.dotnet-home').Path
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:MSBuildEnableWorkloadResolver='false'
$env:MSBUILDDISABLENODEREUSE='1'
$env:MSBUILDUSESERVER='0'
C:\tmp\dotnet-sdk-8\dotnet.exe build ZzapchoRaidLauncher.sln --no-restore -m:1
C:\tmp\dotnet-sdk-8\dotnet.exe run --project src\Zzapcho.Launcher.Tests --no-build
```

`-m:1`은 이 환경에서 MSBuild 병렬 참조 평가가 오류 메시지 없이 실패하는 문제를 피하기 위한 옵션입니다.

## 앱 실행 후 확인할 것

앱을 켜면 먼저 아래 화면을 확인합니다.

1. `입장` 화면
   - `online.zzapcho.kr` 서버 상태가 보이는지 확인합니다.
   - 서버가 꺼져 있으면 오프라인으로 보여야 합니다.

2. `업데이트` 화면
   - manifest 버전이 보이는지 확인합니다.
   - Minecraft 버전과 로더 버전이 보이는지 확인합니다.
   - `다시 검사` 버튼이 동작하는지 확인합니다.

3. `설정` 화면
   - RAM 값을 바꾸고 저장합니다.
   - `manifestUrl` 값을 바꿀 수 있는지 확인합니다.

4. `로그` 화면
   - launcher 로그가 보이는지 확인합니다.
   - 로그 폴더 열기 버튼이 동작하는지 확인합니다.

## 자주 생기는 문제

### PowerShell에서 스크립트 실행이 막힘

아래처럼 `-ExecutionPolicy Bypass`를 붙여 실행합니다.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\dev.ps1
```

### SDK 다운로드가 실패함

인터넷 연결을 확인합니다.
회사/학교/방화벽 환경에서는 `https://dot.net/v1/dotnet-install.ps1` 접근이 막힐 수 있습니다.

그 경우 .NET 8 SDK를 직접 설치한 뒤 다시 실행합니다.

### 빌드는 되는데 앱 실행이 안 됨

Windows가 아닌 환경에서는 WPF 앱이 실행되지 않습니다.
이 런처는 Windows 전용입니다.

## 프로젝트 구조

```text
src/
  Zzapcho.Launcher.App/             WPF UI와 ViewModel
  Zzapcho.Launcher.Core/            모델, 상수, 서비스 인터페이스
  Zzapcho.Launcher.Infrastructure/  파일, 설정, 로그, 서버 핑, manifest 구현
  Zzapcho.Launcher.Tests/           외부 테스트 패키지 없는 간단 테스트 러너
docs/                               제품/보안/로드맵 문서
manifest/                           GitHub에서 운영할 manifest 파일
```

## 주요 기능 구조

### 고정 서버

서버는 코드 상수로 고정되어 있습니다.

```text
online.zzapcho.kr:25565
```

사용자가 서버 주소를 바꾸는 UI는 만들지 않습니다.

### 서버 상태

`MinecraftPingStatusProvider`가 Minecraft Server List Ping으로 서버 상태를 확인합니다.

표시 항목:

- 온라인/오프라인
- 현재 인원
- 최대 인원
- 서버 버전
- MOTD
- 핑
- 서버가 제공하는 sample player

전체 접속자 목록은 기본 ping만으로 항상 나오지 않습니다.
정확한 목록은 이후 `ZzapchoGate` Paper 플러그인 API로 연결합니다.

### 설정

설정 파일:

```text
%LocalAppData%\ZzapchoRaidLauncher\settings.json
```

중요 설정:

- `ramMinMb`
- `ramMaxMb`
- `autoUpdate`
- `crashReportConsent`
- `manifestUrl`

### 로그

로그 폴더:

```text
%LocalAppData%\ZzapchoRaidLauncher\logs
```

로그 파일:

- `launcher.log`
- `game.log`
- `update.log`
- `crash.log`

### manifest

런처가 믿는 공식 파일 목록입니다.

개발용 파일:

```text
manifest/launcher-manifest.sample.json
```

GitHub 운영용 파일:

```text
manifest/launcher-manifest.json
```

GitHub에 올린 뒤 raw URL을 설정의 `manifestUrl`에 넣으면 런처가 그 파일을 읽습니다.

예시:

```text
https://raw.githubusercontent.com/OWNER/REPO/main/manifest/launcher-manifest.json
```

운영자가 GitHub에서 바꿀 수 있는 값:

- `manifestVersion`
- `minecraft.version`
- `minecraft.loader`
- `minecraft.loaderVersion`
- `launcher.minimumVersion`
- `launcher.latestVersion`
- `files`
- 각 파일의 `url`
- 각 파일의 `sha256`
- 각 파일의 `size`

즉 Minecraft 버전이나 런처 최소 버전은 앱을 다시 빌드하지 않고 GitHub manifest 수정으로 바꿀 수 있습니다.

## manifest 파일 추가 방법

1. 공식 모드/리소스팩 파일을 GitHub Releases에 업로드합니다.
2. 파일 SHA-256을 계산합니다.
3. `manifest/launcher-manifest.json`의 `files`에 추가합니다.
4. `manifestVersion`을 올립니다.
5. GitHub에 push합니다.
6. 런처에서 업데이트 화면의 `다시 검사`를 누릅니다.

파일 항목 예시:

```json
{
  "path": "mods/example.jar",
  "url": "https://github.com/OWNER/REPO/releases/download/client-2026.05.09/example.jar",
  "sha256": "64자리_SHA256",
  "size": 123456,
  "required": true
}
```

## 파일 검증 정책

보호 폴더:

- `mods`
- `resourcepacks`
- `shaderpacks`
- `config`

규칙:

- manifest에 있는 파일만 공식 파일입니다.
- manifest에 없는 파일은 quarantine 폴더로 이동합니다.
- SHA-256이 다른 파일은 복구 대상입니다.
- 필수 파일이 없으면 입장 버튼은 막힙니다.

격리 폴더:

```text
%LocalAppData%\ZzapchoRaidLauncher\quarantine
```

## 테스트

```powershell
dotnet run --project src/Zzapcho.Launcher.Tests
```

현재 테스트:

- 설정 저장
- RAM 값 보정
- 서버 ping JSON 파싱
- 로그 기록
- manifest 파싱
- 잘못된 manifest 거부
- SHA-256 검사
- 알 수 없는 파일 격리
- path traversal 차단

## 다음 개발 순서

다음은 3번 단계입니다.

- Microsoft 로그인
- Java 런타임 준비
- Minecraft 파일 준비
- `online.zzapcho.kr` 바로 접속
