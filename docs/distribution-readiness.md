# 배포 준비 상태

이 런처는 일반 유저에게 SDK나 개발 도구를 설치시키지 않는 형태를 기준으로 배포합니다.

## 배포 방식

- 개발자 PC: `scripts/*.ps1`이 저장소 안에 .NET 8 SDK를 자동 설치할 수 있습니다.
- 유저 PC: Velopack이 만든 설치 파일을 실행합니다. 유저는 .NET SDK를 설치하지 않습니다.
- 앱 업데이트: GitHub Releases에 올라간 Velopack 릴리즈를 런처가 확인합니다.
- 클라이언트 파일 버전: `manifest/launcher-manifest.json`에서 관리합니다.

## 버전 수정 위치

런처 앱 버전은 저장소의 `Directory.Build.props`에서 수정합니다.

```xml
<Version>1.0.0-beta.1</Version>
<AssemblyVersion>1.0.0.0</AssemblyVersion>
<FileVersion>1.0.0.0</FileVersion>
```

Velopack 패키지 버전은 SemVer 형식이어야 합니다. 예: `1.0.0`, `1.0.0-beta.1`.

Minecraft 버전, 로더 버전, 서버 파일 목록은 `manifest/launcher-manifest.json`에서 수정합니다.

## 로컬 배포 빌드

퍼블리시 폴더만 만들 때:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish.ps1
```

설치 파일까지 만들 때:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package.ps1
```

결과물:

```text
artifacts/publish/win-x64
artifacts/releases/win-x64
```

## GitHub Actions

- `.github/workflows/build.yml`: PR/Push에서 restore, build, test, self-contained publish를 검증합니다.
- `.github/workflows/release.yml`: 태그 `v*` push 또는 수동 실행으로 Velopack 패키지를 만들고 GitHub Release에 업로드합니다.

수동 릴리즈 예:

```text
Actions -> release -> Run workflow -> version 입력
```

태그 릴리즈 예:

```powershell
git tag v1.0.0-beta.1
git push origin v1.0.0-beta.1
```

## 배포 전 체크

- `scripts\test.ps1` 통과
- `scripts\publish.ps1` 통과
- `scripts\package.ps1` 통과
- GitHub Release에 Velopack 산출물 업로드 확인
- 코드 서명 인증서 적용 여부 확인
- Windows SmartScreen 경고 확인
- 새 PC에서 설치, 실행, 업데이트 확인

## 남은 운영 작업

- 코드 서명 인증서 준비
- GitHub Release 첫 배포 생성
- 실제 Microsoft 로그인과 Minecraft 실행 연결 전까지 Play 동작은 비활성/placeholder 상태로 유지
- 실제 업데이트 적용 UI는 이후 단계에서 `IUpdateService` 확장으로 연결
