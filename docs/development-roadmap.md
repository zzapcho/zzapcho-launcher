# 개발 로드맵

## 1단계 - 앱 뼈대, UI, 설정, 로그, 서버 상태

현재 단계.

- WPF/MVVM 구조
- 입장, 업데이트, 로그, 설정, 정보 화면
- 고정 서버 상태 표시
- Minecraft Server List Ping
- 설정 저장
- 로그 저장/조회

## 2단계 - manifest와 파일 검증

현재 단계.

- 공식 manifest 형식
- SHA-256 파일 검사
- 누락 파일 탐지
- 잘못된 파일 탐지
- 알 수 없는 모드/리소스팩 격리
- GitHub raw manifest URL 기반 버전 관리

## 3단계 - 로그인, Java, Minecraft 실행

- Microsoft 로그인
- Java 런타임 준비
- Minecraft 버전 준비
- `online.zzapcho.kr` 바로 접속
- `servers.dat` 관리

## 4단계 - 런처 인증과 Paper 플러그인

- 런처 입장 허가 요청
- 짧은 permit 발급 구조
- Paper 플러그인 `ZzapchoGate`
- 핸드셰이크 설계

## 5단계 - 자동 업데이트와 설치파일

- Velopack
- GitHub Releases
- 설치 파일
- 강제 업데이트
- 코드서명 준비

## 6단계 - 크래시 리포트와 지원 ZIP

- 크래시 요약
- 로그 ZIP 생성
- 민감 정보 제거
- 백엔드 업로드 구조

## 7단계 - 최종 폴리싱과 릴리즈 준비

- UI 폴리싱
- 첫 실행 경험
- QA 체크리스트
- 보안 리뷰
- 베타 배포 준비

## 완료 기준

최종적으로 사용자는 Java나 모드 구조를 몰라도 런처에서 로그인하고 서버에 입장할 수 있어야 한다.
오류가 발생해도 사용자가 무섭지 않게 이해하고, 운영자는 로그로 원인을 추적할 수 있어야 한다.
