# 잡초 약탈서버 런처

`잡초 약탈서버 런처`는 `online.zzapcho.kr` 전용 Windows Minecraft 런처입니다.

현재 구현 범위는 1-7번 단계의 베타 골격입니다.

- WPF/MVVM 앱 뼈대
- 입장, 업데이트, 로그, 설정, 정보 화면
- 고정 서버 상태 표시
- Minecraft Server List Ping 기반 서버 상태 확인
- 30초 자동 새로고침
- 설정 저장
- 로그 저장과 조회
- GitHub manifest 기반 버전/파일 목록 관리 구조
- manifest 검증
- SHA-256 파일 검사
- 알 수 없는 파일 격리 구조
- 참조 런처(`zzapcho/zzapcho-launcher`) 스타일 기반 UI 개편
- 홈/모드/리소스팩/셰이더/설정/로그 구성
- 개발용 로그인 상태
- 실행/업데이트/크래시/지원 ZIP 서비스 골격
- Velopack 기반 배포 패키징과 런처 업데이트 확인 골격
- Paper 플러그인 문서/설정 골격

아직 구현하지 않은 기능:

- 실제 Microsoft OAuth 로그인
- 실제 Minecraft 실행
- 실제 업데이트 다운로드/재시작 UI
- 실제 크래시 리포트 업로드

## 고정 서버

런처는 하나의 서버만 대상으로 합니다.

```text
online.zzapcho.kr:25565
```

사용자가 서버 주소를 추가하거나 바꾸는 UI는 제공하지 않습니다.

## 저장 위치

런처 데이터는 사용자별 LocalAppData 아래에 저장합니다.

```text
%LocalAppData%\ZzapchoRaidLauncher
```

주요 경로:

- 설정: `%LocalAppData%\ZzapchoRaidLauncher\settings.json`
- 로그: `%LocalAppData%\ZzapchoRaidLauncher\logs`
- 인스턴스: `%LocalAppData%\ZzapchoRaidLauncher\instances\main`
- 크래시: `%LocalAppData%\ZzapchoRaidLauncher\crashes`
- 격리: `%LocalAppData%\ZzapchoRaidLauncher\quarantine`

## GitHub에서 버전 바꾸기

Minecraft 버전, Fabric 로더 버전, 런처 최소 버전, 공식 파일 목록은 앱 코드가 아니라 manifest에서 관리합니다.

운영용 manifest:

```text
manifest/launcher-manifest.json
```

GitHub에 올린 뒤 raw URL을 런처 설정의 `manifestUrl`에 넣으면 됩니다.

예시:

```text
https://raw.githubusercontent.com/OWNER/REPO/main/manifest/launcher-manifest.json
```

GitHub에서 바꿀 수 있는 주요 값:

- `manifestVersion`
- `minecraft.version`
- `minecraft.loader`
- `minecraft.loaderVersion`
- `launcher.minimumVersion`
- `launcher.latestVersion`
- `files`

자세한 개발/운영 방법은 [DEVELOPER-README.md](DEVELOPER-README.md)를 보세요.

## 빌드

개발자가 가장 쉽게 실행하는 방법:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\dev.ps1
```

이 명령은 .NET 8 SDK가 없으면 저장소 내부 `.dotnet-sdk` 폴더에 자동 설치하고, 빌드/테스트 후 앱을 실행합니다.

명령별 사용:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\setup.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\test.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\run.ps1
```

배포용 산출물을 만들 때:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\package.ps1
```

`publish.ps1`는 유저 PC에 SDK가 필요 없는 self-contained 앱 폴더를 만들고, `package.ps1`는 Velopack 설치/업데이트 패키지를 `artifacts/releases/win-x64`에 만듭니다.

직접 설치된 .NET SDK를 쓰는 수동 방식:

```powershell
dotnet restore ZzapchoRaidLauncher.sln
dotnet build ZzapchoRaidLauncher.sln
dotnet run --project src/Zzapcho.Launcher.Tests
```

자세한 개발자용 실행/운영 방법은 [DEVELOPER-README.md](DEVELOPER-README.md)에 정리했습니다.

배포 절차는 [docs/distribution-readiness.md](docs/distribution-readiness.md)를 보세요.

## 보안 방향

런처만으로는 런처 미사용 접속을 완벽히 막을 수 없습니다.

정식 구조에서는 다음이 함께 필요합니다.

- 런처의 manifest 기반 파일 검증
- 공식 파일 SHA-256 검사
- 알 수 없는 파일 격리
- Microsoft 계정 로그인
- 백엔드의 짧은 입장 허가
- Paper 서버 플러그인 `ZzapchoGate`의 서버 측 차단

서버 측 검증 전까지 클라이언트 검사는 보조 방어입니다.
