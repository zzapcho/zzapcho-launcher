# 릴리즈 프로세스

정식 배포는 Velopack과 GitHub Releases를 기준으로 처리한다.

## 개발 중 실행

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\dev.ps1
```

## 배포 목표

- self-contained publish 생성
- Velopack 설치 파일 생성
- GitHub Releases 업로드
- 런처 시작 시 업데이트 확인

## 로컬 배포 명령

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\package.ps1
```

주요 결과물:

```text
artifacts/publish/win-x64
artifacts/releases/win-x64/Zzapcho.RaidLauncher-win-Setup.exe
artifacts/releases/win-x64/Zzapcho.RaidLauncher-win-Portable.zip
```

## GitHub 배포

`.github/workflows/release.yml`을 수동 실행하거나 `v*` 태그를 push하면 Velopack 패키지를 생성하고 GitHub Release에 업로드한다.

런처 버전은 `Directory.Build.props`에서 관리한다.
패키지 버전은 release workflow 입력값으로 덮어쓸 수 있다.

## 코드서명

운영 배포에서는 Authenticode 코드서명 인증서가 필요하다.
서명 없는 EXE는 SmartScreen 경고가 뜰 수 있다.
런처는 Windows 보안 경고를 숨기거나 끄려고 하면 안 된다.
