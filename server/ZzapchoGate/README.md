# ZzapchoGate

Paper 서버에서 런처 사용 여부를 검증하는 플러그인 스켈레톤 위치입니다.

정식 구현 목표:

- UUID 기준 permit 확인
- permit 없음/만료 시 킥
- 15초 handshake timeout
- 정확한 서버 상태 API 제공

킥 메시지:

```text
잡초 약탈서버 런처로 접속해주세요.
```
